using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OgrenciBilgiSistemi.Data;
using OgrenciBilgiSistemi.Models;

namespace OgrenciBilgiSistemi.Controllers
{
    [AutoValidateAntiforgeryToken]
    public class CihazlarController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CihazlarController> _logger;
        private readonly ICihazService _cihazService;

        public CihazlarController(
            AppDbContext context,
            ILogger<CihazlarController> logger,
            ICihazService cihazService)
        {
            _context = context;
            _logger = logger;
            _cihazService = cihazService;
        }

        // 📌 Tüm cihazları listele (yalnızca Aktif = true)
        public async Task<IActionResult> Index(string searchString, int page = 1, CancellationToken ct = default)
        {
            ViewData["CurrentFilter"] = searchString;

            var query = _context.Cihazlar
                                .Where(c => c.Aktif)
                                .AsNoTracking()
                                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var s = searchString.Trim();
                if (Guid.TryParse(s, out var g))
                {
                    query = query.Where(c => c.CihazKodu == g);
                }
                else
                {
                    query = query.Where(c =>
                        EF.Functions.Like(c.CihazAdi, $"%{s}%") ||
                        EF.Functions.Like(c.IpAdresi ?? "", $"%{s}%"));
                }
            }

            query = query.OrderBy(c => c.CihazAdi).ThenBy(c => c.CihazId);

            var paged = await SayfalanmisListeModel<CihazModel>.CreateAsync(query, page, 10, ct);
            return View(paged);
        }

        // ➕ Yeni cihaz ekleme (GET)
        [HttpGet]
        public IActionResult Ekle() => View();

        // ➕ Yeni cihaz ekleme (POST) -> her zaman Aktif = true
        [HttpPost]
        public async Task<IActionResult> Ekle(CihazModel model, CancellationToken ct = default)
        {
            // View tarafı için temel ModelState doğrulaması kalsın (örn. [Required] vs.)
            if (!ModelState.IsValid) return View(model);

            var ok = await _cihazService.CihazEkleAsync(model, ct);
            if (!ok)
            {
                TempData["Error"] = "Cihaz kaydedilemedi. Bilgileri kontrol edin.";
                return View(model);
            }

            TempData["Success"] = "Cihaz başarıyla kaydedildi.";
            return RedirectToAction(nameof(Index));
        }

        // ✏️ Cihaz güncelleme (GET)
        [HttpGet]
        public async Task<IActionResult> Guncelle(int id, CancellationToken ct = default)
        {
            var cihaz = await _cihazService.CihazGetByIdAsync(id, ct);
            if (cihaz is null) return NotFound();
            return View(cihaz);
        }

        // Cihaz güncelleme (POST)
        [HttpPost]
        public async Task<IActionResult> Guncelle(CihazModel model, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                TempData["Warning"] = "Lütfen formu kontrol edin.";
                return View(model);
            }

            var ok = await _cihazService.CihazGuncelleAsync(model, ct);
            if (!ok)
            {
                TempData["Error"] = "Cihaz güncellenemedi. Bilgileri kontrol edin.";
                return View(model);
            }

            TempData["Success"] = "Cihaz başarıyla güncellendi.";
            return RedirectToAction(nameof(Index));
        }

        //  Cihaz silme onayı (GET)
        [HttpGet]
        public async Task<IActionResult> Sil(int id, CancellationToken ct = default)
        {
            var cihaz = await _cihazService.CihazGetByIdAsync(id, ct);
            if (cihaz is null)
            {
                _logger.LogWarning("⚠️ Silinmek istenen cihaz bulunamadı: ID {Id}", id);
                return NotFound();
            }
            return View(cihaz);
        }


        //  Cihaz silme (POST) -> soft delete (Aktif = false)
        [HttpPost, ActionName("Sil")]
        public async Task<IActionResult> SilConfirmed(int id, CancellationToken ct = default)
        {
            var ok = await _cihazService.CihazSilAsync(id, ct);

            TempData[ok ? "Success" : "Error"] = ok
                ? "Cihaz başarıyla silindi."
                : "Silme işlemi sırasında beklenmeyen bir hata oluştu.";

            return RedirectToAction(nameof(Index));
        }

        // Cihazdaki TÜM kullanıcıları listele (GET)
        [HttpGet]
        public async Task<IActionResult> TumKullanicilariListele(int cihazId, CancellationToken ct = default)
        {
            try
            {
                var users = await _cihazService.CihazdanKullanicilariListeleAsync(cihazId, ct);
                ViewBag.CihazId = cihazId;
                return View(users); // Views/Cihazlar/TumKullanicilariListele.cshtml
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Kullanıcı listesi alınırken hata (cihazId={Id})", cihazId);
                TempData["Error"] = "Kullanıcı listesi alınamadı.";
                return RedirectToAction(nameof(Index));
            }
        }

        //  Cihazdaki TÜM kullanıcıları SİL (POST)
        [HttpPost]
        public async Task<IActionResult> TumKullanicilariSil(int cihazId, CancellationToken ct = default)
        {
            try
            {
                var ok = await _cihazService.CihazdakiTumKullanicilariSilAsync(cihazId, ct);
                TempData[ok ? "Message" : "Error"] = ok
                    ? "Cihazdaki tüm kullanıcılar başarıyla silindi."
                    : "Kullanıcılar silinemedi. Cihaz bağlantısını/firmware’i kontrol edin.";
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Tüm kullanıcılar silinirken hata (cihazId={Id})", cihazId);
                TempData["Error"] = "Silme işlemi sırasında beklenmeyen bir hata oluştu.";
            }

            return RedirectToAction(nameof(TumKullanicilariListele), new { cihazId });
        }

    }
}
