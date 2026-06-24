using Microsoft.AspNetCore.Mvc;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Services.Interfaces;
using OgrenciBilgiSistemi.ViewModels;

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
            var paged = await _birimService.SearchPagedAsync(
                searchString: searchString,
                page: pageNumber,
                pageSize: 50,
                filtre: durum,
                sinifMi: null,
                ct: ct);

            return View(new BirimIndexVm
            {
                Birimler = paged,
                AramaMetni = searchString,
                Durum = durum,
            });
        }

        [HttpGet]
        public IActionResult Ekle() => View(new BirimFormVm());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ekle(BirimFormVm vm, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                TempData["Warning"] = "Lütfen formu kontrol edin.";
                return View(vm);
            }

            try
            {
                if (await _birimService.ExistsWithNameAsync(vm.BirimAd, excludeId: null, ct))
                {
                    ModelState.AddModelError(nameof(vm.BirimAd), "Bu ad zaten kullanılıyor.");
                    return View(vm);
                }

                await _birimService.AddAsync(vm.ToModel(), ct);
                TempData["Success"] = "Birim başarıyla kaydedildi.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Birim eklenirken hata oluştu.");
                TempData["Error"] = "Kayıt sırasında bir hata oluştu.";
                return View(vm);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Guncelle(int id, CancellationToken ct)
        {
            var birim = await _birimService.GetByIdAsync(id, true, ct);
            if (birim == null) return NotFound();
            return View(BirimFormVm.FromModel(birim));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Guncelle(BirimFormVm vm, CancellationToken ct)
        {
            vm.FormAction = "Guncelle"; vm.SubmitText = "Güncelle"; vm.IncludeId = true;
            if (!ModelState.IsValid)
            {
                TempData["Warning"] = "Lütfen formu kontrol edin.";
                return View(vm);
            }

            try
            {
                if (await _birimService.ExistsWithNameAsync(vm.BirimAd, excludeId: vm.BirimId, ct))
                {
                    ModelState.AddModelError(nameof(vm.BirimAd), "Bu ad zaten kullanılıyor.");
                    return View(vm);
                }

                await _birimService.UpdateAsync(vm.ToModel(), ct);
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
                return View(vm);
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
