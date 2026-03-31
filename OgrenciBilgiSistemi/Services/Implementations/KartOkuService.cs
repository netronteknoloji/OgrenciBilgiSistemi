using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OgrenciBilgiSistemi.Data;
using OgrenciBilgiSistemi.Dtos;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Services.Interfaces;

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
                .FirstOrDefaultAsync(o => o.OgrenciKartNo == k && o.OgrenciDurum, ct);
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