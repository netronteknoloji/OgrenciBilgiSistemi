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
        public async Task<IActionResult> Guncelle(ServisProfilModel model, string? kullaniciAdi, string? telefon, string? sifre, CancellationToken ct = default)
        {
            ModelState.Remove(nameof(sifre));

            if (!ModelState.IsValid)
                return View(model);

            try
            {
                await _servisProfilService.GuncelleAsync(model, kullaniciAdi, telefon, sifre, ct);
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

        [HttpGet]
        public async Task<IActionResult> OgrenciAra(int servisId, string q, CancellationToken ct = default)
        {
            var sonuclar = await _servisProfilService.AtanmamisOgrenciAraAsync(servisId, q, ct);
            var json = sonuclar.Select(o => new
            {
                o.OgrenciId,
                o.OgrenciAdSoyad,
                o.OgrenciNo,
                BirimAd = o.Birim?.BirimAd ?? "-"
            });
            return Json(json);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OgrenciAta(int servisId, int ogrenciId, CancellationToken ct = default)
        {
            try
            {
                await _servisProfilService.OgrenciAtaAsync(servisId, ogrenciId, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Öğrenci servise atanırken hata oluştu.");
                TempData["ErrMessage"] = "Öğrenci atanırken bir hata oluştu.";
            }
            return RedirectToAction(nameof(Detay), new { id = servisId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OgrenciCikar(int servisId, int ogrenciId, CancellationToken ct = default)
        {
            try
            {
                await _servisProfilService.OgrenciCikarAsync(servisId, ogrenciId, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Öğrenci servisten çıkarılırken hata oluştu.");
                TempData["ErrMessage"] = "Öğrenci çıkarılırken bir hata oluştu.";
            }
            return RedirectToAction(nameof(Detay), new { id = servisId });
        }
    }
}
