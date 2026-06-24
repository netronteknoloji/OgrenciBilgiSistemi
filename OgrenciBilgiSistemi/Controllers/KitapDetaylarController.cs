using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using OgrenciBilgiSistemi.Services.Interfaces;
using OgrenciBilgiSistemi.ViewModels;

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
            var pageIndex = Math.Max(1, pageNumber ?? 1);

            var paged = await _service.SearchPagedAsync(
                sortOrder, searchString, durumFilter, pageIndex, 25, ct);

            return View(new KitapDetayIndexVm
            {
                Detaylar = paged,
                AramaMetni = searchString,
                Siralama = sortOrder,
                DurumFiltre = durumFilter,
            });
        }

        [HttpGet]
        public async Task<IActionResult> Ekle(int? ogrenciId, CancellationToken ct)
        {
            var vm = new KitapDetayFormVm
            {
                OgrenciId = ogrenciId ?? 0,
                Kitaplar = await _service.GetKitapSelectListAsync(ct),
                Ogrenciler = await _service.GetOgrenciSelectListAsync(ct),
                FormAction = "Ekle",
                SubmitText = "Kaydet",
                IncludeId = false,
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ekle(KitapDetayFormVm vm, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                vm.Kitaplar = await _service.GetKitapSelectListAsync(ct);
                vm.Ogrenciler = await _service.GetOgrenciSelectListAsync(ct);
                return View(vm);
            }

            try
            {
                await _service.AddAsync(vm.ToModel(), ct);
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                _logger.LogWarning(ex, "Kitap detayı eklenemedi (iş kuralı).");
                vm.Kitaplar = await _service.GetKitapSelectListAsync(ct);
                vm.Ogrenciler = await _service.GetOgrenciSelectListAsync(ct);
                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kitap detayı eklenirken hata oluştu.");
                TempData["Hata"] = "Kitap detayı eklenirken bir hata oluştu.";
                vm.Kitaplar = await _service.GetKitapSelectListAsync(ct);
                vm.Ogrenciler = await _service.GetOgrenciSelectListAsync(ct);
                return View(vm);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Guncelle(int id, CancellationToken ct)
        {
            var detay = await _service.GetByIdAsync(id, ct);
            if (detay == null) return NotFound();

            var vm = KitapDetayFormVm.FromModel(detay);
            vm.Kitaplar = await _service.GetKitapSelectListAsync(ct);
            vm.Ogrenciler = await _service.GetOgrenciSelectListAsync(ct);
            vm.FormAction = "Guncelle";
            vm.SubmitText = "Güncelle";
            vm.IncludeId = true;

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Guncelle(KitapDetayFormVm vm, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                vm.Kitaplar = await _service.GetKitapSelectListAsync(ct);
                vm.Ogrenciler = await _service.GetOgrenciSelectListAsync(ct);
                return View(vm);
            }

            try
            {
                await _service.UpdateAsync(vm.ToModel(), ct);
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                _logger.LogWarning(ex, "Kitap detayı güncellenemedi (iş kuralı).");
                vm.Kitaplar = await _service.GetKitapSelectListAsync(ct);
                vm.Ogrenciler = await _service.GetOgrenciSelectListAsync(ct);
                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Detay güncellenirken hata oluştu.");
                TempData["Hata"] = "Detay güncellenirken bir hata oluştu.";
                vm.Kitaplar = await _service.GetKitapSelectListAsync(ct);
                vm.Ogrenciler = await _service.GetOgrenciSelectListAsync(ct);
                return View(vm);
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
            var filteredList = await _service.GetFilteredListAsync(sortOrder, searchString, durumFilter, ct);

            using var workbook = new XLWorkbook();
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

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "KitapDetaylar.xlsx");
        }
    }
}
