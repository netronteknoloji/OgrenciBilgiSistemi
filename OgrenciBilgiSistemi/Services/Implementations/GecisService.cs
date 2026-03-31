using System.Data;
using Microsoft.EntityFrameworkCore;
using OgrenciBilgiSistemi.Data;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Services.Interfaces;

namespace OgrenciBilgiSistemi.Services.Implementations
{
    public class GecisService : IGecisService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<GecisService> _logger;

        public GecisService(AppDbContext db, ILogger<GecisService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<GecisKayitSonucu> KaydetAsync(
    int cihazId, int ogrenciId, IstasyonTipi istasyon, DateTime now,
    CancellationToken ct = default, string? gecisTipi = null)
        {
            // Cihaz ve öğrenci aktif mi?
            var cihaz = await _db.Cihazlar
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CihazId == cihazId && c.Aktif, ct);

            if (cihaz is null)
                throw new InvalidOperationException("Cihaz pasif veya bulunamadı.");

            var ogrenciAktifMi = await _db.Ogrenciler
                .AsNoTracking()
                .AnyAsync(o => o.OgrenciId == ogrenciId && o.OgrenciDurum, ct);

            if (!ogrenciAktifMi)
                throw new InvalidOperationException("Öğrenci pasif veya bulunamadı.");

            var dayStart = now.Date;
            var dayEnd = dayStart.AddDays(1);

            var strategy = _db.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

                bool nextIsEntry;
                if (!string.IsNullOrEmpty(gecisTipi))
                {
                    nextIsEntry = string.Equals(gecisTipi, "Giriş", StringComparison.OrdinalIgnoreCase);
                }
                else
                {
                    // Bugün bu istasyondaki son kaydı COALESCE(GTarih, CTarih) ile bul
                    var last = await _db.OgrenciDetaylar
                        .Where(x => x.OgrenciId == ogrenciId
                                    && x.IstasyonTipi == istasyon
                                    && (
                                        (x.OgrenciGTarih >= dayStart && x.OgrenciGTarih < dayEnd)
                                        || (x.OgrenciCTarih >= dayStart && x.OgrenciCTarih < dayEnd)
                                    ))
                        .OrderByDescending(x => x.OgrenciGTarih ?? x.OgrenciCTarih)
                        .FirstOrDefaultAsync(ct);

                    if (last is null)
                    {
                        nextIsEntry = true; // bugün ilk kayıt => giriş
                    }
                    else
                    {
                        // Son kayıt "açık giriş" ise şimdi çıkış, değilse giriş
                        var lastWasOpenEntry = last.OgrenciGTarih.HasValue && !last.OgrenciCTarih.HasValue;
                        nextIsEntry = !lastWasOpenEntry;
                    }
                }

                // DB'ye BÜYÜK, sonuç objesine normal yaz
                var gecisTipiDb = nextIsEntry ? "GİRİŞ" : "ÇIKIŞ";   // <-- DB'ye böyle kaydedilecek
                var gecisTipiResult = nextIsEntry ? "Giriş" : "Çıkış";   // <-- Geri dönüş/ekranda kullanım

                var log = new OgrenciDetayModel
                {
                    OgrenciId = ogrenciId,
                    IstasyonTipi = istasyon,
                    CihazId = cihazId,
                    OgrenciGecisTipi = gecisTipiDb,
                    OgrenciGTarih = nextIsEntry ? now : null,
                    OgrenciCTarih = nextIsEntry ? null : now
                };

                _db.OgrenciDetaylar.Add(log);

                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
                return new GecisKayitSonucu(gecisTipiResult, now);
            });
        }
    }
}