using Microsoft.EntityFrameworkCore;
using OgrenciBilgiSistemi.Data;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Push;

namespace OgrenciBilgiSistemi.Services.Implementations
{
    public class EfBildirimTokenDeposu : IBildirimTokenDeposu
    {
        private readonly AppDbContext _db;

        public EfBildirimTokenDeposu(AppDbContext db)
        {
            _db = db;
        }

        public async Task UpsertAsync(BildirimCihazKaydi kayit, CancellationToken ct = default)
        {
            var simdi = DateTime.Now;

            var mevcut = await _db.BildirimCihazlari
                .FirstOrDefaultAsync(c => c.FcmToken == kayit.FcmToken && !c.IsDeleted, ct);

            if (mevcut is null)
            {
                _db.BildirimCihazlari.Add(new BildirimCihaziModel
                {
                    KullaniciId = kayit.KullaniciId,
                    FcmToken = kayit.FcmToken,
                    Platform = kayit.Platform,
                    UygulamaSurumu = kayit.UygulamaSurumu,
                    CihazModeli = kayit.CihazModeli,
                    OlusturulmaTarihi = simdi,
                    SonGuncelleme = simdi,
                    IsDeleted = false
                });
            }
            else
            {
                mevcut.KullaniciId = kayit.KullaniciId;
                mevcut.Platform = kayit.Platform;
                mevcut.UygulamaSurumu = kayit.UygulamaSurumu;
                mevcut.CihazModeli = kayit.CihazModeli;
                mevcut.SonGuncelleme = simdi;
            }

            await _db.SaveChangesAsync(ct);
        }

        public async Task IptalAsync(IEnumerable<string> tokenlar, CancellationToken ct = default)
        {
            var liste = tokenlar.ToList();
            if (liste.Count == 0) return;

            var simdi = DateTime.Now;
            await _db.BildirimCihazlari
                .Where(c => liste.Contains(c.FcmToken) && !c.IsDeleted)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(c => c.IsDeleted, true)
                    .SetProperty(c => c.SonGuncelleme, simdi), ct);
        }

        public async Task<IReadOnlyList<BildirimTokenKaydi>> AktifTokenleriGetirAsync(int kullaniciId, CancellationToken ct = default)
        {
            return await _db.BildirimCihazlari
                .AsNoTracking()
                .Where(c => c.KullaniciId == kullaniciId && !c.IsDeleted)
                .Select(c => new BildirimTokenKaydi(c.FcmToken, c.Platform))
                .ToListAsync(ct);
        }
    }
}
