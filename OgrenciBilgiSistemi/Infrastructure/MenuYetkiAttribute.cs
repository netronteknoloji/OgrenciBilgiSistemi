using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using OgrenciBilgiSistemi.Constants;
using OgrenciBilgiSistemi.Data;
using OgrenciBilgiSistemi.Shared.Services;
using System.Security.Claims;

namespace OgrenciBilgiSistemi.Infrastructure
{
    /// <summary>
    /// Global filtre: kullanıcının istenen controller/action'a karşılık gelen menü ögesine yetkisi var mı kontrol eder.
    /// Admin rolündeki kullanıcılar her zaman geçer.
    /// </summary>
    public class MenuYetkiFilter : IAsyncAuthorizationFilter
    {
        private readonly AppDbContext _db;
        private readonly TenantBaglami _tenant;

        public MenuYetkiFilter(AppDbContext db, TenantBaglami tenant)
        {
            _db = db;
            _tenant = tenant;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            // Kimlik doğrulanmamışsa global filter zaten yakalar
            if (user.Identity?.IsAuthenticated != true)
                return;

            // Local Admin her yere erişebilir
            if (user.IsInRole(RolAdlari.Admin))
                return;

            // Okul bağlamı yoksa DB'ye gidilmez; TenantZorunluFilter okul seçimine/girişe yönlendirir
            if (string.IsNullOrEmpty(_tenant.ConnectionString))
                return;

            // Mevcut controller ve action adlarını al
            var controllerName = context.RouteData.Values["controller"]?.ToString();
            var actionName = context.RouteData.Values["action"]?.ToString();

            if (string.IsNullOrEmpty(controllerName))
                return;

            // Menü kontrolünden muaf controller'lar (giriş/çıkış, ana sayfa vb.)
            var muafControllerlar = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Hesaplar", "Home", "OkulSecim"
            };

            if (muafControllerlar.Contains(controllerName))
                return;

            // Rapor menüsü yalnızca raporu açar; işlem action'ları işlem menüsünü ister
            var gerekenMenuler = MenuIslemHaritasi.GerekenMenuler(controllerName, actionName)?.ToList();

            // GenelAdmin: DB'deki menü atamalarına göre kontrol
            if (user.IsInRole(RolAdlari.GenelAdmin))
            {
                var genelAdminKullanici = await _db.Kullanicilar
                    .AsNoTracking()
                    .FirstOrDefaultAsync(k => k.Rol == KullaniciRolu.GenelAdmin);

                if (genelAdminKullanici == null)
                {
                    context.Result = new RedirectToActionResult("YetkisizGiris", "Hesaplar", null);
                    return;
                }

                var genelYetkiVarMi = await _db.KullaniciMenuOgeler
                    .AsNoTracking()
                    .AnyAsync(km =>
                        km.KullaniciId == genelAdminKullanici.KullaniciId &&
                        km.MenuOge.Controller == controllerName &&
                        (gerekenMenuler == null || gerekenMenuler.Contains(km.MenuOge.Action!)));

                if (!genelYetkiVarMi)
                    context.Result = new RedirectToActionResult("YetkisizGiris", "Hesaplar", null);

                return;
            }

            // Kullanıcı ID'sini al
            var kullaniciIdStr = user.FindFirstValue("KullaniciId")
                              ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(kullaniciIdStr, out var kullaniciId))
            {
                context.Result = new ForbidResult();
                return;
            }

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

            // Kullanıcının bu controller'a (çok menülü controller'da bu action'ın menüsüne) izni var mı
            var yetkiVarMi = await _db.KullaniciMenuOgeler
                .AsNoTracking()
                .AnyAsync(km =>
                    km.KullaniciId == kullaniciId &&
                    km.MenuOge.Controller == controllerName &&
                    (gerekenMenuler == null || gerekenMenuler.Contains(km.MenuOge.Action!)));

            if (!yetkiVarMi)
            {
                context.Result = new RedirectToActionResult("YetkisizGiris", "Hesaplar", null);
            }
        }
    }
}
