using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Services.Interfaces;

namespace OgrenciBilgiSistemi.Controllers
{
    public class KitapDetaylarController : Controller
    {
        private readonly IKitapDetayService _service;
        private readonly ILogger<KitapDetaylarController> _logger;

        public KitapDetaylarController(IKitapDetayService service, ILogger<KitapDetaylarController> logger)
        {
            _service = service;
            _logger = logger;
        }

        public async Task<IActionResult> Index(
            string? sortOrder,
            string? searchString,
            string? durumFilter,
            int? pageNumber,
            CancellationToken ct = default)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["CurrentFilter"] = searchString;
            ViewData["DurumFilter"] = durumFilter;

            var pageIndex = pageNumber ?? 1;
            if (pageIndex < 1) pageIndex = 1;

            var paged = await _service.SearchPagedAsync(
                sortOrder,
                searchString,
                durumFilter,
                pageIndex,
                25,
                ct);

            // Öğrenci drop-down’ı kullanıyorsan:
            //ViewBag.Ogrenciler = await _service.GetOgrenciSelectListAsync(ct);

            return View(paged);
        }

        [HttpGet]
        public async Task<IActionResult> Ekle(int? ogrenciId, CancellationToken ct)
        {
            var model = new KitapDetayModel();

            if (ogrenciId.HasValue)
                model.OgrenciId = ogrenciId.Value;

            model.Kitaplar = await _service.GetKitapSelectListAsync(ct);
            model.Ogrenciler = await _service.GetOgrenciSelectListAsync(ct);

            ViewData["Action"] = "Ekle";
            ViewData["SubmitText"] = "Kaydet";
            ViewData["IncludeId"] = false;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ekle(KitapDetayModel model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                model.Kitaplar = await _service.GetKitapSelectListAsync(ct);
                model.Ogrenciler = await _service.GetOgrenciSelectListAsync(ct);
                return View(model);
            }

            try
            {
                await _service.AddAsync(model, ct);
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                _logger.LogWarning(ex, "Kitap detayı eklenemedi (iş kuralı).");

                model.Kitaplar = await _service.GetKitapSelectListAsync(ct);
                model.Ogrenciler = await _service.GetOgrenciSelectListAsync(ct);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kitap detayı eklenirken hata oluştu.");
                TempData["Hata"] = "Kitap detayı eklenirken bir hata oluştu.";

                model.Kitaplar = await _service.GetKitapSelectListAsync(ct);
                model.Ogrenciler = await _service.GetOgrenciSelectListAsync(ct);
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Guncelle(int id, CancellationToken ct)
        {
            var detay = await _service.GetByIdAsync(id, ct);
            if (detay == null) return NotFound();

            detay.Kitaplar = await _service.GetKitapSelectListAsync(ct);
            detay.Ogrenciler = await _service.GetOgrenciSelectListAsync(ct);

            ViewData["Action"] = "Guncelle";
            ViewData["SubmitText"] = "Güncelle";
            ViewData["IncludeId"] = true;

            return View(detay);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Guncelle(KitapDetayModel model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                model.Kitaplar = await _service.GetKitapSelectListAsync(ct);
                model.Ogrenciler = await _service.GetOgrenciSelectListAsync(ct);
                return View(model);
            }

            try
            {
                await _service.UpdateAsync(model, ct);
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                _logger.LogWarning(ex, "Kitap detayı güncellenemedi (iş kuralı).");

                model.Kitaplar = await _service.GetKitapSelectListAsync(ct);
                model.Ogrenciler = await _service.GetOgrenciSelectListAsync(ct);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Detay güncellenirken hata oluştu.");
                TempData["Hata"] = "Detay güncellenirken bir hata oluştu.";

                model.Kitaplar = await _service.GetKitapSelectListAsync(ct);
                model.Ogrenciler = await _service.GetOgrenciSelectListAsync(ct);
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sil(int id, CancellationToken ct)
        {
            try
            {
                await _service.TeslimAlAsync(id, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Detay silinirken (teslim alınırken) hata oluştu.");
                TempData["Hata"] = "Detay silinirken bir hata oluştu.";
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> ExportToExcel(
            string? sortOrder,
            string? searchString,
            string? durumFilter,
            CancellationToken ct)
        {
            var filteredList = await _service.GetFilteredListAsync(
                sortOrder,
                searchString,
                durumFilter,
                ct);

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Kitap Detaylar");

                worksheet.Cell(1, 1).Value = "#";
                worksheet.Cell(1, 2).Value = "Kitap Adı";
                worksheet.Cell(1, 3).Value = "Öğrenci Ad Soyad";
                worksheet.Cell(1, 4).Value = "Alış Tarihi";
                worksheet.Cell(1, 5).Value = "Veriş Tarihi";
                worksheet.Cell(1, 6).Value = "Durum";

                int row = 2;
                foreach (var kd in filteredList)
                {
                    worksheet.Cell(row, 1).Value = kd.KitapDetayId;
                    worksheet.Cell(row, 2).Value = kd.Kitap?.KitapAd;
                    worksheet.Cell(row, 3).Value = kd.Ogrenci?.OgrenciAdSoyad;
                    worksheet.Cell(row, 4).Value = kd.KitapAlTarih.ToString("dd.MM.yyyy");
                    worksheet.Cell(row, 5).Value = kd.KitapVerTarih?.ToString("dd.MM.yyyy") ?? "-";
                    worksheet.Cell(row, 6).Value = kd.KitapDurum.ToString();
                    row++;
                }

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();

                    return File(
                        content,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "KitapDetaylar.xlsx");
                }
            }
        }
    }
}