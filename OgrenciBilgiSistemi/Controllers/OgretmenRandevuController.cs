using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OgrenciBilgiSistemi.Services.Interfaces;
using OgrenciBilgiSistemi.Shared.Enums;
using OgrenciBilgiSistemi.ViewModels;

namespace OgrenciBilgiSistemi.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class OgretmenRandevuController : Controller
    {
        private readonly IOgretmenRandevuService _ogretmenRandevuService;
        private readonly IKullaniciService _kullaniciService;
        private readonly ILogger<OgretmenRandevuController> _logger;

        public OgretmenRandevuController(
            IOgretmenRandevuService ogretmenRandevuService,
            IKullaniciService kullaniciService,
            ILogger<OgretmenRandevuController> logger)
        {
            _ogretmenRandevuService = ogretmenRandevuService;
            _kullaniciService = kullaniciService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int? ogretmenId, CancellationToken ct = default)
        {
            var vm = new OgretmenRandevuIndexVm
            {
                OgretmenId = ogretmenId,
                Ogretmenler = await _kullaniciService.GetKullanicilarByRolSelectListAsync(KullaniciRolu.Ogretmen, ct),
                Liste = ogretmenId.HasValue
                    ? await _ogretmenRandevuService.OgretmeneGoreListele(ogretmenId.Value, ct)
                    : [],
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Ekle(int? ogretmenId, CancellationToken ct = default)
        {
            var vm = new OgretmenRandevuFormVm
            {
                OgretmenKullaniciId = ogretmenId ?? 0,
                Ogretmenler = await _kullaniciService.GetKullanicilarByRolSelectListAsync(KullaniciRolu.Ogretmen, ct),
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ekle(OgretmenRandevuFormVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                vm.Ogretmenler = await _kullaniciService.GetKullanicilarByRolSelectListAsync(KullaniciRolu.Ogretmen, ct);
                return View(vm);
            }

            try
            {
                await _ogretmenRandevuService.Ekle(vm.ToModel(), ct);
                return RedirectToAction(nameof(Index), new { ogretmenId = vm.OgretmenKullaniciId });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                vm.Ogretmenler = await _kullaniciService.GetKullanicilarByRolSelectListAsync(KullaniciRolu.Ogretmen, ct);
                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Randevu takvimi eklenemedi.");
                ModelState.AddModelError("", "Randevu takvimi eklenirken bir hata oluştu.");
                vm.Ogretmenler = await _kullaniciService.GetKullanicilarByRolSelectListAsync(KullaniciRolu.Ogretmen, ct);
                return View(vm);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Guncelle(int id, CancellationToken ct = default)
        {
            var model = await _ogretmenRandevuService.Getir(id, ct);
            if (model is null) return NotFound();

            var vm = OgretmenRandevuFormVm.FromModel(model);
            vm.Ogretmenler = await _kullaniciService.GetKullanicilarByRolSelectListAsync(KullaniciRolu.Ogretmen, ct);

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Guncelle(OgretmenRandevuFormVm vm, CancellationToken ct = default)
        {
            if (vm.BitisSaati <= vm.BaslangicSaati)
                ModelState.AddModelError("", "Bitiş saati başlangıçtan büyük olmalıdır.");

            if (!ModelState.IsValid)
            {
                vm.Ogretmenler = await _kullaniciService.GetKullanicilarByRolSelectListAsync(KullaniciRolu.Ogretmen, ct);
                return View(vm);
            }

            try
            {
                await _ogretmenRandevuService.Guncelle(vm.ToModel(), ct);
                return RedirectToAction(nameof(Index), new { ogretmenId = vm.OgretmenKullaniciId });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                vm.Ogretmenler = await _kullaniciService.GetKullanicilarByRolSelectListAsync(KullaniciRolu.Ogretmen, ct);
                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Randevu takvimi güncellenemedi. Id={Id}", vm.OgretmenRandevuId);
                ModelState.AddModelError("", "Randevu takvimi güncellenirken bir hata oluştu.");
                vm.Ogretmenler = await _kullaniciService.GetKullanicilarByRolSelectListAsync(KullaniciRolu.Ogretmen, ct);
                return View(vm);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sil(int id, int ogretmenId, CancellationToken ct = default)
        {
            try
            {
                await _ogretmenRandevuService.Sil(id, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Randevu takvimi silinemedi. Id={Id}", id);
                TempData["Hata"] = "Randevu takvimi silinirken bir hata oluştu.";
            }

            return RedirectToAction(nameof(Index), new { ogretmenId });
        }
    }
}
