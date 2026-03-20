using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OgrenciBilgiSistemi.Data;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.ViewModels;

namespace OgrenciBilgiSistemi.Controllers
{
    [Authorize(Policy = "AdminOnly")]
   public class KullanicilarController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PasswordHasher<KullaniciModel> _passwordHasher;
        private readonly ILogger<KullanicilarController> _logger;

        public KullanicilarController(AppDbContext context, ILogger<KullanicilarController> logger)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<KullaniciModel>();
            _logger = logger;
        }

        // GET: Kullanici
        public async Task<IActionResult> Index(string searchString, int page = 1, CancellationToken ct = default)
        {
            ViewData["CurrentFilter"] = searchString;
            var query = _context.Kullanicilar
                                .Include(k => k.Birim)
                                .AsNoTracking()
                                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
                query = query.Where(k => k.KullaniciAdi.Contains(searchString));

            var paged = await SayfalanmisListeModel<KullaniciModel>.CreateAsync(query.OrderBy(k => k.KullaniciAdi), page, 10,ct);
            return View(paged);
        }


        // GET: Kullanici/Details/5
        public async Task<IActionResult> Detay(int? id)
        {
            if (id == null) return NotFound();

            var kullanici = await _context.Kullanicilar
                .AsNoTracking()
                .Include(k => k.Birim)
                .FirstOrDefaultAsync(m => m.KullaniciId == id);

            if (kullanici == null) return NotFound();

            return View(kullanici);
        }

        // GET: Kullanici/Ekle
        [HttpGet]
        public async Task<IActionResult> Ekle()
        {
            var model = new KullaniciModel
            {
                Birimler = await GetBirimlerSelectList()
            };
            return View(model);
        }

        // POST: Kullanici/Ekle
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ekle(KullaniciModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Birimler = await GetBirimlerSelectList();
                return View(model);
            }

            // 🔐 KullaniciAdi benzersiz olsun
            var exists = await _context.Kullanicilar
                .AnyAsync(k => k.KullaniciAdi == model.KullaniciAdi && k.KullaniciDurum);
            if (exists)
            {
                ModelState.AddModelError(nameof(model.KullaniciAdi), "Bu kullanıcı adı zaten kayıtlı.");
                model.Birimler = await GetBirimlerSelectList();
                return View(model);
            }

            try
            {
                model.Sifre = _passwordHasher.HashPassword(model, model.Sifre);
                _context.Kullanicilar.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı eklenirken hata oluştu.");
                ModelState.AddModelError(string.Empty, "Kayıt sırasında bir hata oluştu.");
                model.Birimler = await GetBirimlerSelectList();
                return View(model);
            }
        }

        // GET: Kullanici/Guncelle/5
        [HttpGet]
        public async Task<IActionResult> Guncelle(int? id)
        {
            if (id == null) return NotFound();

            var kullanici = await _context.Kullanicilar.FindAsync(id);
            if (kullanici == null) return NotFound();

            // ❗ Şifre hash'inin view'a sızmaması için boşalt
            kullanici.Sifre = string.Empty;

            kullanici.Birimler = await GetBirimlerSelectList();
            return View(kullanici);
        }

        // POST: Kullanici/Guncelle/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Guncelle(KullaniciModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Birimler = await GetBirimlerSelectList();
                return View(model);
            }

            try
            {
                var kullanici = await _context.Kullanicilar.FindAsync(model.KullaniciId);
                if (kullanici == null) return NotFound();

                // Kullanıcı adını güncellerken çakışma kontrolü
                var exists = await _context.Kullanicilar
                    .AnyAsync(k => k.KullaniciId != model.KullaniciId &&
                                   k.KullaniciAdi == model.KullaniciAdi &&
                                   k.KullaniciDurum);
                if (exists)
                {
                    ModelState.AddModelError(nameof(model.KullaniciAdi), "Bu kullanıcı adı zaten kayıtlı.");
                    model.Birimler = await GetBirimlerSelectList();
                    return View(model);
                }

                kullanici.KullaniciAdi = model.KullaniciAdi;
                kullanici.Rol = model.Rol;
                kullanici.KullaniciDurum = model.KullaniciDurum;
                kullanici.BirimId = model.BirimId;

                // Yeni şifre girilmişse hashle
                if (!string.IsNullOrWhiteSpace(model.Sifre))
                {
                    kullanici.Sifre = _passwordHasher.HashPassword(kullanici, model.Sifre);
                }

                // BeniHatirla normalde login sürecinde kullanılır; yine de alanı güncelliyoruz
                kullanici.BeniHatirla = model.BeniHatirla;

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı güncellenirken hata oluştu.");
                ModelState.AddModelError(string.Empty, "Güncelleme sırasında bir hata oluştu.");
                model.Birimler = await GetBirimlerSelectList();
                return View(model);
            }
        }

        // Soft delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sil(int id)
        {
            try
            {
                var kullanici = await _context.Kullanicilar.FindAsync(id);
                if (kullanici == null) return NotFound();

                kullanici.KullaniciDurum = false;
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı silinirken hata oluştu.");
                TempData["ErrMessage"] = "Kullanıcı silinirken bir hata oluştu.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /KullaniciMenuAtama/Guncelle/1
        [HttpGet]
        public async Task<IActionResult> YetkiGuncelle(int id)
        {
            var user = await _context.Kullanicilar
                .Include(u => u.KullaniciMenuler)
                .FirstOrDefaultAsync(u => u.KullaniciId == id);

            if (user == null) return NotFound();

            var allMenus = await _context.MenuOgeler
                .AsNoTracking()
                .OrderBy(m => m.Sirala)
                .ToListAsync();

            var assignedMenuIds = user.KullaniciMenuler.Select(km => km.MenuOgeId).ToList();

            var menuViewModels = BuildMenuViewModels(null, allMenus, assignedMenuIds);

            var viewModel = new KullaniciMenuAtamaVm
            {
                KullaniciId = user.KullaniciId,
                KullaniciAdi = user.KullaniciAdi,
                Menuler = menuViewModels
            };

            return View(viewModel);
        }

        // POST: /KullaniciMenuAtama/Guncelle
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> YetkiGuncelle(KullaniciMenuAtamaVm model)
        {
            var user = await _context.Kullanicilar
                .Include(u => u.KullaniciMenuler)
                .FirstOrDefaultAsync(u => u.KullaniciId == model.KullaniciId);

            if (user == null) return NotFound();

            // Null gelirse boş set say
            var desired = (model.SelectedMenuIds ?? new List<int>()).ToHashSet();
            var current = user.KullaniciMenuler.Select(km => km.MenuOgeId).ToHashSet();

            var toRemove = current.Except(desired).ToList();
            var toAdd = desired.Except(current).ToList();

            var strategy = _context.Database.CreateExecutionStrategy();
            try
            {
                await strategy.ExecuteAsync(async () =>
                {
                    using var tx = await _context.Database.BeginTransactionAsync(HttpContext.RequestAborted);

                    if (toRemove.Count > 0)
                    {
                        var removeEntities = user.KullaniciMenuler
                            .Where(km => toRemove.Contains(km.MenuOgeId))
                            .ToList();
                        foreach (var rem in removeEntities)
                            user.KullaniciMenuler.Remove(rem);
                    }

                    foreach (var mid in toAdd)
                    {
                        user.KullaniciMenuler.Add(new KullaniciMenuModel
                        {
                            KullaniciId = user.KullaniciId,
                            MenuOgeId = mid
                        });
                    }

                    await _context.SaveChangesAsync();
                    await tx.CommitAsync(HttpContext.RequestAborted);
                });

                TempData["OkMessage"] = "Yetkiler güncellendi.";
                return RedirectToAction(nameof(YetkiGuncelle), new { id = user.KullaniciId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Yetki güncellenirken hata oluştu.");
                TempData["ErrMessage"] = "Yetkiler güncellenirken bir hata oluştu.";
                return RedirectToAction(nameof(YetkiGuncelle), new { id = user.KullaniciId });
            }
        }

        // --- Helpers ---

        private List<MenuOgeAtamaVm> BuildMenuViewModels(int? parentId, List<MenuOgeModel> allMenus, List<int> assignedMenuIds)
        {
            var menus = allMenus
                .Where(m => m.AnaMenuId == parentId)
                .OrderBy(m => m.Sirala)
                .ToList();

            var result = new List<MenuOgeAtamaVm>();

            foreach (var menu in menus)
            {
                var vm = new MenuOgeAtamaVm
                {
                    MenuOgeId = menu.Id,
                    Baslik = menu.Baslik,
                    AtandiMi = assignedMenuIds.Contains(menu.Id),
                    AnaMenuId = menu.AnaMenuId,
                    AltOgeler = BuildMenuViewModels(menu.Id, allMenus, assignedMenuIds)
                };

                result.Add(vm);
            }

            return result;
        }



        private async Task<List<SelectListItem>> GetBirimlerSelectList()
        {
            return await _context.Birimler
                .AsNoTracking()
                .Where(b => b.BirimDurum == true)
                .OrderBy(b => b.BirimAd)
                .Select(b => new SelectListItem
                {
                    Value = b.BirimId.ToString(),
                    Text = b.BirimAd
                })
                .ToListAsync();
        }
    }
}
