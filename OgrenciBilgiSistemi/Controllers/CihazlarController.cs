using Microsoft.AspNetCore.Mvc;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.ViewModels;

namespace OgrenciBilgiSistemi.Controllers
{
    [AutoValidateAntiforgeryToken]
    public class CihazlarController : Controller
    {
        private readonly ILogger<CihazlarController> _logger;
        private readonly ICihazService _cihazService;

        public CihazlarController(ILogger<CihazlarController> logger, ICihazService cihazService)
        {
            _logger = logger;
            _cihazService = cihazService;
        }

        public async Task<IActionResult> Index(string searchString, int page = 1, CancellationToken ct = default)
        {
            var paged = await _cihazService.SearchPagedAsync(searchString, page, 10, ct);
            return View(new CihazIndexVm { Cihazlar = paged, CurrentFilter = searchString });
        }

        [HttpGet]
        public IActionResult Ekle() => View(new CihazFormVm());

        [HttpPost]
        public async Task<IActionResult> Ekle(CihazFormVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid) return View(vm);

            var ok = await _cihazService.CihazEkleAsync(vm.ToModel(), ct);
            if (!ok)
            {
                TempData["Error"] = "Cihaz kaydedilemedi. Bilgileri kontrol edin.";
                return View(vm);
            }

            TempData["Success"] = "Cihaz başarıyla kaydedildi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Guncelle(int id, CancellationToken ct = default)
        {
            var cihaz = await _cihazService.CihazGetByIdAsync(id, ct);
            if (cihaz is null) return NotFound();
            return View(CihazFormVm.FromModel(cihaz));
        }

        [HttpPost]
        public async Task<IActionResult> Guncelle(CihazFormVm vm, CancellationToken ct = default)
        {
            vm.FormAction = "Guncelle"; vm.SubmitText = "Güncelle"; vm.IncludeId = true; vm.ShowGuid = true;
            if (!ModelState.IsValid)
            {
                TempData["Warning"] = "Lütfen formu kontrol edin.";
                return View(vm);
            }

            var ok = await _cihazService.CihazGuncelleAsync(vm.ToModel(), ct);
            if (!ok)
            {
                TempData["Error"] = "Cihaz güncellenemedi. Bilgileri kontrol edin.";
                return View(vm);
            }

            TempData["Success"] = "Cihaz başarıyla güncellendi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Sil(int id, CancellationToken ct = default)
        {
            var cihaz = await _cihazService.CihazGetByIdAsync(id, ct);
            if (cihaz is null)
            {
                _logger.LogWarning("Silinmek istenen cihaz bulunamadı: ID {Id}", id);
                return NotFound();
            }
            return View(cihaz);
        }

        [HttpPost, ActionName("Sil")]
        public async Task<IActionResult> SilConfirmed(int id, CancellationToken ct = default)
        {
            var ok = await _cihazService.CihazSilAsync(id, ct);

            TempData[ok ? "Success" : "Error"] = ok
                ? "Cihaz başarıyla silindi."
                : "Silme işlemi sırasında beklenmeyen bir hata oluştu.";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> TumKullanicilariListele(int cihazId, CancellationToken ct = default)
        {
            try
            {
                var users = await _cihazService.CihazdanKullanicilariListeleAsync(cihazId, ct);
                return View(new CihazKullanicilariVm { Kullanicilar = users, CihazId = cihazId });
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı listesi alınırken hata (cihazId={Id})", cihazId);
                TempData["Error"] = "Kullanıcı listesi alınamadı.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        public async Task<IActionResult> TumKullanicilariSil(int cihazId, CancellationToken ct = default)
        {
            try
            {
                var ok = await _cihazService.CihazdakiTumKullanicilariSilAsync(cihazId, ct);
                TempData[ok ? "Message" : "Error"] = ok
                    ? "Cihazdaki tüm kullanıcılar başarıyla silindi."
                    : "Kullanıcılar silinemedi. Cihaz bağlantısını/firmware'i kontrol edin.";
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tüm kullanıcılar silinirken hata (cihazId={Id})", cihazId);
                TempData["Error"] = "Silme işlemi sırasında beklenmeyen bir hata oluştu.";
            }

            return RedirectToAction(nameof(TumKullanicilariListele), new { cihazId });
        }
    }
}
