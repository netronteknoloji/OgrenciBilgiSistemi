using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OgrenciBilgiSistemi.Data;
using OgrenciBilgiSistemi.Models;

namespace OgrenciBilgiSistemi.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class ServislerController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ServislerController> _logger;

        public ServislerController(AppDbContext context, ILogger<ServislerController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string searchString, int page = 1, CancellationToken ct = default)
        {
            ViewData["CurrentFilter"] = searchString;

            var query = _context.Servisler
                .Include(s => s.Kullanici)
                .Include(s => s.Ogrenciler)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var s = searchString.Trim();
                query = query.Where(srv => srv.Plaka.Contains(s) ||
                    (srv.Kullanici != null && srv.Kullanici.KullaniciAdi.Contains(s)));
            }

            var paged = await SayfalanmisListeModel<ServisModel>
                .CreateAsync(query.OrderBy(s => s.Plaka), page, 20, ct);

            return View(paged);
        }

        [HttpGet]
        public async Task<IActionResult> Ekle()
        {
            ViewBag.Kullanicilar = await GetSoforSelectList();
            return View(new ServisModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ekle(ServisModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Kullanicilar = await GetSoforSelectList();
                return View(model);
            }

            try
            {
                _context.Servisler.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Servis eklenirken hata oluştu.");
                ModelState.AddModelError(string.Empty, "Kayıt sırasında bir hata oluştu.");
                ViewBag.Kullanicilar = await GetSoforSelectList();
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Guncelle(int? id)
        {
            if (id == null) return NotFound();

            var servis = await _context.Servisler.FindAsync(id);
            if (servis == null) return NotFound();

            ViewBag.Kullanicilar = await GetSoforSelectList(servis.KullaniciId);
            return View(servis);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Guncelle(ServisModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Kullanicilar = await GetSoforSelectList(model.KullaniciId);
                return View(model);
            }

            try
            {
                var servis = await _context.Servisler.FindAsync(model.ServisId);
                if (servis == null) return NotFound();

                servis.Plaka = model.Plaka;
                servis.KullaniciId = model.KullaniciId;
                servis.ServisDurum = model.ServisDurum;

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Servis güncellenirken hata oluştu.");
                ModelState.AddModelError(string.Empty, "Güncelleme sırasında bir hata oluştu.");
                ViewBag.Kullanicilar = await GetSoforSelectList(model.KullaniciId);
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sil(int id)
        {
            try
            {
                var servis = await _context.Servisler.FindAsync(id);
                if (servis == null) return NotFound();

                servis.ServisDurum = false;
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Servis silinirken hata oluştu.");
                TempData["ErrMessage"] = "Servis silinirken bir hata oluştu.";
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Şoför rolündeki kullanıcıları dropdown listesi olarak döner.
        /// </summary>
        private async Task<List<SelectListItem>> GetSoforSelectList(int? selectedId = null)
        {
            var kullanicilar = await _context.Kullanicilar
                .AsNoTracking()
                .Where(k => k.KullaniciDurum && k.Rol == KullaniciRolu.Sofor)
                .OrderBy(k => k.KullaniciAdi)
                .Select(k => new SelectListItem
                {
                    Value = k.KullaniciId.ToString(),
                    Text = k.KullaniciAdi,
                    Selected = selectedId.HasValue && k.KullaniciId == selectedId.Value
                })
                .ToListAsync();

            return kullanicilar;
        }
    }
}
