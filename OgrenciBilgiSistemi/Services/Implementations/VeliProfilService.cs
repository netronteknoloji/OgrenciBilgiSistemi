using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OgrenciBilgiSistemi.Data;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Services.Interfaces;
using OgrenciBilgiSistemi.Shared.Enums;
using OgrenciBilgiSistemi.ViewModels;

namespace OgrenciBilgiSistemi.Services.Implementations
{
    public sealed class VeliProfilService : IVeliProfilService
    {
        private readonly AppDbContext _db;
        private readonly PasswordHasher<KullaniciModel> _passwordHasher = new();

        public VeliProfilService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<SayfalanmisListeModel<VeliProfilModel>> SearchPagedAsync(
            string? searchString, int page, int pageSize = 20, CancellationToken ct = default)
        {
            var query = _db.VeliProfiller
                .Include(v => v.Kullanici)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var s = searchString.Trim();
                query = query.Where(v => v.Kullanici.KullaniciAdi.Contains(s) ||
                    (v.Kullanici.Telefon != null && v.Kullanici.Telefon.Contains(s)));
            }

            var paged = await SayfalanmisListeModel<VeliProfilModel>
                .CreateAsync(query.OrderBy(v => v.Kullanici.KullaniciAdi), page, pageSize, ct);

            // Her velinin öğrenci sayısını doldur
            var veliIdler = paged.Select(v => v.KullaniciId).ToList();
            var sayilar = await _db.Ogrenciler
                .Where(o => o.VeliId != null && veliIdler.Contains(o.VeliId.Value) && o.OgrenciDurum)
                .GroupBy(o => o.VeliId!.Value)
                .Select(g => new { VeliId = g.Key, Sayi = g.Count() })
                .ToDictionaryAsync(x => x.VeliId, x => x.Sayi, ct);

            foreach (var v in paged)
                v.OgrenciSayisi = sayilar.GetValueOrDefault(v.KullaniciId, 0);

            return paged;
        }

        public async Task<int> EkleKullaniciVeProfilAsync(VeliEkleVm vm, CancellationToken ct = default)
        {
            var kullanici = new KullaniciModel
            {
                KullaniciAdi = vm.KullaniciAdi,
                Rol = KullaniciRolu.Veli,
                Telefon = vm.Telefon,
                KullaniciDurum = true,
                VeliProfil = new VeliProfilModel
                {
                    VeliAdres = vm.VeliAdres,
                    VeliMeslek = vm.VeliMeslek,
                    VeliIsYeri = vm.VeliIsYeri,
                    VeliEmail = vm.VeliEmail,
                    VeliYakinlik = vm.VeliYakinlik,
                    VeliDurum = true
                }
            };

            kullanici.Sifre = _passwordHasher.HashPassword(kullanici, vm.Sifre);

            _db.Kullanicilar.Add(kullanici);
            await _db.SaveChangesAsync(ct);
            return kullanici.KullaniciId;
        }

        public async Task GuncelleAsync(VeliProfilModel model, CancellationToken ct = default)
        {
            var mevcut = await _db.VeliProfiller.FindAsync([model.KullaniciId], ct)
                ?? throw new KeyNotFoundException("Veli profili bulunamadı.");

            mevcut.VeliAdres = model.VeliAdres;
            mevcut.VeliMeslek = model.VeliMeslek;
            mevcut.VeliIsYeri = model.VeliIsYeri;
            mevcut.VeliEmail = model.VeliEmail;
            mevcut.VeliYakinlik = model.VeliYakinlik;
            mevcut.VeliDurum = model.VeliDurum;

            await _db.SaveChangesAsync(ct);
        }

        public async Task SilAsync(int kullaniciId, CancellationToken ct = default)
        {
            var veli = await _db.VeliProfiller
                .FirstOrDefaultAsync(v => v.KullaniciId == kullaniciId, ct);

            if (veli is null) return;

            veli.VeliDurum = false;
            await _db.SaveChangesAsync(ct);
        }

        public async Task<VeliProfilModel?> GetByIdAsync(int kullaniciId, CancellationToken ct = default)
        {
            return await _db.VeliProfiller
                .Include(v => v.Kullanici)
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.KullaniciId == kullaniciId, ct);
        }

        public async Task<List<OgrenciModel>> GetOgrencilerAsync(int kullaniciId, CancellationToken ct = default)
            => await _db.Ogrenciler
                .Include(o => o.Birim)
                .Where(o => o.VeliId == kullaniciId && o.OgrenciDurum)
                .OrderBy(o => o.OgrenciAdSoyad)
                .AsNoTracking()
                .ToListAsync(ct);
    }
}
