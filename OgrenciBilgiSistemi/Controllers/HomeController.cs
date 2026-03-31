using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OgrenciBilgiSistemi.Data;
using OgrenciBilgiSistemi.Dtos;

public class HomeController : Controller
{
    private readonly AppDbContext _db;
    public HomeController(AppDbContext db) { _db = db; }

    public IActionResult Index() => View();

    // --- KPI'lar (bugünün özeti) ---
    [HttpGet]
    public async Task<IActionResult> DashboardStats()
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);

        // BUGÜN Yemekhane GİRİŞ: Istasyon=Yemekhane + Tip=GİRİŞ + OgrenciGTarih aralığı
        var bugunYemekhaneGiris = await _db.OgrenciDetaylar.AsNoTracking()
            .Where(x => x.IstasyonTipi == IstasyonTipi.Yemekhane
                     && x.OgrenciGecisTipi == "GİRİŞ"
                     && x.OgrenciGTarih != null
                     && x.OgrenciGTarih! >= today
                     && x.OgrenciGTarih! < tomorrow)
            .CountAsync();

        // BUGÜN Anakapı ÇIKIŞ: Istasyon=AnaKapi + Tip=ÇIKIŞ + OgrenciCTarih aralığı
        var bugunAnakapiCikis = await _db.OgrenciDetaylar.AsNoTracking()
            .Where(x => x.IstasyonTipi == IstasyonTipi.AnaKapi
                     && x.OgrenciGecisTipi == "ÇIKIŞ"
                     && x.OgrenciCTarih != null
                     && x.OgrenciCTarih! >= today
                     && x.OgrenciCTarih! < tomorrow)
            .CountAsync();

        var toplamOgrenci = await _db.Ogrenciler.AsNoTracking().CountAsync();

        return Json(new DashboardStatsDto
        {
            ToplamOgrenci = toplamOgrenci,
            BugunYemekhaneGiris = bugunYemekhaneGiris,
            BugunAnakapiCikis = bugunAnakapiCikis
        });
    }

    // --- Çizgi grafik: içinde bulunulan ayın günlerine göre (Yemekhane GİRİŞ / Anakapı ÇIKIŞ) ---
    [HttpGet]
    public async Task<IActionResult> DashboardSeries(int? yil, int? ay)
    {
        var now = DateTime.Now;
        int y = yil ?? now.Year;
        int m = ay ?? now.Month;

        var start = new DateTime(y, m, 1);
        var end = start.AddMonths(1);
        int days = DateTime.DaysInMonth(y, m);

        var ymk = await _db.OgrenciDetaylar.AsNoTracking()
            .Where(x => x.IstasyonTipi == IstasyonTipi.Yemekhane
                     && x.OgrenciGecisTipi == "GİRİŞ"
                     && x.OgrenciGTarih != null
                     && x.OgrenciGTarih! >= start
                     && x.OgrenciGTarih! < end)
            .GroupBy(x => new { d = x.OgrenciGTarih!.Value.Date })
            .Select(g => new { Gun = g.Key.d, Adet = g.Count() })
            .ToListAsync();

        var ank = await _db.OgrenciDetaylar.AsNoTracking()
            .Where(x => x.IstasyonTipi == IstasyonTipi.AnaKapi
                     && x.OgrenciGecisTipi == "ÇIKIŞ"
                     && x.OgrenciCTarih != null
                     && x.OgrenciCTarih! >= start
                     && x.OgrenciCTarih! < end)
            .GroupBy(x => new { d = x.OgrenciCTarih!.Value.Date })
            .Select(g => new { Gun = g.Key.d, Adet = g.Count() })
            .ToListAsync();

        // Gün bazlı dizileri doldur
        var ymkMap = ymk.ToDictionary(x => x.Gun, x => x.Adet);
        var ankMap = ank.ToDictionary(x => x.Gun, x => x.Adet);

        var dto = new DashboardSeriesDto { Yil = y, Ay = m };

        for (int d = 1; d <= days; d++)
        {
            var day = new DateTime(y, m, d);
            dto.GunEtiketleri.Add(d.ToString());
            dto.YemekhaneGiris.Add(ymkMap.TryGetValue(day, out var yg) ? yg : 0);
            dto.AnakapiCikis.Add(ankMap.TryGetValue(day, out var ac) ? ac : 0);
        }

        return Json(dto);
    }
}