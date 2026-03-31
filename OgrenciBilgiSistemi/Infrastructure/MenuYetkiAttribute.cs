using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using OgrenciBilgiSistemi.Data;
using OgrenciBilgiSistemi.Models;
using System.Security.Claims;

namespace OgrenciBilgiSistemi.Infrastructure
{
    /// <summary>
    /// Controller action'larında menü bazlı yetkilendirme uygular.
    /// Kullanıcının ilgili menü ögesine erişim izni olup olmadığını kontrol eder.
    /// Admin rolündeki kullanıcılar her zaman geçer.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class MenuYetkiAttribute : TypeFilterAttribute
    {
        public MenuYetkiAttribute() : base(typeof(MenuYetkiFilter))
        {
        }
    }

    public class MenuYetkiFilter : IAsyncAuthorizationFilter
    {
        private readonly AppDbContext _db;

        public MenuYetkiFilter(AppDbContext db)
        {
            _db = db;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            // Kimlik doğrulanmamışsa global filter zaten yakalar
            if (user.Identity?.IsAuthenticated != true)
                return;

            // Admin her yere erişebilir
            if (user.IsInRole("Admin"))
                return;

            // Kullanıcı ID'sini al
            var kullaniciIdStr = user.FindFirstValue("KullaniciId")
                              ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(kullaniciIdStr, out var kullaniciId))
            {
                context.Result = new ForbidResult();
                return;
            }

            // Mevcut controller ve action adlarını al
            var controllerName = context.RouteData.Values["controller"]?.ToString();
            var actionName = context.RouteData.Values["action"]?.ToString();

            if (string.IsNullOrEmpty(controllerName))
                return;

            // Menü kontrolünden muaf controller'lar (giriş/çıkış, ana sayfa vb.)
            var muafControllerlar = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Hesaplar", "Home"
            };

            if (muafControllerlar.Contains(controllerName))
                return;

            // Bu controller için tanımlı bir menü ögesi var mı kontrol et
            var menuVarMi = await _db.MenuOgeler
                .AsNoTracking()
                .AnyAsync(m => m.Controller == controllerName);

            // Menü tanımında olmayan controller'lar varsayılan olarak engellenir (default-deny)
            if (!menuVarMi)
            {
                context.Result = new RedirectToActionResult("YetkisizGiris", "Hesaplar", null);
                return;
            }

            // Kullanıcının bu controller'a erişim izni var mı
            var yetkiVarMi = await _db.KullaniciMenuOgeler
                .AsNoTracking()
                .AnyAsync(km =>
                    km.KullaniciId == kullaniciId &&
                    km.MenuOge.Controller == controllerName);

            if (!yetkiVarMi)
            {
                context.Result = new RedirectToActionResult("YetkisizGiris", "Hesaplar", null);
            }
        }
    }
}
