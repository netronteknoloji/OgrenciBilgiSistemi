using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Services.Interfaces;

namespace OgrenciBilgiSistemi.Controllers;

public class HomeController : Controller
{
    private readonly IDashboardService _dashboardService;
    public HomeController(IDashboardService dashboardService) { _dashboardService = dashboardService; }

    public IActionResult Index() => View();

    // Okul geneli istatistikler yalnızca Yönetici ekranında gösterilir
    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DashboardStats(CancellationToken ct)
    {
        var dto = await _dashboardService.GetStatsAsync(ct);
        return Json(dto);
    }

    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DashboardSeries(int? yil, int? ay, CancellationToken ct)
    {
        var now = DateTime.Now;
        int y = yil ?? now.Year;
        int m = ay ?? now.Month;
        var dto = await _dashboardService.GetSeriesAsync(y, m, ct);
        return Json(dto);
    }

    // UseExceptionHandler("/Home/Error") hedefi; oturum ve menü gerektirmez
    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new HataGorunumModel { IstemKimlik = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
