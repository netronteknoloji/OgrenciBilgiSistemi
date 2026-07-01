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
            // Not: Kart normalizasyonu (baştaki sıfırların atılması) Controller tarafında yapılır.
            var k = (kartNo ?? string.Empty).Trim();

            return await _context.Ogrenciler
                .AsNoTracking()
                .Include(o => o.Birim)
                .FirstOrDefaultAsync(o => o.OgrenciKartNo == k && !o.IsDeleted, ct);
        }

        public async Task<bool> YemekIzniVarMiAsync(int ogrenciId, int yil, int ay, CancellationToken ct = default)
        {
            var ayAktif = await _context.OgrenciYemekler.AsNoTracking()
                .AnyAsync(x => x.OgrenciId == ogrenciId && x.Yil == yil && x.Ay == ay && x.Aktif, ct);

            if (ayAktif)
                return true;

            return await _context.OgrenciYemekOdemeler.AsNoTracking()
                .AnyAsync(p => p.OgrenciId == ogrenciId && p.Yil == yil && p.Ay == ay && p.Tutar > 0m, ct);
        }

        public async Task<bool> BugunYemekGirisiVarMiAsync(int ogrenciId, DateTime today, DateTime tomorrow, CancellationToken ct = default)
        {
            return await _context.OgrenciDetaylar.AsNoTracking()
                .AnyAsync(l => l.OgrenciId == ogrenciId
                               && l.Cihaz != null
                               && l.Cihaz.IstasyonTipi == IstasyonTipi.Yemekhane
                               && ((l.OgrenciGTarih >= today && l.OgrenciGTarih < tomorrow)
                                   || (l.OgrenciCTarih >= today && l.OgrenciCTarih < tomorrow)), ct);
        }

        public async Task<(bool CikisVarMi, bool GirisVarMi)> BugunAnaKapiHareketleriAsync(
            int ogrenciId, DateTime today, DateTime tomorrow, CancellationToken ct = default)
        {
            var cikisVarMi = await _context.OgrenciDetaylar.AsNoTracking()
                .AnyAsync(l => l.OgrenciId == ogrenciId
                               && l.Cihaz != null
                               && l.Cihaz.IstasyonTipi == IstasyonTipi.AnaKapi
                               && l.OgrenciCTarih >= today && l.OgrenciCTarih < tomorrow, ct);

            var girisVarMi = await _context.OgrenciDetaylar.AsNoTracking()
                .AnyAsync(l => l.OgrenciId == ogrenciId
                               && l.Cihaz != null
                               && l.Cihaz.IstasyonTipi == IstasyonTipi.AnaKapi
                               && l.OgrenciGTarih >= today && l.OgrenciGTarih < tomorrow, ct);

            return (cikisVarMi, girisVarMi);
        }

        public Task<OgrenciBilgisiDto> OgrenciDtoHazirla(
            OgrenciModel ogrenci,
            OgrenciDetayModel log,
            CancellationToken ct = default)
        {
            const string GIRIS = "GİRİŞ";
            const string CIKIS = "ÇIKIŞ";

            // DB her zaman büyük harfle “GİRİŞ/ÇIKIŞ” yazıyor:
            bool isGiris = string.Equals(log.OgrenciGecisTipi, GIRIS, StringComparison.Ordinal);
            bool isCikis = string.Equals(log.OgrenciGecisTipi, CIKIS, StringComparison.Ordinal);

            // Saat alanını doğru taraftan çek
            var saat = (isGiris ? log.OgrenciGTarih : log.OgrenciCTarih)?.ToString("HH:mm") ?? "-";

            var dto = new OgrenciBilgisiDto
            {
                OgrenciAdSoyad = ogrenci.OgrenciAdSoyad,
                OgrenciNo = ogrenci.OgrenciNo,
                OgrenciSinif = ogrenci.Birim?.BirimAd ?? "-",
                OgrenciGorsel = ogrenci.OgrenciGorsel,
                OgrenciGirisSaati = isGiris ? saat : "-",
                OgrenciCikisSaati = isCikis ? saat : "-",
                OglenCikisDurumu = ogrenci.OgrenciCikisDurumu,
                GecisTipi = isGiris ? "Giriş" : (isCikis ? "Çıkış" : log.OgrenciGecisTipi ?? string.Empty),

                // Istasyon, CihazAdi, CihazKodu, Reason, Error, Info -> Controller tarafında set ediliyor.
            };

            return Task.FromResult(dto);
        }
    }
}