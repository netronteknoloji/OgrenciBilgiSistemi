using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Services.Interfaces;

namespace OgrenciBilgiSistemi.Controllers
{
    public class KitaplarController : Controller
    {
        private readonly IKitapService _kitapService;
        private readonly ILogger<KitaplarController> _logger;

        public KitaplarController(IKitapService kitapService, ILogger<KitaplarController> logger)
        {
            _kitapService = kitapService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string sortOrder, string searchString, int? pageNumber, CancellationToken ct = default)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["CurrentFilter"] = searchString;

            var pageIndex = pageNumber.GetValueOrDefault(1);
            if (pageIndex < 1) pageIndex = 1;

            var paged = await _kitapService.SearchPagedAsync(sortOrder, searchString, pageIndex, 25, ct);
            return View(paged);
        }

        [HttpGet]
        public IActionResult Ekle() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ekle(KitapModel model, IFormFile? KitapGorselFile, CancellationToken ct)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                await _kitapService.AddAsync(model, KitapGorselFile, ct);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kitap eklenirken hata oluştu.");
                TempData["Hata"] = "Kitap eklenirken bir hata oluştu.";
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Guncelle(int id, CancellationToken ct)
        {
            var kitap = await _kitapService.GetByIdAsync(id, tumKitaplar: true, ct);
            if (kitap == null) return NotFound();

            return View(kitap);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Guncelle(KitapModel model, IFormFile? KitapGorselFile, CancellationToken ct)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                await _kitapService.UpdateAsync(model, KitapGorselFile, ct);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kitap güncellenirken hata oluştu.");
                TempData["Hata"] = "Kitap güncellenirken bir hata oluştu.";
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sil(int id, CancellationToken ct)
        {
            try
            {
                await _kitapService.SoftDeleteAsync(id, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kitap silinirken hata oluştu.");
                TempData["Hata"] = "Kitap silinirken bir hata oluştu.";
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> ExportToExcel(string sortOrder, string searchString, CancellationToken ct)
        {
            var filteredList = await _kitapService.GetFilteredListAsync(sortOrder, searchString, onlyActive: true, ct);

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Kitap Listesi");

                worksheet.Cell(1, 1).Value = "#";
                worksheet.Cell(1, 2).Value = "Kitap Adı";
                worksheet.Cell(1, 3).Value = "Tür";
                worksheet.Cell(1, 4).Value = "Gün";
                worksheet.Cell(1, 5).Value = "Durum";

                int row = 2;
                foreach (var k in filteredList)
                {
                    worksheet.Cell(row, 1).Value = k.KitapId;
                    worksheet.Cell(row, 2).Value = k.KitapAd;
                    worksheet.Cell(row, 3).Value = k.KitapTurAd;
                    worksheet.Cell(row, 4).Value = k.KitapGun;
                    worksheet.Cell(row, 5).Value = k.KitapDurum ? "Aktif" : "Pasif";
                    row++;
                }

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();

                    return File(content,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "KitapListesi.xlsx");
                }
            }
        }
    }
}