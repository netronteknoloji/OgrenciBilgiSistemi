using Microsoft.AspNetCore.Mvc;
using OgrenciBilgiSistemi.Services.Interfaces;

namespace OgrenciBilgiSistemi.Controllers;

public class HomeController : Controller
{
    private readonly IDashboardService _dashboardService;
    public HomeController(IDashboardService dashboardService) { _dashboardService = dashboardService; }

    public IActionResult Index() => View();

    [HttpGet]
    public async Task<IActionResult> DashboardStats(CancellationToken ct)
    {
        var dto = await _dashboardService.GetStatsAsync(ct);
        return Json(dto);
    }

    [HttpGet]
    public async Task<IActionResult> DashboardSeries(int? yil, int? ay, CancellationToken ct)
    {
        var now = DateTime.Now;
        int y = yil ?? now.Year;
        int m = ay ?? now.Month;
        var dto = await _dashboardService.GetSeriesAsync(y, m, ct);
        return Json(dto);
    }
}
