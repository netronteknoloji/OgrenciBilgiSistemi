using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OgrenciBilgiSistemi.Data;
using OgrenciBilgiSistemi.Services.Interfaces;
using OgrenciBilgiSistemi.DTOs;

public class MenuViewComponent : ViewComponent
{
    private readonly IMenuService _menuService;
    public MenuViewComponent(IMenuService menuService) => _menuService = menuService;

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var user = ViewContext?.HttpContext?.User;
        if (user is null || user.Identity?.IsAuthenticated != true)
            return View("Default", Array.Empty<MenuOgeDto>());

        // Okul seçilmeden (ör. Genel Admin okul seçim ekranı) menü DB'den okunamaz
        var tenant = HttpContext.RequestServices.GetRequiredService<OgrenciBilgiSistemi.Shared.Services.TenantBaglami>();
        if (string.IsNullOrEmpty(tenant.ConnectionString))
            return View("Default", Array.Empty<MenuOgeDto>());

        // GenelAdmin: DB'deki menü atamalarına göre göster
        if (user.IsInRole("GenelAdmin"))
        {
            var db = HttpContext.RequestServices.GetRequiredService<AppDbContext>();
            var genelAdminKullanici = await db.Kullanicilar
                .AsNoTracking()
                .FirstOrDefaultAsync(k => k.Rol == KullaniciRolu.GenelAdmin);

            if (genelAdminKullanici == null)
                return View("Default", new List<MenuOgeDto>());

            var genelMenuler = await _menuService.GetSidebarForUserAsync(
                genelAdminKullanici.KullaniciId, user);
            return View("Default", genelMenuler ?? new List<MenuOgeDto>());
        }

        // Kullanıcı ID'yi bul
        var idStr = user.FindFirst("KullaniciId")?.Value
                 ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                 ?? user.FindFirst("sub")?.Value;

        if (!int.TryParse(idStr, out var userId))
            return View("Default", Array.Empty<MenuOgeDto>());

        // Menüleri getir
        var menus = await _menuService.GetSidebarForUserAsync(userId, user);
        return View("Default", menus ?? new List<MenuOgeDto>());
    }
}
