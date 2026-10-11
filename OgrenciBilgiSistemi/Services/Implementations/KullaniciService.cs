using OgrenciBilgiSistemi.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using OgrenciBilgiSistemi.Data;
using OgrenciBilgiSistemi.Helpers;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Services.Interfaces;
using OgrenciBilgiSistemi.Shared.Services;
using OgrenciBilgiSistemi.ViewModels;

namespace OgrenciBilgiSistemi.Services.Implementations
{
    public sealed class KullaniciService : IKullaniciService
    {
        private readonly AppDbContext _db;
        private readonly IFileStorage _files;
        private readonly IMemoryCache _cache;
        private readonly TenantBaglami _tenant;
        private readonly PasswordHasher<KullaniciModel> _passwordHasher = new();

        public KullaniciService(AppDbContext db, IFileStorage files, IMemoryCache cache, TenantBaglami tenant)
        {
            _db = db;
            _files = files;
            _cache = cache;
            _tenant = tenant;
        }

        public async Task<SayfalanmisListeModel<KullaniciModel>> SearchPagedAsync(
            string? searchString, int page, int pageSize = 10, CancellationToken ct = default)
        {
            // Genel Yönetici satırı okul kullanıcısı değildir; listede gösterilmez
            var query = _db.Kullanicilar
                .AsNoTracking()
                .Where(k => k.Rol != KullaniciRolu.GenelAdmin);

            if (!string.IsNullOrWhiteSpace(searchString))
                query = query.Where(k => k.KullaniciAdi.Contains(searchString));

            return await SayfalanmisListeModel<KullaniciModel>
                .CreateAsync(query.OrderBy(k => k.KullaniciAdi), page, pageSize, ct);
        }

        public async Task<KullaniciModel?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            return await _db.Kullanicilar
                .Include(k => k.ServisProfil)
                .Include(k => k.OgretmenProfil).ThenInclude(o => o!.Birim)
                .Include(k => k.VeliProfil)
                .FirstOrDefaultAsync(k => k.KullaniciId == id, ct);
        }

        public async Task EkleAsync(KullaniciModel model, CancellationToken ct = default)
        {
            // Genel Yönetici yalnızca yapılandırmadan gelir; panelden atanamaz
            if (model.Rol == KullaniciRolu.GenelAdmin)
                throw new InvalidOperationException("Genel Yönetici rolü atanamaz.");

            // OgretmenProfil: görsel kaydet
            // Görsel yolu yalnızca yüklenen dosyadan gelir; formdan gelen değer kullanılmaz
            if (model.Rol == KullaniciRolu.Ogretmen && model.OgretmenProfil != null)
            {
                var dosya = model.OgretmenProfil.GorselFile;
                model.OgretmenProfil.GorselPath = dosya is { Length: > 0 }
                    ? await _files.SaveImageAsync(dosya, null, ct, "kullanici")
                    : null;
            }

            model.Sifre = _passwordHasher.HashPassword(model, model.Sifre);

            // Role uygun olmayan profilleri temizle (form'dan gelmiş olabilir)
            if (model.Rol != KullaniciRolu.Ogretmen)
                model.OgretmenProfil = null;
            if (model.Rol != KullaniciRolu.Servis)
                model.ServisProfil = null;
            if (model.Rol != KullaniciRolu.Veli)
                model.VeliProfil = null;

            _db.Kullanicilar.Add(model);
            await _db.SaveChangesAsync(ct);
        }

        public async Task GuncelleAsync(KullaniciModel model, int islemYapanKullaniciId, CancellationToken ct = default)
        {
            var kullanici = await _db.Kullanicilar
                .Include(k => k.OgretmenProfil)
                .Include(k => k.ServisProfil)
                .Include(k => k.VeliProfil)
                .FirstOrDefaultAsync(k => k.KullaniciId == model.KullaniciId, ct)
                ?? throw new KeyNotFoundException("Kullanıcı bulunamadı.");

            if (model.Rol == KullaniciRolu.GenelAdmin || kullanici.Rol == KullaniciRolu.GenelAdmin)
                throw new InvalidOperationException("Genel Yönetici rolü atanamaz ve bu kullanıcı düzenlenemez.");

            var kendisiMi = kullanici.KullaniciId == islemYapanKullaniciId;
            if (kendisiMi && model.IsDeleted)
                throw new InvalidOperationException("Kendi hesabınızı pasife alamazsınız.");
            if (kendisiMi && kullanici.Rol == KullaniciRolu.Admin && model.Rol != KullaniciRolu.Admin)
                throw new InvalidOperationException("Kendi Yönetici rolünüzü kaldıramazsınız.");
            if (kullanici.Rol == KullaniciRolu.Admin && !kullanici.IsDeleted
                && (model.Rol != KullaniciRolu.Admin || model.IsDeleted)
                && await SonAktifAdminMiAsync(kullanici.KullaniciId, ct))
                throw new InvalidOperationException("Okulun son aktif Yöneticisi pasife alınamaz veya rolü değiştirilemez.");

            var eskiOgretmenBirimId = kullanici.OgretmenProfil?.BirimId;

            kullanici.KullaniciAdi = model.KullaniciAdi;
            kullanici.Rol = model.Rol;
            kullanici.IsDeleted = model.IsDeleted;
            kullanici.Telefon = model.Telefon;
            kullanici.BeniHatirla = model.BeniHatirla;

            // OgretmenProfil yönetimi
            if (model.Rol == KullaniciRolu.Ogretmen)
            {
                var profil = kullanici.OgretmenProfil;
                if (profil == null)
                {
                    profil = new OgretmenProfilModel { KullaniciId = kullanici.KullaniciId };
                    _db.OgretmenProfiller.Add(profil);
                }

                profil.BirimId = model.OgretmenProfil?.BirimId;
                profil.Email = model.OgretmenProfil?.Email;
                profil.IsDeleted = kullanici.IsDeleted;

                if (model.OgretmenProfil?.GorselFile != null && model.OgretmenProfil.GorselFile.Length > 0)
                    profil.GorselPath = await _files.SaveImageAsync(model.OgretmenProfil.GorselFile, profil.GorselPath, ct, "kullanici");
            }
            else if (kullanici.OgretmenProfil != null)
            {
                // Artık öğretmen değil: profil öğretmen listelerinde/atamalarda görünmesin
                kullanici.OgretmenProfil.IsDeleted = true;
            }

            // ServisProfil yönetimi
            if (model.Rol == KullaniciRolu.Servis)
            {
                var profil = kullanici.ServisProfil;
                if (profil == null)
                {
                    profil = new ServisProfilModel { KullaniciId = kullanici.KullaniciId };
                    _db.ServisProfiller.Add(profil);
                }

                profil.Plaka = model.ServisProfil?.Plaka ?? string.Empty;
                profil.IsDeleted = kullanici.IsDeleted;
            }
            else if (kullanici.ServisProfil != null)
            {
                kullanici.ServisProfil.IsDeleted = true;
            }

            // VeliProfil yönetimi
            if (model.Rol == KullaniciRolu.Veli)
            {
                var profil = kullanici.VeliProfil;
                if (profil == null)
                {
                    profil = new VeliProfilModel { KullaniciId = kullanici.KullaniciId };
                    _db.VeliProfiller.Add(profil);
                }

                profil.VeliAdres = model.VeliProfil?.VeliAdres;
                profil.VeliMeslek = model.VeliProfil?.VeliMeslek;
                profil.VeliIsYeri = model.VeliProfil?.VeliIsYeri;
                profil.VeliEmail = model.VeliProfil?.VeliEmail;
                profil.VeliYakinlik = model.VeliProfil?.VeliYakinlik;
                profil.IsDeleted = kullanici.IsDeleted;
            }
            else if (kullanici.VeliProfil != null)
            {
                // Artık veli değil: veli listelerinde ve duyuru alıcılarında görünmesin
                kullanici.VeliProfil.IsDeleted = true;
            }

            if (!string.IsNullOrWhiteSpace(model.Sifre))
                kullanici.Sifre = _passwordHasher.HashPassword(kullanici, model.Sifre);

            await _db.SaveChangesAsync(ct);

            if (eskiOgretmenBirimId.HasValue || kullanici.OgretmenProfil != null)
                await OgretmenAtamaHelper.OgretmenDegistiAsync(
                    _db, kullanici.KullaniciId, [eskiOgretmenBirimId, kullanici.OgretmenProfil?.BirimId], ct);
        }

        public async Task SilAsync(int id, int islemYapanKullaniciId, CancellationToken ct = default)
        {
            var kullanici = await _db.Kullanicilar.FindAsync([id], ct);
            if (kullanici == null) return;

            if (kullanici.Rol == KullaniciRolu.GenelAdmin)
                throw new InvalidOperationException("Genel Yönetici kaydı silinemez.");
            if (id == islemYapanKullaniciId)
                throw new InvalidOperationException("Kendi hesabınızı silemezsiniz.");
            if (kullanici.Rol == KullaniciRolu.Admin && !kullanici.IsDeleted && await SonAktifAdminMiAsync(id, ct))
                throw new InvalidOperationException("Okulun son aktif Yöneticisi silinemez.");

            // OgretmenId türetilmiş alan: öğretmen silinince öğrenciler birimdeki diğer öğretmene aktarılır.
            var bagliOgrenciVar = await _db.Ogrenciler
                .AnyAsync(o => !o.IsDeleted &&
                               (o.VeliId == id || o.ServisId == id), ct);
            if (bagliOgrenciVar)
                throw new InvalidOperationException(
                    "Bu kullanıcıya bağlı aktif öğrenci kaydı var. " +
                    "Kullanıcıyı pasife almadan önce öğrenci ilişkilerini kaldırın veya öğrencileri pasife alın.");

            kullanici.IsDeleted = true;

            // Bağlı servis profili varsa durumunu pasife çek
            var servisProfil = await _db.ServisProfiller.FindAsync([id], ct);
            if (servisProfil != null)
                servisProfil.IsDeleted = true;

            // Bağlı öğretmen profili varsa durumunu pasife çek
            var ogretmenProfil = await _db.OgretmenProfiller.FindAsync([id], ct);
            if (ogretmenProfil != null)
                ogretmenProfil.IsDeleted = true;

            // Bağlı veli profili varsa durumunu pasife çek
            var veliProfil = await _db.VeliProfiller.FindAsync([id], ct);
            if (veliProfil != null)
                veliProfil.IsDeleted = true;

            await _db.SaveChangesAsync(ct);

            if (ogretmenProfil != null)
                await OgretmenAtamaHelper.OgretmenDegistiAsync(_db, id, [ogretmenProfil.BirimId], ct);
        }

        // Bu kullanıcı dışında aktif Admin yoksa true
        private Task<bool> SonAktifAdminMiAsync(int adminId, CancellationToken ct)
            => _db.Kullanicilar.AllAsync(k => k.KullaniciId == adminId || k.IsDeleted || k.Rol != KullaniciRolu.Admin, ct);

        public Task<bool> KullaniciAdiVarMiAsync(string kullaniciAdi, int? excludeId = null, CancellationToken ct = default)
            => _db.Kullanicilar.AnyAsync(k =>
                k.KullaniciAdi == kullaniciAdi &&
                !k.IsDeleted &&
                (!excludeId.HasValue || k.KullaniciId != excludeId.Value), ct);

        public async Task<List<SelectListItem>> GetPersonellerSelectListAsync(CancellationToken ct = default)
            => await _db.Kullanicilar
                .AsNoTracking()
                .Where(k => !k.IsDeleted && k.Rol == KullaniciRolu.Ogretmen)
                .OrderBy(k => k.KullaniciAdi)
                .Select(k => new SelectListItem
                {
                    Value = k.KullaniciId.ToString(),
                    Text = k.KullaniciAdi
                })
                .ToListAsync(ct);

        public async Task<List<SelectListItem>> GetServislerSelectListAsync(CancellationToken ct = default)
            => await _db.ServisProfiller
                .AsNoTracking()
                .Where(s => !s.IsDeleted)
                .OrderBy(s => s.Plaka)
                .Select(s => new SelectListItem
                {
                    Value = s.KullaniciId.ToString(),
                    Text = s.Plaka
                })
                .ToListAsync(ct);

        public async Task<List<SelectListItem>> GetServislerPlakaliSelectListAsync(CancellationToken ct = default)
            => await _db.Kullanicilar
                .AsNoTracking()
                .Where(k => k.Rol == KullaniciRolu.Servis && !k.IsDeleted)
                .OrderBy(k => k.KullaniciAdi)
                .Select(k => new SelectListItem
                {
                    Value = k.KullaniciId.ToString(),
                    Text = k.ServisProfil != null ? k.ServisProfil.Plaka + " - " + k.KullaniciAdi : k.KullaniciAdi
                })
                .ToListAsync(ct);

        public async Task<List<SelectListItem>> GetKullanicilarByRolSelectListAsync(KullaniciRolu rol, CancellationToken ct = default)
            => await _db.Kullanicilar
                .AsNoTracking()
                .Where(k => !k.IsDeleted && k.Rol == rol)
                .OrderBy(k => k.KullaniciAdi)
                .Select(k => new SelectListItem
                {
                    Value = k.KullaniciId.ToString(),
                    Text = k.KullaniciAdi
                })
                .ToListAsync(ct);

        // --- Yetki Yönetimi ---

        // Yalnızca Yönetici (AdminOnly) ekranları: Öğretmene atanırsa menüde görünür ama açılmaz
        private static readonly HashSet<string> SadeceYoneticiControllerlari = new(StringComparer.OrdinalIgnoreCase)
        {
            "Kullanicilar", "Ogretmenler", "Veliler", "Servisler", "Duyurular", "Randevular", "OgretmenRandevu"
        };

        private static bool RoleUygunMenuMu(KullaniciRolu rol, MenuOgeModel menu)
            => rol != KullaniciRolu.Ogretmen
               || string.IsNullOrEmpty(menu.Controller)
               || !SadeceYoneticiControllerlari.Contains(menu.Controller);

        public async Task<KullaniciMenuAtamaVm?> GetYetkiVmAsync(int kullaniciId, CancellationToken ct = default)
        {
            var user = await _db.Kullanicilar
                .Include(u => u.KullaniciMenuler)
                .FirstOrDefaultAsync(u => u.KullaniciId == kullaniciId, ct);

            // Genel Yönetici'nin menüleri okul seçiminde otomatik tamamlanır; elle düzenlenmez
            if (user == null || user.Rol == KullaniciRolu.GenelAdmin) return null;

            var allMenus = (await _db.MenuOgeler
                .AsNoTracking()
                .OrderBy(m => m.Sirala)
                .ToListAsync(ct))
                .Where(m => RoleUygunMenuMu(user.Rol, m))
                .ToList();

            var assignedMenuIds = user.KullaniciMenuler.Select(km => km.MenuOgeId).ToList();

            return new KullaniciMenuAtamaVm
            {
                KullaniciId = user.KullaniciId,
                KullaniciAdi = user.KullaniciAdi,
                Menuler = BuildMenuViewModels(null, allMenus, assignedMenuIds)
            };
        }

        public async Task YetkiGuncelleAsync(int kullaniciId, List<int>? selectedMenuIds, CancellationToken ct = default)
        {
            var user = await _db.Kullanicilar
                .Include(u => u.KullaniciMenuler)
                .FirstOrDefaultAsync(u => u.KullaniciId == kullaniciId, ct)
                ?? throw new KeyNotFoundException("Kullanıcı bulunamadı.");

            if (user.Rol == KullaniciRolu.GenelAdmin)
                throw new InvalidOperationException("Genel Yönetici yetkileri elle düzenlenemez.");

            // Role uymayan menüler (Öğretmen için Yönetici ekranları) atanmaz
            var uygunMenuIdler = (await _db.MenuOgeler.AsNoTracking().ToListAsync(ct))
                .Where(m => RoleUygunMenuMu(user.Rol, m))
                .Select(m => m.Id)
                .ToHashSet();

            var desired = (selectedMenuIds ?? new List<int>()).Where(uygunMenuIdler.Contains).ToHashSet();
            var current = user.KullaniciMenuler.Select(km => km.MenuOgeId).ToHashSet();

            var toRemove = current.Except(desired).ToList();
            var toAdd = desired.Except(current).ToList();

            var strategy = _db.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var tx = await _db.Database.BeginTransactionAsync(ct);

                if (toRemove.Count > 0)
                {
                    var removeEntities = user.KullaniciMenuler
                        .Where(km => toRemove.Contains(km.MenuOgeId))
                        .ToList();
                    foreach (var rem in removeEntities)
                        user.KullaniciMenuler.Remove(rem);
                }

                foreach (var mid in toAdd)
                {
                    user.KullaniciMenuler.Add(new KullaniciMenuModel
                    {
                        KullaniciId = user.KullaniciId,
                        MenuOgeId = mid
                    });
                }

                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            });

            _cache.Remove($"menu:{_tenant.OkulKodu}:{kullaniciId}");
        }

        // seciliBirimId pasif olsa da listede kalır; aksi halde form kaydedilince birim sessizce silinir.
        public async Task<List<SelectListItem>> GetBirimlerSelectListAsync(int? seciliBirimId = null, CancellationToken ct = default)
            => await _db.Birimler
                .AsNoTracking()
                .Where(b => !b.IsDeleted || b.BirimId == seciliBirimId)
                .OrderBy(b => b.BirimAd)
                .Select(b => new SelectListItem
                {
                    Value = b.BirimId.ToString(),
                    Text = b.BirimAd
                })
                .ToListAsync(ct);

        private static List<MenuOgeAtamaVm> BuildMenuViewModels(
            int? parentId, List<MenuOgeModel> allMenus, List<int> assignedMenuIds)
        {
            return allMenus
                .Where(m => m.AnaMenuId == parentId)
                .OrderBy(m => m.Sirala)
                .Select(menu => new MenuOgeAtamaVm
                {
                    MenuOgeId = menu.Id,
                    Baslik = menu.Baslik,
                    AtandiMi = assignedMenuIds.Contains(menu.Id),
                    AnaMenuId = menu.AnaMenuId,
                    AltOgeler = BuildMenuViewModels(menu.Id, allMenus, assignedMenuIds)
                })
                .ToList();
        }
    }
}
