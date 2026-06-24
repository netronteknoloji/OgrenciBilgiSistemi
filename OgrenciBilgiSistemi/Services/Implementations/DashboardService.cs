using Microsoft.EntityFrameworkCore;
using OgrenciBilgiSistemi.Data;
using OgrenciBilgiSistemi.Dtos;
using OgrenciBilgiSistemi.Services.Interfaces;

namespace OgrenciBilgiSistemi.Services.Implementations
{
    public class DashboardService : IDashboardService
    {
        private readonly AppDbContext _db;
        public DashboardService(AppDbContext db) { _db = db; }

        public async Task<DashboardStatsDto> GetStatsAsync(CancellationToken ct = default)
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var bugunYemekhaneGiris = await _db.OgrenciDetaylar.AsNoTracking()
                .Where(x => x.IstasyonTipi == IstasyonTipi.Yemekhane
                         && x.OgrenciGecisTipi == "GİRİŞ"
                         && x.OgrenciGTarih != null
                         && x.OgrenciGTarih >= today
                         && x.OgrenciGTarih < tomorrow)
                .CountAsync(ct);

            var bugunAnakapiCikis = await _db.OgrenciDetaylar.AsNoTracking()
                .Where(x => x.IstasyonTipi == IstasyonTipi.AnaKapi
                         && x.OgrenciGecisTipi == "ÇIKIŞ"
                         && x.OgrenciCTarih != null
                         && x.OgrenciCTarih >= today
                         && x.OgrenciCTarih < tomorrow)
                .CountAsync(ct);

            var toplamOgrenci = await _db.Ogrenciler.AsNoTracking().CountAsync(ct);

            return new DashboardStatsDto
            {
                ToplamOgrenci = toplamOgrenci,
                BugunYemekhaneGiris = bugunYemekhaneGiris,
                BugunAnakapiCikis = bugunAnakapiCikis
            };
        }

        public async Task<DashboardSeriesDto> GetSeriesAsync(int yil, int ay, CancellationToken ct = default)
        {
            var start = new DateTime(yil, ay, 1);
            var end = start.AddMonths(1);
            int days = DateTime.DaysInMonth(yil, ay);

            var ymk = await _db.OgrenciDetaylar.AsNoTracking()
                .Where(x => x.IstasyonTipi == IstasyonTipi.Yemekhane
                         && x.OgrenciGecisTipi == "GİRİŞ"
                         && x.OgrenciGTarih != null
                         && x.OgrenciGTarih >= start
                         && x.OgrenciGTarih < end)
                .GroupBy(x => x.OgrenciGTarih!.Value.Date)
                .Select(g => new { Gun = g.Key, Adet = g.Count() })
                .ToListAsync(ct);

            var ank = await _db.OgrenciDetaylar.AsNoTracking()
                .Where(x => x.IstasyonTipi == IstasyonTipi.AnaKapi
                         && x.OgrenciGecisTipi == "ÇIKIŞ"
                         && x.OgrenciCTarih != null
                         && x.OgrenciCTarih >= start
                         && x.OgrenciCTarih < end)
                .GroupBy(x => x.OgrenciCTarih!.Value.Date)
                .Select(g => new { Gun = g.Key, Adet = g.Count() })
                .ToListAsync(ct);

            var ymkMap = ymk.ToDictionary(x => x.Gun, x => x.Adet);
            var ankMap = ank.ToDictionary(x => x.Gun, x => x.Adet);

            var dto = new DashboardSeriesDto { Yil = yil, Ay = ay };

            for (int d = 1; d <= days; d++)
            {
                var day = new DateTime(yil, ay, d);
                dto.GunEtiketleri.Add(d.ToString());
                dto.YemekhaneGiris.Add(ymkMap.TryGetValue(day, out var yg) ? yg : 0);
                dto.AnakapiCikis.Add(ankMap.TryGetValue(day, out var ac) ? ac : 0);
            }

            return dto;
        }
    }
}
