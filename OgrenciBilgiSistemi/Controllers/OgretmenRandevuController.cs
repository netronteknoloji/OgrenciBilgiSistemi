using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OgrenciBilgiSistemi.Data;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Services.Interfaces;
using OgrenciBilgiSistemi.Shared.Enums;

namespace OgrenciBilgiSistemi.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class OgretmenRandevuController : Controller
    {
        private readonly IOgretmenRandevuService _ogretmenRandevuService;
        private readonly AppDbContext _db;
        private readonly ILogger<OgretmenRandevuController> _logger;

        public OgretmenRandevuController(
            IOgretmenRandevuService ogretmenRandevuService,
            AppDbContext db,
            ILogger<OgretmenRandevuController> logger)
        {
            _ogretmenRandevuService = ogretmenRandevuService;
            _db = db;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int? ogretmenId, CancellationToken ct = default)
        {
            ViewData["OgretmenId"] = ogretmenId;
            ViewData["Ogretmenler"] = await OgretmenListesi(ct);

            if (ogretmenId.HasValue)
            {
                var liste = await _ogretmenRandevuService.OgretmeneGoreListele(ogretmenId.Value, ct);
                return View(liste);
            }

            return View(new List<OgretmenRandevuModel>());
        }

        [HttpGet]
        public async Task<IActionResult> Ekle(int? ogretmenId, CancellationToken ct = default)
        {
            ViewData["Ogretmenler"] = await OgretmenListesi(ct);
            var model = new OgretmenRandevuModel();
            if (ogretmenId.HasValue)
                model.OgretmenKullaniciId = ogretmenId.Value;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ekle(OgretmenRandevuModel model, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                ViewData["Ogretmenler"] = await OgretmenListesi(ct);
                return View(model);
            }

            try
            {
                await _ogretmenRandevuService.Ekle(model, ct);
                return RedirectToAction(nameof(Index), new { ogretmenId = model.OgretmenKullaniciId });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                ViewData["Ogretmenler"] = await OgretmenListesi(ct);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Randevu takvimi eklenemedi.");
                ModelState.AddModelError("", "Randevu takvimi eklenirken bir hata oluştu.");
                ViewData["Ogretmenler"] = await OgretmenListesi(ct);
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Guncelle(int id, CancellationToken ct = default)
        {
            var model = await _ogretmenRandevuService.Getir(id, ct);
            if (model is null) return NotFound();

            ViewData["Ogretmenler"] = await OgretmenListesi(ct);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Guncelle(OgretmenRandevuModel model, CancellationToken ct = default)
        {
            if (model.BitisSaati <= model.BaslangicSaati)
                ModelState.AddModelError("", "Bitiş saati başlangıçtan büyük olmalıdır.");

            if (!ModelState.IsValid)
            {
                ViewData["Ogretmenler"] = await OgretmenListesi(ct);
                return View(model);
            }

            try
            {
                await _ogretmenRandevuService.Guncelle(model, ct);
                return RedirectToAction(nameof(Index), new { ogretmenId = model.OgretmenKullaniciId });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                ViewData["Ogretmenler"] = await OgretmenListesi(ct);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Randevu takvimi güncellenemedi. Id={Id}", model.OgretmenRandevuId);
                ModelState.AddModelError("", "Randevu takvimi güncellenirken bir hata oluştu.");
                ViewData["Ogretmenler"] = await OgretmenListesi(ct);
                return View(model);
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

        private async Task<List<SelectListItem>> OgretmenListesi(CancellationToken ct)
        {
            return await _db.Kullanicilar
                .AsNoTracking()
                .Where(k => k.Rol == KullaniciRolu.Ogretmen && k.KullaniciDurum)
                .OrderBy(k => k.KullaniciAdi)
                .Select(k => new SelectListItem(k.KullaniciAdi, k.KullaniciId.ToString()))
                .ToListAsync(ct);
        }
    }
}
