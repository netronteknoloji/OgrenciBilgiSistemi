using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OgrenciBilgiSistemi.Services.Interfaces;
using OgrenciBilgiSistemi.ViewModels;

namespace OgrenciBilgiSistemi.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class ServislerController : Controller
    {
        private readonly IServisProfilService _servisProfilService;
        private readonly ILogger<ServislerController> _logger;

        public ServislerController(
            IServisProfilService servisProfilService,
            ILogger<ServislerController> logger)
        {
            _servisProfilService = servisProfilService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string searchString, int page = 1, CancellationToken ct = default)
        {
            var paged = await _servisProfilService.SearchPagedAsync(searchString, page, 50, ct);
            return View(new ServisIndexVm { Servisler = paged, AramaMetni = searchString });
        }

        [HttpGet]
        public IActionResult Ekle()
        {
            return View(new ServisFormVm());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ekle(ServisFormVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
                return View(vm);

            try
            {
                await _servisProfilService.EkleKullaniciVeProfilAsync(vm.ToEkleVm(), ct);
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

            return View(ServisFormVm.FromModel(profil));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Guncelle(ServisFormVm vm, CancellationToken ct = default)
        {
            vm.FormAction = "Guncelle"; vm.SubmitText = "Güncelle";
            ModelState.Remove(nameof(vm.Sifre));

            if (!ModelState.IsValid)
                return View(vm);

            try
            {
                await _servisProfilService.GuncelleAsync(vm.ToProfilModel(), vm.KullaniciAdi, vm.Telefon, vm.Sifre, ct);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Servis profili güncellenirken hata oluştu.");
                ModelState.AddModelError(string.Empty, "Güncelleme sırasında bir hata oluştu.");
                return View(vm);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Detay(int id, CancellationToken ct = default)
        {
            var servis = await _servisProfilService.GetByIdAsync(id, ct);
            if (servis == null) return NotFound();

            var ogrenciler = await _servisProfilService.GetOgrencilerAsync(id, ct);

            return View(new ServisDetayVm { Servis = servis, Ogrenciler = ogrenciler });
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
