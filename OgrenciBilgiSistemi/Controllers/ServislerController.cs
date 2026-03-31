using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Services.Interfaces;
using OgrenciBilgiSistemi.ViewModels;
using OgrenciBilgiSistemi.Shared.Enums;

namespace OgrenciBilgiSistemi.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class ServislerController : Controller
    {
        private readonly IServisProfilService _servisProfilService;
        private readonly IKullaniciService _kullaniciService;
        private readonly ILogger<ServislerController> _logger;

        public ServislerController(
            IServisProfilService servisProfilService,
            IKullaniciService kullaniciService,
            ILogger<ServislerController> logger)
        {
            _servisProfilService = servisProfilService;
            _kullaniciService = kullaniciService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string searchString, int page = 1, CancellationToken ct = default)
        {
            ViewData["CurrentFilter"] = searchString;
            var paged = await _servisProfilService.SearchPagedAsync(searchString, page, 50, ct);
            return View(paged);
        }

        [HttpGet]
        public IActionResult Ekle()
        {
            return View(new ServisEkleVm());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ekle(ServisEkleVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
                return View(vm);

            try
            {
                await _servisProfilService.EkleKullaniciVeProfilAsync(vm, ct);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Servis eklenirken hata oluştu.");
                ModelState.AddModelError(string.Empty, "Kayıt sırasında bir hata oluştu.");
                return View(vm);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Guncelle(int? id, CancellationToken ct = default)
        {
            if (id == null) return NotFound();

            var profil = await _servisProfilService.GetByIdAsync(id.Value, ct);
            if (profil == null) return NotFound();

            return View(profil);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Guncelle(ServisProfilModel model, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                await _servisProfilService.GuncelleAsync(model, ct);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Servis profili güncellenirken hata oluştu.");
                ModelState.AddModelError(string.Empty, "Güncelleme sırasında bir hata oluştu.");
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Detay(int id, CancellationToken ct = default)
        {
            var servis = await _servisProfilService.GetByIdAsync(id, ct);
            if (servis == null) return NotFound();

            var ogrenciler = await _servisProfilService.GetOgrencilerAsync(id, ct);

            var vm = new ServisDetayVm
            {
                Servis = servis,
                Ogrenciler = ogrenciler
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sil(int id)
        {
            try
            {
                await _servisProfilService.SilAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Servis profili silinirken hata oluştu.");
                TempData["ErrMessage"] = "Servis profili silinirken bir hata oluştu.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
