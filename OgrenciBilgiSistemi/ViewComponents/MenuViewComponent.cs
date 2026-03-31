using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using OgrenciBilgiSistemi.Services.Interfaces;
using OgrenciBilgiSistemi.DTOs;

public class MenuViewComponent : ViewComponent
{
    private readonly IMenuService _menuService;
    public MenuViewComponent(IMenuService menuService) => _menuService = menuService;

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var user = ViewContext?.HttpContext?.User;
        // 🔧 Mantık ifadesi sadeleştirildi
        if (user is null || user.Identity?.IsAuthenticated != true)
            return View("Default", Array.Empty<MenuOgeDto>());

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