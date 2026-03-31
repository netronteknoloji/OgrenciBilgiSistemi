using Microsoft.AspNetCore.Mvc;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Services.Interfaces;

namespace OgrenciBilgiSistemi.Controllers
{
    public class BirimlerController : Controller
    {
        private readonly IBirimService _birimService;
        private readonly ILogger<BirimlerController> _logger;

        public BirimlerController(IBirimService birimService, ILogger<BirimlerController> logger)
        {
            _birimService = birimService;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string? searchString, int pageNumber = 1,
            BirimFiltre durum = BirimFiltre.Aktif, CancellationToken ct = default)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["Durum"] = durum;

            var paged = await _birimService.SearchPagedAsync(
                searchString: searchString,
                page: pageNumber,
                pageSize: 50,
                filtre: durum,
                sinifMi: null,
                ct: ct);

            return View(paged);
        }

        [HttpGet]
        public IActionResult Ekle() => View(new BirimModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ekle(BirimModel model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                TempData["Warning"] = "Lütfen formu kontrol edin.";
                return View(model);
            }

            try
            {
                if (await _birimService.ExistsWithNameAsync(model.BirimAd, excludeId: null, ct))
                {
                    ModelState.AddModelError(nameof(model.BirimAd), "Bu ad zaten kullanılıyor.");
                    return View(model);
                }

                await _birimService.AddAsync(model, ct);
                TempData["Success"] = "Birim başarıyla kaydedildi.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Birim eklenirken hata oluştu.");
                TempData["Error"] = "Kayıt sırasında bir hata oluştu.";
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Guncelle(int id, CancellationToken ct)
        {
            var birim = await _birimService.GetByIdAsync(id, true, ct);
            if (birim == null) return NotFound();
            return View(birim);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Guncelle(BirimModel model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                TempData["Warning"] = "Lütfen formu kontrol edin.";
                return View(model);
            }

            try
            {
                if (await _birimService.ExistsWithNameAsync(model.BirimAd, excludeId: model.BirimId, ct))
                {
                    ModelState.AddModelError(nameof(model.BirimAd), "Bu ad zaten kullanılıyor.");
                    return View(model);
                }

                await _birimService.UpdateAsync(model, ct);
                TempData["Success"] = "Birim başarıyla güncellendi.";
                return RedirectToAction(nameof(Index));
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Birim güncellenirken hata oluştu.");
                TempData["Error"] = "Güncelleme sırasında bir hata oluştu.";
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sil(int id, CancellationToken ct)
        {
            try
            {
                await _birimService.DeleteAsync(id, ct);
                TempData["Success"] = "Birim silindi.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Birim silinirken hata oluştu.");
                TempData["Error"] = "Silme sırasında bir hata oluştu.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}