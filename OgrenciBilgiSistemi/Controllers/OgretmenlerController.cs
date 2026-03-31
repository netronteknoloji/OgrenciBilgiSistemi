using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Services.Interfaces;
using OgrenciBilgiSistemi.ViewModels;

namespace OgrenciBilgiSistemi.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class OgretmenlerController : Controller
    {
        private readonly IOgretmenProfilService _ogretmenProfilService;
        private readonly IKullaniciService _kullaniciService;
        private readonly IBirimService _birimService;
        private readonly ILogger<OgretmenlerController> _logger;

        public OgretmenlerController(
            IOgretmenProfilService ogretmenProfilService,
            IKullaniciService kullaniciService,
            IBirimService birimService,
            ILogger<OgretmenlerController> logger)
        {
            _ogretmenProfilService = ogretmenProfilService;
            _kullaniciService = kullaniciService;
            _birimService = birimService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string searchString, int page = 1, CancellationToken ct = default)
        {
            ViewData["CurrentFilter"] = searchString;
            var paged = await _ogretmenProfilService.SearchPagedAsync(searchString, page, 50, ct);
            return View(paged);
        }

        [HttpGet]
        public async Task<IActionResult> Ekle()
        {
            ViewBag.Birimler = await _kullaniciService.GetBirimlerSelectListAsync();
            return View(new OgretmenEkleVm());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ekle(OgretmenEkleVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Birimler = await _kullaniciService.GetBirimlerSelectListAsync(ct);
                return View(vm);
            }

            try
            {
                await _ogretmenProfilService.EkleKullaniciVeProfilAsync(vm, ct);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Öğretmen eklenirken hata oluştu.");
                ModelState.AddModelError(string.Empty, "Kayıt sırasında bir hata oluştu.");
                ViewBag.Birimler = await _kullaniciService.GetBirimlerSelectListAsync(ct);
                return View(vm);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Guncelle(int? id, CancellationToken ct = default)
        {
            if (id == null) return NotFound();

            var profil = await _ogretmenProfilService.GetByIdAsync(id.Value, ct);
            if (profil == null) return NotFound();

            ViewBag.Birimler = await _kullaniciService.GetBirimlerSelectListAsync(ct);
            return View(profil);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Guncelle(OgretmenProfilModel model, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Birimler = await _kullaniciService.GetBirimlerSelectListAsync(ct);
                return View(model);
            }

            try
            {
                await _ogretmenProfilService.GuncelleAsync(model, ct);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Öğretmen profili güncellenirken hata oluştu.");
                ModelState.AddModelError(string.Empty, "Güncelleme sırasında bir hata oluştu.");
                ViewBag.Birimler = await _kullaniciService.GetBirimlerSelectListAsync(ct);
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sil(int id, CancellationToken ct = default)
        {
            try
            {
                await _ogretmenProfilService.SilAsync(id, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Öğretmen profili silinirken hata oluştu.");
                TempData["ErrMessage"] = "Öğretmen profili silinirken bir hata oluştu.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
