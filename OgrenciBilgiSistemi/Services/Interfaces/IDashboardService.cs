using OgrenciBilgiSistemi.Dtos;

namespace OgrenciBilgiSistemi.Services.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardStatsDto> GetStatsAsync(CancellationToken ct = default);
        Task<DashboardSeriesDto> GetSeriesAsync(int yil, int ay, CancellationToken ct = default);
    }
}
