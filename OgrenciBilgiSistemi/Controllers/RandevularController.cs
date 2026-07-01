using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OgrenciBilgiSistemi.Services.Interfaces;
using OgrenciBilgiSistemi.ViewModels;

namespace OgrenciBilgiSistemi.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class RandevularController : Controller
    {
        private readonly IRandevuService _randevuService;
        private readonly IKullaniciService _kullaniciService;
        private readonly ILogger<RandevularController> _logger;

        public RandevularController(
            IRandevuService randevuService,
            IKullaniciService kullaniciService,
            ILogger<RandevularController> logger)
        {
            _randevuService = randevuService;
            _kullaniciService = kullaniciService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? arama, int? ogretmenId, RandevuDurumu? durum,
            DateTime? baslangic, DateTime? bitis,
            int sayfaNo = 1, CancellationToken ct = default)
        {
            var paged = await _randevuService.AraVeListele(arama, ogretmenId, durum, baslangic, bitis, sayfaNo, 20, ct);

            var vm = new RandevuListeVm
            {
                Randevular = paged,
                Arama = arama,
                OgretmenId = ogretmenId,
                Durum = durum,
                Baslangic = baslangic?.ToString("yyyy-MM-dd"),
                Bitis = bitis?.ToString("yyyy-MM-dd"),
                Ogretmenler = await _kullaniciService.GetKullanicilarByRolSelectListAsync(KullaniciRolu.Ogretmen, ct),
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Detay(int id, CancellationToken ct = default)
        {
            var randevu = await _randevuService.IdIleGetir(id, ct);
            if (randevu is null)
                return NotFound();

            return View(randevu);
        }

        [HttpGet]
        public async Task<IActionResult> ExcelExport(
            string? arama, int? ogretmenId, RandevuDurumu? durum,
            DateTime? baslangic, DateTime? bitis,
            CancellationToken ct = default)
        {
            var liste = await _randevuService.ExcelListeleAsync(arama, ogretmenId, durum, baslangic, bitis, ct);

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Randevular");

            ws.Cell(1, 1).Value = "Tarih";
            ws.Cell(1, 2).Value = "Öğretmen";
            ws.Cell(1, 3).Value = "Veli";
            ws.Cell(1, 4).Value = "Öğrenci";
            ws.Cell(1, 5).Value = "Durum";
            ws.Cell(1, 6).Value = "Oluşturan";
            ws.Cell(1, 7).Value = "Not";

            var headerRange = ws.Range(1, 1, 1, 7);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            int row = 2;
            foreach (var r in liste)
            {
                var cTarih = ws.Cell(row, 1);
                cTarih.Value = r.RandevuTarihi;
                cTarih.Style.DateFormat.Format = "dd.MM.yyyy HH:mm";

                ws.Cell(row, 2).Value = r.Ogretmen?.KullaniciAdi ?? "-";
                ws.Cell(row, 3).Value = r.Veli?.KullaniciAdi ?? "-";
                ws.Cell(row, 4).Value = r.Ogrenci?.OgrenciAdSoyad ?? "-";
                ws.Cell(row, 5).Value = r.Durum.ToString();
                ws.Cell(row, 6).Value = r.OgretmenTarafindanOlusturuldu ? "Öğretmen" : "Veli";
                ws.Cell(row, 7).Value = r.Not ?? "";
                row++;
            }

            ws.SheetView.FreezeRows(1);
            ws.Range(1, 1, Math.Max(1, row - 1), 7).SetAutoFilter();

            int summaryRow = row + 1;
            ws.Range(summaryRow, 1, summaryRow, 6).Merge();
            var totalCell = ws.Cell(summaryRow, 7);
            totalCell.Value = $"Toplam {liste.Count} kayıt";
            totalCell.Style.Font.Bold = true;
            totalCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            totalCell.Style.Border.TopBorder = XLBorderStyleValues.Thin;

            ws.Columns(1, 7).AdjustToContents(1, summaryRow);
            foreach (var col in ws.Columns(1, 7))
                if (col.Width < 15) col.Width = 15;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Randevular_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IptalEt(int id, CancellationToken ct = default)
        {
            try
            {
                await _randevuService.IptalEt(id, ct);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Randevu iptal edilemedi. Id={Id}", id);
                TempData["Hata"] = "Randevu iptal edilirken bir hata oluştu.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
