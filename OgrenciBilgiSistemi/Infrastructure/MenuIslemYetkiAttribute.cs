using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using OgrenciBilgiSistemi.Constants;
using OgrenciBilgiSistemi.Data;
using System.Security.Claims;

namespace OgrenciBilgiSistemi.Infrastructure
{
    /// <summary>
    /// Belirli bir menü ögesine (Controller + Action) yetki ister.
    /// Global <see cref="MenuYetkiFilter"/> yalnızca controller'a baktığı için, aynı controller'da
    /// farklı menülere bağlı action'ları (ör. Aidat İşlemleri vs Aidat Raporu) ayırmak için kullanılır.
    /// Admin her zaman geçer.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class MenuIslemYetkiAttribute : TypeFilterAttribute
    {
        public MenuIslemYetkiAttribute(string controller, string action) : base(typeof(MenuIslemYetkiFilter))
        {
            Arguments = new object[] { controller, action };
        }
    }

    public class MenuIslemYetkiFilter : IAsyncAuthorizationFilter
    {
        private readonly AppDbContext _db;
        private readonly string _controller;
        private readonly string _action;

        public MenuIslemYetkiFilter(AppDbContext db, string controller, string action)
        {
            _db = db;
            _controller = controller;
            _action = action;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            if (user.Identity?.IsAuthenticated != true)
                return;

            if (user.IsInRole(RolAdlari.Admin))
                return;

            int? kullaniciId = null;

            if (user.IsInRole(RolAdlari.GenelAdmin))
            {
                kullaniciId = await _db.Kullanicilar
                    .AsNoTracking()
                    .Where(k => k.Rol == KullaniciRolu.GenelAdmin)
                    .Select(k => (int?)k.KullaniciId)
                    .FirstOrDefaultAsync();
            }
            else
            {
                var kullaniciIdStr = user.FindFirstValue("KullaniciId")
                                  ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(kullaniciIdStr, out var id))
                    kullaniciId = id;
            }

            if (kullaniciId is null)
            {
                context.Result = new RedirectToActionResult("YetkisizGiris", "Hesaplar", null);
                return;
            }

            var yetkiVarMi = await _db.KullaniciMenuOgeler
                .AsNoTracking()
                .AnyAsync(km =>
                    km.KullaniciId == kullaniciId &&
                    km.MenuOge.Controller == _controller &&
                    km.MenuOge.Action == _action);

            if (!yetkiVarMi)
                context.Result = new RedirectToActionResult("YetkisizGiris", "Hesaplar", null);
        }
    }
}
