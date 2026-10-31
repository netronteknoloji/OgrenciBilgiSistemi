using Microsoft.EntityFrameworkCore;
using OgrenciBilgiSistemi.Data;
using OgrenciBilgiSistemi.Dtos;
using OgrenciBilgiSistemi.Models;

namespace OgrenciBilgiSistemi.Services.Implementations
{
    public class KartOkuService : IKartOkuService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<KartOkuService> _logger;

        public KartOkuService(AppDbContext context, ILogger<KartOkuService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<OgrenciModel?> GetOgrenciByKartNoAsync(string kartNo, CancellationToken ct = default)
        {
            // Not: Kart normalizasyonu (KartNo.Normalize) çağıran tarafta yapılır.
            var k = (kartNo ?? string.Empty).Trim();

            return await _context.Ogrenciler
                .AsNoTracking()
                .Include(o => o.Birim)
                .FirstOrDefaultAsync(o => o.OgrenciKartNo == k && !o.IsDeleted, ct);
        }

        // Hak tanımı tek yerde (YemekHakki); ödeme hak vermez
        public Task<bool> YemekIzniVarMiAsync(int ogrenciId, int yil, int ay, CancellationToken ct = default)
            => YemekHakki.HakkiOlanlar(_context, yil, ay).AnyAsync(id => id == ogrenciId, ct);

        public async Task<bool> BugunYemekGirisiVarMiAsync(int ogrenciId, DateTime today, DateTime tomorrow, CancellationToken ct = default)
        {
            return await _context.OgrenciDetaylar.AsNoTracking()
                .AnyAsync(l => l.OgrenciId == ogrenciId
                               && l.Cihaz != null
                               && l.Cihaz.IstasyonTipi == IstasyonTipi.Yemekhane
                               && ((l.OgrenciGTarih >= today && l.OgrenciGTarih < tomorrow)
                                   || (l.OgrenciCTarih >= today && l.OgrenciCTarih < tomorrow)), ct);
        }

        /// <summary>
        /// Bugünkü Ana Kapı öğle hareketleri: ilk çıkış ve o çıkıştan SONRAKİ giriş (öğle dönüşü).
        /// Çıkıştan önceki girişler dönüş sayılmaz.
        /// </summary>
        public async Task<(bool CikisVarMi, bool GirisVarMi, DateTime? CikisZamani)> BugunAnaKapiHareketleriAsync(
            int ogrenciId, DateTime today, DateTime tomorrow, CancellationToken ct = default)
        {
            var cikisZamani = await _context.OgrenciDetaylar.AsNoTracking()
                .Where(l => l.OgrenciId == ogrenciId
                            && l.Cihaz != null
                            && l.Cihaz.IstasyonTipi == IstasyonTipi.AnaKapi
                            && l.OgrenciCTarih >= today && l.OgrenciCTarih < tomorrow)
                .MinAsync(l => l.OgrenciCTarih, ct);

            if (cikisZamani is null)
                return (false, false, null);

            var girisVarMi = await _context.OgrenciDetaylar.AsNoTracking()
                .AnyAsync(l => l.OgrenciId == ogrenciId
                               && l.Cihaz != null
                               && l.Cihaz.IstasyonTipi == IstasyonTipi.AnaKapi
                               && l.OgrenciGTarih > cikisZamani && l.OgrenciGTarih < tomorrow, ct);

            return (true, girisVarMi, cikisZamani);
        }

    }
}