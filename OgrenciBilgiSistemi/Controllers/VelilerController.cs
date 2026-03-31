using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Services.Interfaces;
using OgrenciBilgiSistemi.ViewModels;

namespace OgrenciBilgiSistemi.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class VelilerController : Controller
    {
        private readonly IVeliProfilService _veliProfilService;
        private readonly IKullaniciService _kullaniciService;
        private readonly ILogger<VelilerController> _logger;

        public VelilerController(
            IVeliProfilService veliProfilService,
            IKullaniciService kullaniciService,
            ILogger<VelilerController> logger)
        {
            _veliProfilService = veliProfilService;
            _kullaniciService = kullaniciService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string searchString, int page = 1, CancellationToken ct = default)
        {
            ViewData["CurrentFilter"] = searchString;
            var paged = await _veliProfilService.SearchPagedAsync(searchString, page, 50, ct);
            return View(paged);
        }

        [HttpGet]
        public IActionResult Ekle()
        {
            return View(new VeliEkleVm());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ekle(VeliEkleVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
                return View(vm);

            try
            {
                await _veliProfilService.EkleKullaniciVeProfilAsync(vm, ct);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Veli eklenirken hata oluştu.");
                ModelState.AddModelError(string.Empty, "Kayıt sırasında bir hata oluştu.");
                return View(vm);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Guncelle(int? id, CancellationToken ct = default)
        {
            if (id == null) return NotFound();

            var profil = await _veliProfilService.GetByIdAsync(id.Value, ct);
            if (profil == null) return NotFound();

            return View(profil);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Guncelle(VeliProfilModel model, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                await _veliProfilService.GuncelleAsync(model, ct);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Veli profili güncellenirken hata oluştu.");
                ModelState.AddModelError(string.Empty, "Güncelleme sırasında bir hata oluştu.");
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Detay(int id, CancellationToken ct = default)
        {
            var veli = await _veliProfilService.GetByIdAsync(id, ct);
            if (veli == null) return NotFound();

            var ogrenciler = await _veliProfilService.GetOgrencilerAsync(id, ct);

            var vm = new VeliDetayVm
            {
                Veli = veli,
                Ogrenciler = ogrenciler
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sil(int id, CancellationToken ct = default)
        {
            try
            {
                await _veliProfilService.SilAsync(id, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Veli profili silinirken hata oluştu.");
                TempData["ErrMessage"] = "Veli profili silinirken bir hata oluştu.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
