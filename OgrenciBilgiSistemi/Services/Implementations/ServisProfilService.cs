using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OgrenciBilgiSistemi.Data;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Services.Interfaces;
using OgrenciBilgiSistemi.Shared.Enums;
using OgrenciBilgiSistemi.ViewModels;

namespace OgrenciBilgiSistemi.Services.Implementations
{
    public sealed class ServisProfilService : IServisProfilService
    {
        private readonly AppDbContext _db;
        private readonly PasswordHasher<KullaniciModel> _passwordHasher = new();

        public ServisProfilService(AppDbContext db) => _db = db;

        public async Task<SayfalanmisListeModel<ServisProfilModel>> SearchPagedAsync(
            string? searchString, int page, int pageSize = 20, CancellationToken ct = default)
        {
            var query = _db.ServisProfiller
                .Include(s => s.Kullanici)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var s = searchString.Trim();
                query = query.Where(sp => sp.Plaka.Contains(s) ||
                    sp.Kullanici.KullaniciAdi.Contains(s));
            }

            var paged = await SayfalanmisListeModel<ServisProfilModel>
                .CreateAsync(query.OrderBy(s => s.Plaka), page, pageSize, ct);

            // Her servisin öğrenci sayısını doldur
            var servisIdler = paged.Select(s => s.KullaniciId).ToList();
            var sayilar = await _db.Ogrenciler
                .Where(o => o.ServisId != null && servisIdler.Contains(o.ServisId.Value) && o.OgrenciDurum)
                .GroupBy(o => o.ServisId!.Value)
                .Select(g => new { ServisId = g.Key, Sayi = g.Count() })
                .ToDictionaryAsync(x => x.ServisId, x => x.Sayi, ct);

            foreach (var s in paged)
                s.OgrenciSayisi = sayilar.GetValueOrDefault(s.KullaniciId, 0);

            return paged;
        }

        public async Task<int> EkleKullaniciVeProfilAsync(ServisEkleVm vm, CancellationToken ct = default)
        {
            var kullanici = new KullaniciModel
            {
                KullaniciAdi = vm.KullaniciAdi,
                Rol = KullaniciRolu.Servis,
                Telefon = vm.Telefon,
                KullaniciDurum = true,
                ServisProfil = new ServisProfilModel
                {
                    Plaka = vm.Plaka,
                    ServisDurum = true
                }
            };

            kullanici.Sifre = _passwordHasher.HashPassword(kullanici, vm.Sifre);

            _db.Kullanicilar.Add(kullanici);
            await _db.SaveChangesAsync(ct);
            return kullanici.KullaniciId;
        }

        public async Task GuncelleAsync(ServisProfilModel model, CancellationToken ct = default)
        {
            var mevcut = await _db.ServisProfiller.FindAsync([model.KullaniciId], ct)
                ?? throw new KeyNotFoundException("Servis profili bulunamadı.");

            mevcut.Plaka = model.Plaka;
            mevcut.ServisDurum = model.ServisDurum;

            await _db.SaveChangesAsync(ct);
        }

        public async Task SilAsync(int kullaniciId, CancellationToken ct = default)
        {
            var profil = await _db.ServisProfiller.FindAsync([kullaniciId], ct);
            if (profil == null) return;

            profil.ServisDurum = false;
            await _db.SaveChangesAsync(ct);
        }

        public async Task<ServisProfilModel?> GetByIdAsync(int kullaniciId, CancellationToken ct = default)
            => await _db.ServisProfiller
                .Include(s => s.Kullanici)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.KullaniciId == kullaniciId, ct);

        public async Task<List<OgrenciModel>> GetOgrencilerAsync(int kullaniciId, CancellationToken ct = default)
            => await _db.Ogrenciler
                .Include(o => o.Birim)
                .Where(o => o.ServisId == kullaniciId && o.OgrenciDurum)
                .OrderBy(o => o.OgrenciAdSoyad)
                .AsNoTracking()
                .ToListAsync(ct);
    }
}
