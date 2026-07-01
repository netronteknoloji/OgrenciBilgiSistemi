using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
            var paged = await _kullaniciService.SearchPagedAsync(searchString, page, 50, ct);
            return View(new KullaniciIndexVm { Kullanicilar = paged, AramaMetni = searchString });
        }

        [HttpGet]
        public async Task<IActionResult> Ekle()
        {
            var vm = new KullaniciFormVm();
            await DropdownDoldur(vm);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ekle(KullaniciFormVm vm)
        {
            if (!string.IsNullOrEmpty(vm.Sifre) && (vm.Sifre.Length < 4 || vm.Sifre.Length > 50))
                ModelState.AddModelError(nameof(vm.Sifre), "Şifre 4-50 karakter olmalıdır.");

            if (!ModelState.IsValid)
            {
                await DropdownDoldur(vm);
                return View(vm);
            }

            if (await _kullaniciService.KullaniciAdiVarMiAsync(vm.KullaniciAdi))
            {
                ModelState.AddModelError(nameof(vm.KullaniciAdi), "Bu kullanıcı adı zaten kayıtlı.");
                await DropdownDoldur(vm);
                return View(vm);
            }

            try
            {
                await _kullaniciService.EkleAsync(vm.ToModel());
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await DropdownDoldur(vm);
                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı eklenirken hata oluştu.");
                ModelState.AddModelError(string.Empty, "Kayıt sırasında bir hata oluştu.");
                await DropdownDoldur(vm);
                return View(vm);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Guncelle(int? id, int? returnPage, string? returnFilter)
        {
            if (id == null) return NotFound();
            var kullanici = await _kullaniciService.GetByIdAsync(id.Value);
            if (kullanici == null) return NotFound();

            KullaniciBaseFormVm vm;
            switch (kullanici.Rol)
            {
                case KullaniciRolu.Ogretmen:
                    var ogVm = OgretmenKullaniciFormVm.FromModel(kullanici);
                    ogVm.Birimler = await _kullaniciService.GetBirimlerSelectListAsync();
                    vm = ogVm;
                    break;
                case KullaniciRolu.Servis:
                    vm = ServisKullaniciFormVm.FromModel(kullanici);
                    break;
                case KullaniciRolu.Veli:
                    vm = VeliKullaniciFormVm.FromModel(kullanici);
                    break;
                default:
                    var adminVm = KullaniciFormVm.FromModel(kullanici);
                    await DropdownDoldur(adminVm);
                    vm = adminVm;
                    break;
            }

            vm.ReturnPage = returnPage;
            vm.ReturnFilter = returnFilter;
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Guncelle(KullaniciFormVm vm)
        {
            vm.FormAction = "Guncelle"; vm.SubmitText = "Güncelle"; vm.IncludeId = true;
            ModelState.Remove(nameof(vm.Sifre));

            if (!string.IsNullOrWhiteSpace(vm.Sifre) && (vm.Sifre.Length < 4 || vm.Sifre.Length > 50))
                ModelState.AddModelError(nameof(vm.Sifre), "Şifre 4-50 karakter olmalıdır.");

            if (!ModelState.IsValid)
            {
                await DropdownDoldur(vm);
                return View(vm);
            }

            if (await _kullaniciService.KullaniciAdiVarMiAsync(vm.KullaniciAdi, vm.KullaniciId))
            {
                ModelState.AddModelError(nameof(vm.KullaniciAdi), "Bu kullanıcı adı zaten kayıtlı.");
                await DropdownDoldur(vm);
                return View(vm);
            }

            try
            {
                await _kullaniciService.GuncelleAsync(vm.ToModel());
                TempData["GuncellenenId"] = vm.KullaniciId;
                var url = Url.Action(nameof(Index), new { page = vm.ReturnPage, searchString = vm.ReturnFilter });
                return Redirect(url + $"#kullanici-{vm.KullaniciId}");
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await DropdownDoldur(vm);
                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı güncellenirken hata oluştu.");
                ModelState.AddModelError(string.Empty, "Güncelleme sırasında bir hata oluştu.");
                await DropdownDoldur(vm);
                return View(vm);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuncelleOgretmen(OgretmenKullaniciFormVm vm)
        {
            vm.FormAction = "GuncelleOgretmen"; vm.SubmitText = "Güncelle"; vm.IncludeId = true;
            ModelState.Remove(nameof(vm.Sifre));

            if (!string.IsNullOrWhiteSpace(vm.Sifre) && (vm.Sifre.Length < 4 || vm.Sifre.Length > 50))
                ModelState.AddModelError(nameof(vm.Sifre), "Şifre 4-50 karakter olmalıdır.");

            if (!ModelState.IsValid)
            {
                vm.Birimler = await _kullaniciService.GetBirimlerSelectListAsync();
                return View("Guncelle", vm);
            }

            if (await _kullaniciService.KullaniciAdiVarMiAsync(vm.KullaniciAdi, vm.KullaniciId))
            {
                ModelState.AddModelError(nameof(vm.KullaniciAdi), "Bu kullanıcı adı zaten kayıtlı.");
                vm.Birimler = await _kullaniciService.GetBirimlerSelectListAsync();
                return View("Guncelle", vm);
            }

            try
            {
                await _kullaniciService.GuncelleAsync(vm.ToModel());
                TempData["GuncellenenId"] = vm.KullaniciId;
                var url = Url.Action(nameof(Index), new { page = vm.ReturnPage, searchString = vm.ReturnFilter });
                return Redirect(url + $"#kullanici-{vm.KullaniciId}");
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                vm.Birimler = await _kullaniciService.GetBirimlerSelectListAsync();
                return View("Guncelle", vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı güncellenirken hata oluştu.");
                ModelState.AddModelError(string.Empty, "Güncelleme sırasında bir hata oluştu.");
                vm.Birimler = await _kullaniciService.GetBirimlerSelectListAsync();
                return View("Guncelle", vm);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuncelleServis(ServisKullaniciFormVm vm)
        {
            vm.FormAction = "GuncelleServis"; vm.SubmitText = "Güncelle"; vm.IncludeId = true;
            ModelState.Remove(nameof(vm.Sifre));

            if (!string.IsNullOrWhiteSpace(vm.Sifre) && (vm.Sifre.Length < 4 || vm.Sifre.Length > 50))
                ModelState.AddModelError(nameof(vm.Sifre), "Şifre 4-50 karakter olmalıdır.");

            if (!ModelState.IsValid)
                return View("Guncelle", vm);

            if (await _kullaniciService.KullaniciAdiVarMiAsync(vm.KullaniciAdi, vm.KullaniciId))
            {
                ModelState.AddModelError(nameof(vm.KullaniciAdi), "Bu kullanıcı adı zaten kayıtlı.");
                return View("Guncelle", vm);
            }

            try
            {
                await _kullaniciService.GuncelleAsync(vm.ToModel());
                TempData["GuncellenenId"] = vm.KullaniciId;
                var url = Url.Action(nameof(Index), new { page = vm.ReturnPage, searchString = vm.ReturnFilter });
                return Redirect(url + $"#kullanici-{vm.KullaniciId}");
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View("Guncelle", vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı güncellenirken hata oluştu.");
                ModelState.AddModelError(string.Empty, "Güncelleme sırasında bir hata oluştu.");
                return View("Guncelle", vm);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuncelleVeli(VeliKullaniciFormVm vm)
        {
            vm.FormAction = "GuncelleVeli"; vm.SubmitText = "Güncelle"; vm.IncludeId = true;
            ModelState.Remove(nameof(vm.Sifre));

            if (!string.IsNullOrWhiteSpace(vm.Sifre) && (vm.Sifre.Length < 4 || vm.Sifre.Length > 50))
                ModelState.AddModelError(nameof(vm.Sifre), "Şifre 4-50 karakter olmalıdır.");

            if (!ModelState.IsValid)
                return View("Guncelle", vm);

            if (await _kullaniciService.KullaniciAdiVarMiAsync(vm.KullaniciAdi, vm.KullaniciId))
            {
                ModelState.AddModelError(nameof(vm.KullaniciAdi), "Bu kullanıcı adı zaten kayıtlı.");
                return View("Guncelle", vm);
            }

            try
            {
                await _kullaniciService.GuncelleAsync(vm.ToModel());
                TempData["GuncellenenId"] = vm.KullaniciId;
                var url = Url.Action(nameof(Index), new { page = vm.ReturnPage, searchString = vm.ReturnFilter });
                return Redirect(url + $"#kullanici-{vm.KullaniciId}");
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View("Guncelle", vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı güncellenirken hata oluştu.");
                ModelState.AddModelError(string.Empty, "Güncelleme sırasında bir hata oluştu.");
                return View("Guncelle", vm);
            }
        }

        private async Task DropdownDoldur(KullaniciFormVm vm)
        {
            vm.Servisler = await _kullaniciService.GetServislerSelectListAsync();
            vm.Birimler = await _kullaniciService.GetBirimlerSelectListAsync();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sil(int id)
        {
            try
            {
                await _kullaniciService.SilAsync(id);
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrMessage"] = ex.Message;
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
