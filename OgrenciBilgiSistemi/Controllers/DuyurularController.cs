using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Services.Interfaces;
using OgrenciBilgiSistemi.Shared.Enums;

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
            return View(paged);
        }

        [HttpGet]
        public IActionResult Olustur()
        {
            return View(new DuyuruModel { Hedef = DuyuruHedefi.TumVeliler });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Olustur(DuyuruModel model, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                await _duyuruService.Olustur(OturumKullaniciId, DuyuruHedefi.TumVeliler,
                    model.Baslik, model.Icerik, ct);
                TempData["Mesaj"] = "Duyuru başarıyla yayınlandı.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Duyuru oluşturulamadı.");
                ModelState.AddModelError("", "Duyuru oluşturulurken bir hata oluştu.");
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Detay(int id, CancellationToken ct = default)
        {
            var duyuru = await _duyuruService.IdIleGetir(id, ct);
            if (duyuru is null) return NotFound();

            return View(duyuru);
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
