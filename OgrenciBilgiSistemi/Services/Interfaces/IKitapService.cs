using Microsoft.AspNetCore.Http;
using OgrenciBilgiSistemi.Models;

namespace OgrenciBilgiSistemi.Services.Interfaces
{
    public interface IKitapService
    {
        Task<SayfalanmisListeModel<KitapModel>> SearchPagedAsync(
            string? sortOrder,
            string? searchString,
            int pageIndex,
            int pageSize,
            CancellationToken ct = default);

        Task<int> AddAsync(KitapModel model, IFormFile? gorsel, CancellationToken ct = default);
        Task UpdateAsync(KitapModel model, IFormFile? gorsel, CancellationToken ct = default);
        Task SoftDeleteAsync(int id, CancellationToken ct = default);

        Task<KitapModel?> GetByIdAsync(int id, bool tumKitaplar = false, CancellationToken ct = default);

        Task<List<KitapModel>> GetFilteredListAsync(
            string? sortOrder,
            string? searchString,
            bool onlyActive = true,
            CancellationToken ct = default);
    }
}
