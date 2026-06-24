using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OgrenciBilgiSistemi.Services.Interfaces;
using OgrenciBilgiSistemi.Shared.Enums;
using OgrenciBilgiSistemi.ViewModels;

namespace OgrenciBilgiSistemi.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class DuyurularController : Controller
    {
        private readonly IDuyuruService _duyuruService;
        private readonly ILogger<DuyurularController> _logger;

        public DuyurularController(IDuyuruService duyuruService, ILogger<DuyurularController> logger)
        {
            _duyuruService = duyuruService;
            _logger = logger;
        }

        private int OturumKullaniciId =>
            int.Parse(User.FindFirst("KullaniciId")!.Value);

        [HttpGet]
        public async Task<IActionResult> Index(int sayfaNo = 1, CancellationToken ct = default)
        {
            var paged = await _duyuruService.Listele(sayfaNo, 20, ct);
            return View(new DuyuruIndexVm { Duyurular = paged });
        }

        [HttpGet]
        public IActionResult Olustur() => View(new DuyuruOlusturVm());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Olustur(DuyuruOlusturVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
                return View(vm);

            try
            {
                await _duyuruService.Olustur(OturumKullaniciId, DuyuruHedefi.TumVeliler,
                    vm.Baslik, vm.Icerik, ct);
                TempData["Mesaj"] = "Duyuru başarıyla yayınlandı.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Duyuru oluşturulamadı.");
                ModelState.AddModelError("", "Duyuru oluşturulurken bir hata oluştu.");
                return View(vm);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Detay(int id, CancellationToken ct = default)
        {
            var duyuru = await _duyuruService.IdIleGetir(id, ct);
            if (duyuru is null) return NotFound();
            return View(DuyuruDetayVm.FromModel(duyuru));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sil(int id, CancellationToken ct = default)
        {
            try
            {
                await _duyuruService.Sil(id, ct);
                TempData["Mesaj"] = "Duyuru silindi.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Duyuru silinemedi. Id={Id}", id);
                TempData["Hata"] = "Duyuru silinirken bir hata oluştu.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
