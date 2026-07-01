using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OgrenciBilgiSistemi.Services.Interfaces;
using OgrenciBilgiSistemi.ViewModels;

namespace OgrenciBilgiSistemi.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class OgretmenlerController : Controller
    {
        private readonly IOgretmenProfilService _ogretmenProfilService;
        private readonly IKullaniciService _kullaniciService;
        private readonly ILogger<OgretmenlerController> _logger;

        public OgretmenlerController(
            IOgretmenProfilService ogretmenProfilService,
            IKullaniciService kullaniciService,
            ILogger<OgretmenlerController> logger)
        {
            _ogretmenProfilService = ogretmenProfilService;
            _kullaniciService = kullaniciService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string searchString, int page = 1,
            OgretmenFiltre durum = OgretmenFiltre.Aktif, CancellationToken ct = default)
        {
            var paged = await _ogretmenProfilService.SearchPagedAsync(searchString, page, 50, durum, ct);
            return View(new OgretmenIndexVm { Ogretmenler = paged, AramaMetni = searchString, Durum = durum });
        }

        [HttpGet]
        public async Task<IActionResult> Ekle(CancellationToken ct = default)
        {
            var vm = new OgretmenFormVm();
            vm.Birimler = await _kullaniciService.GetBirimlerSelectListAsync(ct);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ekle(OgretmenFormVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                vm.Birimler = await _kullaniciService.GetBirimlerSelectListAsync(ct);
                return View(vm);
            }

            try
            {
                await _ogretmenProfilService.EkleKullaniciVeProfilAsync(vm.ToEkleVm(), ct);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Öğretmen eklenirken hata oluştu.");
                ModelState.AddModelError(string.Empty, "Kayıt sırasında bir hata oluştu.");
                vm.Birimler = await _kullaniciService.GetBirimlerSelectListAsync(ct);
                return View(vm);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Guncelle(int? id, CancellationToken ct = default)
        {
            if (id == null) return NotFound();

            var profil = await _ogretmenProfilService.GetByIdAsync(id.Value, ct);
            if (profil == null) return NotFound();

            var vm = OgretmenFormVm.FromModel(profil);
            vm.Birimler = await _kullaniciService.GetBirimlerSelectListAsync(ct);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Guncelle(OgretmenFormVm vm, CancellationToken ct = default)
        {
            vm.FormAction = "Guncelle"; vm.SubmitText = "Güncelle";
            ModelState.Remove(nameof(vm.Sifre));

            if (!ModelState.IsValid)
            {
                vm.Birimler = await _kullaniciService.GetBirimlerSelectListAsync(ct);
                return View(vm);
            }

            try
            {
                await _ogretmenProfilService.GuncelleAsync(vm.ToProfilModel(), vm.KullaniciAdi, vm.Telefon, vm.Sifre, ct);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Öğretmen profili güncellenirken hata oluştu.");
                ModelState.AddModelError(string.Empty, "Güncelleme sırasında bir hata oluştu.");
                vm.Birimler = await _kullaniciService.GetBirimlerSelectListAsync(ct);
                return View(vm);
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
