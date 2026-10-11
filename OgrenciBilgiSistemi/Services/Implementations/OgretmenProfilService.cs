using OgrenciBilgiSistemi.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OgrenciBilgiSistemi.Data;
using OgrenciBilgiSistemi.Helpers;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Services.Interfaces;
using OgrenciBilgiSistemi.ViewModels;

namespace OgrenciBilgiSistemi.Services.Implementations
{
    public sealed class OgretmenProfilService : IOgretmenProfilService
    {
        private readonly AppDbContext _db;
        private readonly IFileStorage _files;
        private readonly ILogger<OgretmenProfilService> _logger;
        private readonly PasswordHasher<KullaniciModel> _passwordHasher = new();

        public OgretmenProfilService(AppDbContext db, IFileStorage files, ILogger<OgretmenProfilService> logger)
        {
            _db = db;
            _files = files;
            _logger = logger;
        }

        public async Task<SayfalanmisListeModel<OgretmenProfilModel>> SearchPagedAsync(
            string? searchString, int page, int pageSize = 20,
            OgretmenFiltre filtre = OgretmenFiltre.Aktif, CancellationToken ct = default)
        {
            var query = _db.OgretmenProfiller
                .Include(o => o.Kullanici)
                .Include(o => o.Birim)
                .AsNoTracking()
                .AsQueryable();

            query = filtre switch
            {
                OgretmenFiltre.Aktif => query.Where(o => !o.IsDeleted),
                OgretmenFiltre.Pasif => query.Where(o => o.IsDeleted),
                _ => query
            };

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
                IsDeleted = false
            };

            if (vm.GorselFile != null && vm.GorselFile.Length > 0)
                profil.GorselPath = await _files.SaveImageAsync(vm.GorselFile, null, ct, "ogretmen");

            var kullanici = new KullaniciModel
            {
                KullaniciAdi = vm.KullaniciAdi,
                Rol = KullaniciRolu.Ogretmen,
                Telefon = vm.Telefon,
                IsDeleted = false,
                OgretmenProfil = profil
            };

            kullanici.Sifre = _passwordHasher.HashPassword(kullanici, vm.Sifre);

            try
            {
                _db.Kullanicilar.Add(kullanici);
                await _db.SaveChangesAsync(ct);
                await OgretmenAtamaHelper.OgretmenDegistiAsync(_db, kullanici.KullaniciId, [profil.BirimId], ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Öğretmen kullanıcısı eklenirken hata. KullaniciAdi: {Ad}", vm.KullaniciAdi);
                throw;
            }
            return kullanici.KullaniciId;
        }

        public async Task GuncelleAsync(OgretmenProfilModel model, string? kullaniciAdi, string? telefon, string? sifre, CancellationToken ct = default)
        {
            var mevcut = await _db.OgretmenProfiller.FindAsync([model.KullaniciId], ct)
                ?? throw new KeyNotFoundException("Öğretmen profili bulunamadı.");

            var eskiBirimId = mevcut.BirimId;
            mevcut.BirimId = model.BirimId;
            mevcut.Email = model.Email;
            mevcut.IsDeleted = model.IsDeleted;

            if (model.GorselFile != null && model.GorselFile.Length > 0)
                mevcut.GorselPath = await _files.SaveImageAsync(model.GorselFile, mevcut.GorselPath, ct, "ogretmen");

            var kullanici = await _db.Kullanicilar.FindAsync([model.KullaniciId], ct);
            if (kullanici != null)
            {
                kullanici.KullaniciAdi = kullaniciAdi ?? kullanici.KullaniciAdi;
                kullanici.Telefon = telefon;
                kullanici.IsDeleted = model.IsDeleted;

                if (!string.IsNullOrWhiteSpace(sifre))
                    kullanici.Sifre = _passwordHasher.HashPassword(kullanici, sifre);
            }

            try
            {
                await _db.SaveChangesAsync(ct);
                await OgretmenAtamaHelper.OgretmenDegistiAsync(_db, model.KullaniciId, [eskiBirimId, mevcut.BirimId], ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Öğretmen profili güncellenirken hata. Id: {Id}", model.KullaniciId);
                throw;
            }
        }

        public async Task SilAsync(int kullaniciId, CancellationToken ct = default)
        {
            var profil = await _db.OgretmenProfiller.FindAsync([kullaniciId], ct);
            if (profil == null) return;

            profil.IsDeleted = true;

            // Kullanıcı hesabı da pasiflenir; aksi halde öğretmen giriş yapmaya devam eder.
            var kullanici = await _db.Kullanicilar.FindAsync([kullaniciId], ct);
            if (kullanici != null)
                kullanici.IsDeleted = true;

            try
            {
                await _db.SaveChangesAsync(ct);
                await OgretmenAtamaHelper.OgretmenDegistiAsync(_db, kullaniciId, [profil.BirimId], ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Öğretmen pasifleştirilirken hata. Id: {Id}", kullaniciId);
                throw;
            }
        }

        public async Task<OgretmenProfilModel?> GetByIdAsync(int kullaniciId, CancellationToken ct = default)
            => await _db.OgretmenProfiller
                .Include(o => o.Birim)
                .Include(o => o.Kullanici)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.KullaniciId == kullaniciId, ct);

    }
}
