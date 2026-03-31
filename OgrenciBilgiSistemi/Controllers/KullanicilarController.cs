using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Services.Interfaces;
using OgrenciBilgiSistemi.ViewModels;

namespace OgrenciBilgiSistemi.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class KullanicilarController : Controller
    {
        private readonly IKullaniciService _kullaniciService;
        private readonly ILogger<KullanicilarController> _logger;

        public KullanicilarController(IKullaniciService kullaniciService, ILogger<KullanicilarController> logger)
        {
            _kullaniciService = kullaniciService;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string searchString, int page = 1, CancellationToken ct = default)
        {
            ViewData["CurrentFilter"] = searchString;
            var paged = await _kullaniciService.SearchPagedAsync(searchString, page, 50, ct);
            return View(paged);
        }

        [HttpGet]
        public async Task<IActionResult> Ekle()
        {
            var model = new KullaniciModel();
            await DropdownDoldur(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ekle(KullaniciModel model)
        {
            if (!ModelState.IsValid)
            {
                await DropdownDoldur(model);
                return View(model);
            }

            if (await _kullaniciService.KullaniciAdiVarMiAsync(model.KullaniciAdi))
            {
                ModelState.AddModelError(nameof(model.KullaniciAdi), "Bu kullanıcı adı zaten kayıtlı.");
                await DropdownDoldur(model);
                return View(model);
            }

            try
            {
                await _kullaniciService.EkleAsync(model);
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await DropdownDoldur(model);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı eklenirken hata oluştu.");
                ModelState.AddModelError(string.Empty, "Kayıt sırasında bir hata oluştu.");
                await DropdownDoldur(model);
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Guncelle(int? id, int? returnPage, string? returnFilter)
        {
            if (id == null) return NotFound();

            var kullanici = await _kullaniciService.GetByIdAsync(id.Value);
            if (kullanici == null) return NotFound();

            kullanici.Sifre = string.Empty;
            ViewData["ReturnPage"] = returnPage;
            ViewData["ReturnFilter"] = returnFilter;
            await DropdownDoldur(kullanici);
            return View(kullanici);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Guncelle(KullaniciModel model, int? returnPage, string? returnFilter)
        {
            ModelState.Remove(nameof(model.Sifre));

            if (!ModelState.IsValid)
            {
                ViewData["ReturnPage"] = returnPage;
                ViewData["ReturnFilter"] = returnFilter;
                await DropdownDoldur(model);
                return View(model);
            }

            if (await _kullaniciService.KullaniciAdiVarMiAsync(model.KullaniciAdi, model.KullaniciId))
            {
                ModelState.AddModelError(nameof(model.KullaniciAdi), "Bu kullanıcı adı zaten kayıtlı.");
                ViewData["ReturnPage"] = returnPage;
                ViewData["ReturnFilter"] = returnFilter;
                await DropdownDoldur(model);
                return View(model);
            }

            try
            {
                await _kullaniciService.GuncelleAsync(model);
                TempData["GuncellenenId"] = model.KullaniciId;
                var url = Url.Action(nameof(Index), new { page = returnPage, searchString = returnFilter });
                return Redirect(url + $"#kullanici-{model.KullaniciId}");
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await DropdownDoldur(model);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı güncellenirken hata oluştu.");
                ModelState.AddModelError(string.Empty, "Güncelleme sırasında bir hata oluştu.");
                await DropdownDoldur(model);
                return View(model);
            }
        }

        private async Task DropdownDoldur(KullaniciModel model)
        {
            model.Servisler = await _kullaniciService.GetServislerSelectListAsync();
            ViewBag.Birimler = await _kullaniciService.GetBirimlerSelectListAsync();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sil(int id)
        {
            try
            {
                await _kullaniciService.SilAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı silinirken hata oluştu.");
                TempData["ErrMessage"] = "Kullanıcı silinirken bir hata oluştu.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> YetkiGuncelle(int id)
        {
            var vm = await _kullaniciService.GetYetkiVmAsync(id);
            if (vm == null) return NotFound();
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> YetkiGuncelle(KullaniciMenuAtamaVm model)
        {
            try
            {
                await _kullaniciService.YetkiGuncelleAsync(model.KullaniciId, model.SelectedMenuIds);
                TempData["OkMessage"] = "Yetkiler güncellendi.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Yetki güncellenirken hata oluştu.");
                TempData["ErrMessage"] = "Yetkiler güncellenirken bir hata oluştu.";
            }
            return RedirectToAction(nameof(YetkiGuncelle), new { id = model.KullaniciId });
        }
    }
}
