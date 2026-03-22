using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OgrenciBilgiSistemi.Data;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Services.Interfaces;
using OgrenciBilgiSistemi.Shared.Enums;
using OgrenciBilgiSistemi.ViewModels;

namespace OgrenciBilgiSistemi.Services.Implementations
{
    public sealed class KullaniciService : IKullaniciService
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly PasswordHasher<KullaniciModel> _passwordHasher = new();

        public KullaniciService(AppDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        public async Task<SayfalanmisListeModel<KullaniciModel>> SearchPagedAsync(
            string? searchString, int page, int pageSize = 10, CancellationToken ct = default)
        {
            var query = _db.Kullanicilar
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
                query = query.Where(k => k.KullaniciAdi.Contains(searchString));

            return await SayfalanmisListeModel<KullaniciModel>
                .CreateAsync(query.OrderBy(k => k.KullaniciAdi), page, pageSize, ct);
        }

        public async Task<KullaniciModel?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var kullanici = await _db.Kullanicilar
                .Include(k => k.Birim)
                .FirstOrDefaultAsync(k => k.KullaniciId == id, ct);

            if (kullanici != null)
            {
                var servisId = await _db.Servisler
                    .Where(s => s.KullaniciId == id)
                    .Select(s => (int?)s.ServisId)
                    .FirstOrDefaultAsync(ct);

                kullanici.ServisId = servisId;
            }

            return kullanici;
        }

        public async Task EkleAsync(KullaniciModel model, CancellationToken ct = default)
        {
            // Role göre koşullu FK temizliği
            if (model.Rol != KullaniciRolu.Ogretmen && model.Rol != KullaniciRolu.Admin)
            {
                model.BirimId = null;
                model.KartNo = null;
            }

            // KartNo normalize + tekillik
            model.KartNo = NormalizeKartNo(model.KartNo);
            if (!string.IsNullOrWhiteSpace(model.KartNo) &&
                await _db.Kullanicilar.AnyAsync(k => k.KartNo == model.KartNo && k.KullaniciDurum, ct))
                throw new InvalidOperationException("Bu kart numarası başka bir kullanıcıda kayıtlı.");

            if (model.Rol == KullaniciRolu.Sofor && model.ServisId.HasValue &&
                await _db.Servisler.AnyAsync(s => s.ServisId == model.ServisId && s.KullaniciId != null, ct))
                throw new InvalidOperationException("Bu servis zaten başka bir şoföre atanmış.");

            // Görsel kaydet
            if (model.GorselFile != null && model.GorselFile.Length > 0)
                model.GorselPath = await SaveImageAsync(model.GorselFile, ct);

            model.Sifre = _passwordHasher.HashPassword(model, model.Sifre);
            _db.Kullanicilar.Add(model);
            await _db.SaveChangesAsync(ct);

            // Servis bağlantısı (kayıt sonrası KullaniciId oluşmuş olur)
            if (model.Rol == KullaniciRolu.Sofor && model.ServisId.HasValue)
            {
                var servis = await _db.Servisler.FindAsync([model.ServisId.Value], ct);
                if (servis != null)
                {
                    servis.KullaniciId = model.KullaniciId;
                    await _db.SaveChangesAsync(ct);
                }
            }
        }

        public async Task GuncelleAsync(KullaniciModel model, CancellationToken ct = default)
        {
            var kullanici = await _db.Kullanicilar.FindAsync([model.KullaniciId], ct)
                ?? throw new KeyNotFoundException("Kullanıcı bulunamadı.");

            // KartNo normalize + tekillik
            var normalizedKartNo = NormalizeKartNo(model.KartNo);
            if (!string.Equals(kullanici.KartNo, normalizedKartNo, StringComparison.Ordinal) &&
                !string.IsNullOrWhiteSpace(normalizedKartNo) &&
                await _db.Kullanicilar.AnyAsync(k => k.KartNo == normalizedKartNo && k.KullaniciId != model.KullaniciId && k.KullaniciDurum, ct))
                throw new InvalidOperationException("Bu kart numarası başka bir kullanıcıda kayıtlı.");

            if (model.Rol == KullaniciRolu.Sofor && model.ServisId.HasValue &&
                await _db.Servisler.AnyAsync(s => s.ServisId == model.ServisId && s.KullaniciId != null && s.KullaniciId != model.KullaniciId, ct))
                throw new InvalidOperationException("Bu servis zaten başka bir şoföre atanmış.");

            kullanici.KullaniciAdi = model.KullaniciAdi;
            kullanici.Rol = model.Rol;
            kullanici.KullaniciDurum = model.KullaniciDurum;
            kullanici.Telefon = model.Telefon;
            kullanici.BeniHatirla = model.BeniHatirla;
            kullanici.Email = model.Email;
            // Role göre BirimId ve KartNo
            if (model.Rol == KullaniciRolu.Ogretmen || model.Rol == KullaniciRolu.Admin)
            {
                kullanici.BirimId = model.BirimId;
                kullanici.KartNo = normalizedKartNo;
            }
            else
            {
                kullanici.BirimId = null;
                kullanici.KartNo = null;
            }

            // Görsel kaydet
            if (model.GorselFile != null && model.GorselFile.Length > 0)
                kullanici.GorselPath = await SaveImageAsync(model.GorselFile, ct);

            if (!string.IsNullOrWhiteSpace(model.Sifre))
                kullanici.Sifre = _passwordHasher.HashPassword(kullanici, model.Sifre);

            // Servis bağlantısı yönetimi
            var eskiServis = await _db.Servisler
                .FirstOrDefaultAsync(s => s.KullaniciId == kullanici.KullaniciId, ct);

            if (eskiServis != null && eskiServis.ServisId != model.ServisId)
                eskiServis.KullaniciId = null;

            if (model.Rol == KullaniciRolu.Sofor && model.ServisId.HasValue)
            {
                var yeniServis = await _db.Servisler.FindAsync([model.ServisId.Value], ct);
                if (yeniServis != null)
                    yeniServis.KullaniciId = kullanici.KullaniciId;
            }

            await _db.SaveChangesAsync(ct);
        }

        public async Task SilAsync(int id, CancellationToken ct = default)
        {
            var kullanici = await _db.Kullanicilar.FindAsync([id], ct);
            if (kullanici == null) return;

            kullanici.KullaniciDurum = false;

            // Bağlı servis varsa bağlantıyı temizle
            var servis = await _db.Servisler
                .FirstOrDefaultAsync(s => s.KullaniciId == id, ct);
            if (servis != null)
                servis.KullaniciId = null;

            await _db.SaveChangesAsync(ct);
        }

        public Task<bool> KullaniciAdiVarMiAsync(string kullaniciAdi, int? excludeId = null, CancellationToken ct = default)
            => _db.Kullanicilar.AnyAsync(k =>
                k.KullaniciAdi == kullaniciAdi &&
                k.KullaniciDurum &&
                (!excludeId.HasValue || k.KullaniciId != excludeId.Value), ct);

        public async Task<List<SelectListItem>> GetPersonellerSelectListAsync(CancellationToken ct = default)
            => await _db.Kullanicilar
                .AsNoTracking()
                .Where(k => k.KullaniciDurum && k.Rol == KullaniciRolu.Ogretmen)
                .OrderBy(k => k.KullaniciAdi)
                .Select(k => new SelectListItem
                {
                    Value = k.KullaniciId.ToString(),
                    Text = k.KullaniciAdi
                })
                .ToListAsync(ct);

        public async Task<List<SelectListItem>> GetServislerSelectListAsync(CancellationToken ct = default)
            => await _db.Servisler
                .AsNoTracking()
                .Where(s => s.ServisDurum)
                .OrderBy(s => s.Plaka)
                .Select(s => new SelectListItem
                {
                    Value = s.ServisId.ToString(),
                    Text = s.Plaka
                })
                .ToListAsync(ct);

        public async Task<List<SelectListItem>> GetKullanicilarByRolSelectListAsync(KullaniciRolu rol, CancellationToken ct = default)
            => await _db.Kullanicilar
                .AsNoTracking()
                .Where(k => k.KullaniciDurum && k.Rol == rol)
                .OrderBy(k => k.KullaniciAdi)
                .Select(k => new SelectListItem
                {
                    Value = k.KullaniciId.ToString(),
                    Text = k.KullaniciAdi
                })
                .ToListAsync(ct);

        // --- Yetki Yönetimi ---

        public async Task<KullaniciMenuAtamaVm?> GetYetkiVmAsync(int kullaniciId, CancellationToken ct = default)
        {
            var user = await _db.Kullanicilar
                .Include(u => u.KullaniciMenuler)
                .FirstOrDefaultAsync(u => u.KullaniciId == kullaniciId, ct);

            if (user == null) return null;

            var allMenus = await _db.MenuOgeler
                .AsNoTracking()
                .OrderBy(m => m.Sirala)
                .ToListAsync(ct);

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

            var desired = (selectedMenuIds ?? new List<int>()).ToHashSet();
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
        }

        public async Task<List<SelectListItem>> GetBirimlerSelectListAsync(CancellationToken ct = default)
            => await _db.Birimler
                .AsNoTracking()
                .Where(b => b.BirimDurum)
                .OrderBy(b => b.BirimAd)
                .Select(b => new SelectListItem
                {
                    Value = b.BirimId.ToString(),
                    Text = b.BirimAd
                })
                .ToListAsync(ct);

        private static string? NormalizeKartNo(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            var t = s.Trim().TrimStart('0');
            return t;
        }

        private async Task<string> SaveImageAsync(IFormFile file, CancellationToken ct)
        {
            var root = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads", "kullanici");
            Directory.CreateDirectory(root);

            var ext = Path.GetExtension(file.FileName);
            var name = $"kul_{Guid.NewGuid():N}{ext}";
            var full = Path.Combine(root, name);

            using (var fs = new FileStream(full, FileMode.Create))
                await file.CopyToAsync(fs, ct);

            var rel = Path.Combine("uploads", "kullanici", name).Replace("\\", "/");
            return "/" + rel;
        }

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
