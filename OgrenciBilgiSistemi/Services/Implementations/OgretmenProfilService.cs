using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OgrenciBilgiSistemi.Data;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Services.Interfaces;
using OgrenciBilgiSistemi.Shared.Enums;
using OgrenciBilgiSistemi.ViewModels;

namespace OgrenciBilgiSistemi.Services.Implementations
{
    public sealed class OgretmenProfilService : IOgretmenProfilService
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly PasswordHasher<KullaniciModel> _passwordHasher = new();

        public OgretmenProfilService(AppDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        public async Task<SayfalanmisListeModel<OgretmenProfilModel>> SearchPagedAsync(
            string? searchString, int page, int pageSize = 20, CancellationToken ct = default)
        {
            var query = _db.OgretmenProfiller
                .Include(o => o.Kullanici)
                .Include(o => o.Birim)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var s = searchString.Trim();
                query = query.Where(o =>
                    o.Kullanici.KullaniciAdi.Contains(s) ||
                    (o.Email != null && o.Email.Contains(s)) ||
                    (o.Birim != null && o.Birim.BirimAd.Contains(s)));
            }

            return await SayfalanmisListeModel<OgretmenProfilModel>
                .CreateAsync(query.OrderBy(o => o.Kullanici.KullaniciAdi), page, pageSize, ct);
        }

        public async Task<int> EkleKullaniciVeProfilAsync(OgretmenEkleVm vm, CancellationToken ct = default)
        {
            var profil = new OgretmenProfilModel
            {
                Email = vm.Email,
                BirimId = vm.BirimId,
                OgretmenDurum = true
            };

            if (vm.GorselFile != null && vm.GorselFile.Length > 0)
                profil.GorselPath = await SaveImageAsync(vm.GorselFile, ct);

            var kullanici = new KullaniciModel
            {
                KullaniciAdi = vm.KullaniciAdi,
                Rol = KullaniciRolu.Ogretmen,
                Telefon = vm.Telefon,
                KullaniciDurum = true,
                OgretmenProfil = profil
            };

            kullanici.Sifre = _passwordHasher.HashPassword(kullanici, vm.Sifre);

            _db.Kullanicilar.Add(kullanici);
            await _db.SaveChangesAsync(ct);
            return kullanici.KullaniciId;
        }

        public async Task GuncelleAsync(OgretmenProfilModel model, CancellationToken ct = default)
        {
            var mevcut = await _db.OgretmenProfiller.FindAsync([model.KullaniciId], ct)
                ?? throw new KeyNotFoundException("Öğretmen profili bulunamadı.");

            mevcut.BirimId = model.BirimId;
            mevcut.Email = model.Email;
            mevcut.OgretmenDurum = model.OgretmenDurum;

            if (model.GorselFile != null && model.GorselFile.Length > 0)
                mevcut.GorselPath = await SaveImageAsync(model.GorselFile, ct);

            await _db.SaveChangesAsync(ct);
        }

        public async Task SilAsync(int kullaniciId, CancellationToken ct = default)
        {
            var profil = await _db.OgretmenProfiller.FindAsync([kullaniciId], ct);
            if (profil == null) return;

            profil.OgretmenDurum = false;
            await _db.SaveChangesAsync(ct);
        }

        public async Task<OgretmenProfilModel?> GetByIdAsync(int kullaniciId, CancellationToken ct = default)
            => await _db.OgretmenProfiller
                .Include(o => o.Birim)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.KullaniciId == kullaniciId, ct);

        private async Task<string> SaveImageAsync(IFormFile file, CancellationToken ct)
        {
            var root = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads", "ogretmen");
            Directory.CreateDirectory(root);

            var ext = Path.GetExtension(file.FileName);
            var name = $"ogr_{Guid.NewGuid():N}{ext}";
            var full = Path.Combine(root, name);

            using (var fs = new FileStream(full, FileMode.Create))
                await file.CopyToAsync(fs, ct);

            var rel = Path.Combine("uploads", "ogretmen", name).Replace("\\", "/");
            return "/" + rel;
        }
    }
}
