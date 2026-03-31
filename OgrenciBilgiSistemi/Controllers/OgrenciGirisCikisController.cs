using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OgrenciBilgiSistemi.Data;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.ViewModels;

namespace OgrenciBilgiSistemi.Controllers
{
    public class OgrenciGirisCikisController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<OgrenciGirisCikisController> _logger;

        public OgrenciGirisCikisController(AppDbContext context, ILogger<OgrenciGirisCikisController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Detay(
            string? sortOrder,
            string? searchString,
            int page = 1,
            IstasyonTipi? istasyonTipi = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            CancellationToken ct = default)
        {
            // Sayfa guard
            if (page < 1) page = 1;

            ViewData["CurrentSort"] = sortOrder;
            ViewData["CurrentFilter"] = searchString;
            ViewData["StartDate"] = startDate?.ToString("yyyy-MM-dd");
            ViewData["EndDate"] = endDate?.ToString("yyyy-MM-dd");
            ViewData["IstasyonTipi"] = istasyonTipi.HasValue ? ((int)istasyonTipi.Value).ToString() : "";

            // Ana sorgu
            var q = _context.OgrenciDetaylar
                .AsNoTracking()
                .Include(x => x.Ogrenci).ThenInclude(o => o.Birim)
                .Include(x => x.Cihaz)
                .AsQueryable();

            // Tarih aralığı
            if (startDate.HasValue)
            {
                var s = startDate.Value.Date;
                q = q.Where(x => (x.OgrenciGTarih ?? x.OgrenciCTarih) >= s);
            }
            if (endDate.HasValue)
            {
                var e = endDate.Value.Date.AddDays(1);
                q = q.Where(x => (x.OgrenciGTarih ?? x.OgrenciCTarih) < e);
            }

            // İstasyon filtresi
            if (istasyonTipi.HasValue)
                q = q.Where(x => x.IstasyonTipi == istasyonTipi.Value);

            // Arama: AdSoyad / No / KartNo
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var s = searchString.Trim();

                if (int.TryParse(s, out var no))
                {
                    q = q.Where(x =>
                        (x.Ogrenci != null && x.Ogrenci.OgrenciNo == no) ||
                        (x.Ogrenci != null && x.Ogrenci.OgrenciKartNo != null && (
                            x.Ogrenci.OgrenciKartNo == s ||
                            EF.Functions.Like(x.Ogrenci.OgrenciKartNo, $"%{s}%")
                        )) ||
                        (x.Ogrenci != null && x.Ogrenci.OgrenciAdSoyad != null && (
                            EF.Functions.Like(EF.Functions.Collate(x.Ogrenci.OgrenciAdSoyad, "Turkish_100_CI_AI"), $"%{s}%") ||
                            EF.Functions.Like(EF.Functions.Collate(x.Ogrenci.OgrenciAdSoyad, "Latin1_General_CI_AI"), $"%{s}%")
                        )));
                }
                else
                {
                    q = q.Where(x =>
                        x.Ogrenci != null && (
                            (x.Ogrenci.OgrenciAdSoyad != null && (
                                EF.Functions.Like(EF.Functions.Collate(x.Ogrenci.OgrenciAdSoyad, "Turkish_100_CI_AI"), $"%{s}%") ||
                                EF.Functions.Like(EF.Functions.Collate(x.Ogrenci.OgrenciAdSoyad, "Latin1_General_CI_AI"), $"%{s}%")
                            )) ||
                            (x.Ogrenci.OgrenciKartNo != null &&
                                EF.Functions.Like(x.Ogrenci.OgrenciKartNo, $"%{s}%"))
                        )
                    );
                }
            }

            // Sıralama
            q = sortOrder switch
            {
                "AdSoyad" => q.OrderBy(x => x.Ogrenci!.OgrenciAdSoyad).ThenBy(x => x.Ogrenci!.OgrenciNo),
                "AdSoyad_desc" => q.OrderByDescending(x => x.Ogrenci!.OgrenciAdSoyad).ThenBy(x => x.Ogrenci!.OgrenciNo),
                "No" => q.OrderBy(x => x.Ogrenci!.OgrenciNo).ThenBy(x => x.Ogrenci!.OgrenciAdSoyad),
                "No_desc" => q.OrderByDescending(x => x.Ogrenci!.OgrenciNo).ThenBy(x => x.Ogrenci!.OgrenciAdSoyad),
                "Tarih" => q.OrderBy(x => (x.OgrenciGTarih ?? x.OgrenciCTarih)).ThenBy(x => x.OgrenciDetayId),
                "Tarih_desc" => q.OrderByDescending(x => (x.OgrenciGTarih ?? x.OgrenciCTarih)).ThenByDescending(x => x.OgrenciDetayId),
                _ => q.OrderByDescending(x => (x.OgrenciGTarih ?? x.OgrenciCTarih)).ThenByDescending(x => x.OgrenciDetayId)
            };

            // Sayfalama
            var model = await SayfalanmisListeModel<OgrenciDetayModel>.CreateAsync(q, page, 50, ct);
            return View(model);
        }

        // Tek öğrencinin giriş-çıkışları
        [HttpGet]
        public async Task<IActionResult> GirisCikisDetay(
            int id,
            DateTime? startDate,
            DateTime? endDate,
            IstasyonTipi? istasyonTipi,
            int? pageNumber,
            CancellationToken ct = default)
        {
            var ogrenci = await _context.Ogrenciler
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.OgrenciId == id);

            if (ogrenci is null) return NotFound();

            var q = _context.OgrenciDetaylar
                .AsNoTracking()
                .Include(d => d.Cihaz)
                .Include(d => d.Ogrenci)
                .Where(d => d.OgrenciId == id);

            // --- Tarih filtreleri (Giriş veya Çıkış kapsayıcı) ---
            var hasStart = startDate.HasValue;
            var hasEnd = endDate.HasValue;
            var s = startDate?.Date;
            var eExclusive = endDate?.Date.AddDays(1);

            if (hasStart && hasEnd)
            {
                q = q.Where(d =>
                    (d.OgrenciGTarih.HasValue && d.OgrenciGTarih.Value >= s!.Value && d.OgrenciGTarih.Value < eExclusive!.Value) ||
                    (d.OgrenciCTarih.HasValue && d.OgrenciCTarih.Value >= s!.Value && d.OgrenciCTarih.Value < eExclusive!.Value));
            }
            else if (hasStart)
            {
                q = q.Where(d =>
                    (d.OgrenciGTarih.HasValue && d.OgrenciGTarih.Value >= s!.Value) ||
                    (d.OgrenciCTarih.HasValue && d.OgrenciCTarih.Value >= s!.Value));
            }
            else if (hasEnd)
            {
                q = q.Where(d =>
                    (d.OgrenciGTarih.HasValue && d.OgrenciGTarih.Value < eExclusive!.Value) ||
                    (d.OgrenciCTarih.HasValue && d.OgrenciCTarih.Value < eExclusive!.Value));
            }

            if (istasyonTipi.HasValue)
            {
                q = q.Where(d => d.Cihaz != null && d.Cihaz.IstasyonTipi == istasyonTipi.Value);
            }

            // En az bir tarihi olanlar
            q = q.Where(d => d.OgrenciGTarih.HasValue || d.OgrenciCTarih.HasValue);

            // --- Sıralama (Gün DESC → PairIndex ASC → Giriş önce → Zaman DESC → Id DESC) ---
            q = q
                // 1) Gün (en yeni gün en üstte)
                .OrderByDescending(d => (d.OgrenciGTarih ?? d.OgrenciCTarih)!.Value.Date)

                // 2) Pair index: Aynı gün, kendisinden BÜYÜK tüm Giriş sayısı + 1
                //    Böylece her "Giriş" ile onu izleyen "Çıkış" aynı pairIndex alır.
                .ThenBy(d =>
                    1 + _context.OgrenciDetaylar
                        .Where(x =>
                            x.OgrenciId == d.OgrenciId &&
                            x.OgrenciGTarih.HasValue &&
                            // aynı gün
                            EF.Functions.DateDiffDay(
                                x.OgrenciGTarih.Value,
                                (d.OgrenciGTarih ?? d.OgrenciCTarih)!.Value) == 0 &&
                            // kendisinden büyük Girişler
                            x.OgrenciGTarih.Value > (d.OgrenciGTarih ?? d.OgrenciCTarih)!.Value
                        )
                        .Count()
                )

                // 3) Aynı pair içinde Giriş önce, ardından Çıkış
                .ThenByDescending(d => d.OgrenciGTarih.HasValue)

                // 4) Tür içi zaman DESC (daha yeni üstte)
                .ThenByDescending(d => d.OgrenciGTarih ?? d.OgrenciCTarih)

                // 5) Deterministik tie-breaker
                .ThenByDescending(d => d.OgrenciDetayId);

            var proj = q.Select(h => new OgrenciGirisCikisVm
            {
                OgrenciDetayId = h.OgrenciDetayId,
                OgrenciAdSoyad = h.Ogrenci != null ? h.Ogrenci.OgrenciAdSoyad : "Bilinmiyor",
                OgrenciKartNo = h.Ogrenci != null ? h.Ogrenci.OgrenciKartNo : "-",
                OgrenciGTarih = h.OgrenciGTarih,
                OgrenciCTarih = h.OgrenciCTarih,
                OgrenciGecisTipi = h.OgrenciGecisTipi,
                CihazAdi = h.Cihaz != null ? h.Cihaz.CihazAdi : "Bilinmiyor"
            });

            var pageIndex = pageNumber.GetValueOrDefault(1);
            var hareketlerPaged = await SayfalanmisListeModel<OgrenciGirisCikisVm>
                .CreateAsync(proj, pageIndex, 25, ct);

            var vm = new OgrenciGirisCikisListViewModel
            {
                Ogrenci = ogrenci,
                Hareketler = hareketlerPaged
            };

            ViewData["StartDate"] = startDate?.ToString("yyyy-MM-dd");
            ViewData["EndDate"] = endDate?.ToString("yyyy-MM-dd");
            ViewData["IstasyonTipi"] = istasyonTipi.HasValue ? ((int)istasyonTipi.Value).ToString() : null;

            return View("GirisCikisDetay", vm);
        }


        [HttpGet]
        public IActionResult DetayExcel(
    string? searchName,
    DateTime? startDate,
    DateTime? endDate,
    IstasyonTipi? istasyonTipi)
        {
            var loglar = _context.OgrenciDetaylar
                .Include(l => l.Ogrenci)
                    .ThenInclude(o => o.Birim)
                .Include(l => l.Cihaz)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchName))
            {
                var s = searchName.Trim();
                loglar = loglar.Where(l => l.Ogrenci != null &&
                                           (EF.Functions.Like(l.Ogrenci.OgrenciAdSoyad, $"%{s}%")
                                            || (l.Ogrenci.Birim != null &&
                                                EF.Functions.Like(l.Ogrenci.Birim.BirimAd, $"%{s}%"))));
            }

            if (istasyonTipi.HasValue)
            {
                loglar = loglar.Where(l => l.Cihaz != null &&
                                           l.Cihaz.IstasyonTipi == istasyonTipi.Value);
            }

            if (startDate.HasValue)
            {
                var s = startDate.Value.Date;
                loglar = loglar.Where(l =>
                    (l.OgrenciGTarih.HasValue && l.OgrenciGTarih.Value >= s) ||
                    (l.OgrenciCTarih.HasValue && l.OgrenciCTarih.Value >= s));
            }

            if (endDate.HasValue)
            {
                var endExclusive = endDate.Value.Date.AddDays(1);
                loglar = loglar.Where(l =>
                    (l.OgrenciGTarih.HasValue && l.OgrenciGTarih.Value < endExclusive) ||
                    (l.OgrenciCTarih.HasValue && l.OgrenciCTarih.Value < endExclusive));
            }

            var filteredLogs = loglar
                .OrderByDescending(l => l.OgrenciCTarih ?? l.OgrenciGTarih)
                .ToList();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Öğrenci Giriş-Çıkış");

            // Başlıklar (8 sütun)
            ws.Cell(1, 1).Value = "#";
            ws.Cell(1, 2).Value = "Ad Soyad";
            ws.Cell(1, 3).Value = "Sınıf/Birim";
            ws.Cell(1, 4).Value = "Kart No";
            ws.Cell(1, 5).Value = "Giriş Tarihi";
            ws.Cell(1, 6).Value = "Çıkış Tarihi";
            ws.Cell(1, 7).Value = "Geçiş Tipi";
            ws.Cell(1, 8).Value = "Cihaz Adı";

            // Başlık stili
            var headerRange = ws.Range(1, 1, 1, 8);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            // Veriler
            int row = 2;
            foreach (var log in filteredLogs)
            {
                ws.Cell(row, 1).Value = log.OgrenciDetayId;
                ws.Cell(row, 2).Value = log.Ogrenci?.OgrenciAdSoyad ?? "-";
                ws.Cell(row, 3).Value = log.Ogrenci?.Birim?.BirimAd ?? "-";
                ws.Cell(row, 4).Value = log.Ogrenci?.OgrenciKartNo ?? "-";

                var cGiris = ws.Cell(row, 5);
                if (log.OgrenciGTarih.HasValue)
                {
                    cGiris.Value = log.OgrenciGTarih.Value;
                    cGiris.Style.DateFormat.Format = "dd.MM.yyyy HH:mm";
                }
                else cGiris.Value = "-";

                var cCikis = ws.Cell(row, 6);
                if (log.OgrenciCTarih.HasValue)
                {
                    cCikis.Value = log.OgrenciCTarih.Value;
                    cCikis.Style.DateFormat.Format = "dd.MM.yyyy HH:mm";
                }
                else cCikis.Value = "-";

                ws.Cell(row, 7).Value = log.OgrenciGecisTipi.ToString();
                ws.Cell(row, 8).Value = log.Cihaz?.CihazAdi ?? "-";
                row++;
            }

            // Üst başlığı sabitle
            ws.SheetView.FreezeRows(1);

            // Filtre ekle
            ws.Range(1, 1, Math.Max(1, row - 1), 8).SetAutoFilter();

            // Alt toplam satırı
            int summaryRow = row + 1;
            ws.Range(summaryRow, 1, summaryRow, 7).Merge(); // ilk 7 sütun birleşik kalır
            var totalCell = ws.Cell(summaryRow, 8);         // toplam bilgi 8. sütunda
            totalCell.Value = $"Toplam {filteredLogs.Count} kayıt";
            totalCell.Style.Font.Bold = true;
            totalCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            totalCell.Style.Border.TopBorder = XLBorderStyleValues.Thin;

            // Otomatik sütun genişliği
            ws.Columns(1, 8).AdjustToContents(1, summaryRow);
            foreach (var col in ws.Columns(1, 8))
                if (col.Width < 15) col.Width = 15;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();

            return File(content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "OgrenciGirisCikis.xlsx");
        }
    }
}