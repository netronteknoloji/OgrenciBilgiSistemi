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
            return await _db.Kullanicilar
                .Include(k => k.ServisProfil)
                .Include(k => k.OgretmenProfil).ThenInclude(o => o!.Birim)
                .Include(k => k.VeliProfil)
                .FirstOrDefaultAsync(k => k.KullaniciId == id, ct);
        }

        public async Task EkleAsync(KullaniciModel model, CancellationToken ct = default)
        {
            // OgretmenProfil: görsel kaydet
            if (model.Rol == KullaniciRolu.Ogretmen && model.OgretmenProfil != null)
            {
                if (model.OgretmenProfil.GorselFile != null && model.OgretmenProfil.GorselFile.Length > 0)
                    model.OgretmenProfil.GorselPath = await SaveImageAsync(model.OgretmenProfil.GorselFile, ct);
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

        public async Task GuncelleAsync(KullaniciModel model, CancellationToken ct = default)
        {
            var kullanici = await _db.Kullanicilar
                .Include(k => k.OgretmenProfil)
                .Include(k => k.ServisProfil)
                .Include(k => k.VeliProfil)
                .FirstOrDefaultAsync(k => k.KullaniciId == model.KullaniciId, ct)
                ?? throw new KeyNotFoundException("Kullanıcı bulunamadı.");

            kullanici.KullaniciAdi = model.KullaniciAdi;
            kullanici.Rol = model.Rol;
            kullanici.KullaniciDurum = model.KullaniciDurum;
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

                if (model.OgretmenProfil?.GorselFile != null && model.OgretmenProfil.GorselFile.Length > 0)
                    profil.GorselPath = await SaveImageAsync(model.OgretmenProfil.GorselFile, ct);
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
            }

            if (!string.IsNullOrWhiteSpace(model.Sifre))
                kullanici.Sifre = _passwordHasher.HashPassword(kullanici, model.Sifre);

            await _db.SaveChangesAsync(ct);
        }

        public async Task SilAsync(int id, CancellationToken ct = default)
        {
            var kullanici = await _db.Kullanicilar.FindAsync([id], ct);
            if (kullanici == null) return;

            kullanici.KullaniciDurum = false;

            // Bağlı servis profili varsa durumunu pasife çek
            var servisProfil = await _db.ServisProfiller.FindAsync([id], ct);
            if (servisProfil != null)
                servisProfil.ServisDurum = false;

            // Bağlı öğretmen profili varsa durumunu pasife çek
            var ogretmenProfil = await _db.OgretmenProfiller.FindAsync([id], ct);
            if (ogretmenProfil != null)
                ogretmenProfil.OgretmenDurum = false;

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
            => await _db.ServisProfiller
                .AsNoTracking()
                .Where(s => s.ServisDurum)
                .OrderBy(s => s.Plaka)
                .Select(s => new SelectListItem
                {
                    Value = s.KullaniciId.ToString(),
                    Text = s.Plaka
                })
                .ToListAsync(ct);

        public async Task<List<SelectListItem>> GetServislerByIdSelectListAsync(int? selectedId = null, CancellationToken ct = default)
            => await _db.Kullanicilar
                .AsNoTracking()
                .Where(k => k.KullaniciDurum && k.Rol == KullaniciRolu.Servis)
                .OrderBy(k => k.KullaniciAdi)
                .Select(k => new SelectListItem
                {
                    Value = k.KullaniciId.ToString(),
                    Text = k.KullaniciAdi,
                    Selected = selectedId.HasValue && k.KullaniciId == selectedId.Value
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
