using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        private const string KullaniciAdiKullaniliyorMesaji =
            "Bu kullanıcı adı kullanılıyor. Farklı bir kullanıcı adı girin.";

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
            var paged = await _veliProfilService.SearchPagedAsync(searchString, page, 50, ct);
            return View(new VeliIndexVm { Veliler = paged, AramaMetni = searchString });
        }

        [HttpGet]
        public IActionResult Ekle()
        {
            return View(new VeliFormVm());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ekle(VeliFormVm vm, CancellationToken ct = default)
        {
            if (!string.IsNullOrWhiteSpace(vm.KullaniciAdi) &&
                await _kullaniciService.KullaniciAdiVarMiAsync(vm.KullaniciAdi, null, ct))
                ModelState.AddModelError(nameof(vm.KullaniciAdi), KullaniciAdiKullaniliyorMesaji);

            if (!ModelState.IsValid)
                return View(vm);

            try
            {
                await _veliProfilService.EkleKullaniciVeProfilAsync(vm.ToEkleVm(), ct);
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

            return View(VeliFormVm.FromModel(profil));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Guncelle(VeliFormVm vm, CancellationToken ct = default)
        {
            vm.FormAction = "Guncelle"; vm.SubmitText = "Güncelle";
            ModelState.Remove(nameof(vm.Sifre));

            if (!string.IsNullOrWhiteSpace(vm.KullaniciAdi) &&
                await _kullaniciService.KullaniciAdiVarMiAsync(vm.KullaniciAdi, vm.KullaniciId, ct))
                ModelState.AddModelError(nameof(vm.KullaniciAdi), KullaniciAdiKullaniliyorMesaji);

            if (!ModelState.IsValid)
                return View(vm);

            try
            {
                await _veliProfilService.GuncelleAsync(vm.ToProfilModel(), vm.KullaniciAdi, vm.Telefon, vm.Sifre, ct);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Veli profili güncellenirken hata oluştu.");
                ModelState.AddModelError(string.Empty, "Güncelleme sırasında bir hata oluştu.");
                return View(vm);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Detay(int id, CancellationToken ct = default)
        {
            var veli = await _veliProfilService.GetByIdAsync(id, ct);
            if (veli == null) return NotFound();

            var ogrenciler = await _veliProfilService.GetOgrencilerAsync(id, ct);
            return View(new VeliDetayVm { Veli = veli, Ogrenciler = ogrenciler });
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
