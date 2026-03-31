using Microsoft.AspNetCore.Mvc.Rendering;
using OgrenciBilgiSistemi.Models;

namespace OgrenciBilgiSistemi.Services.Interfaces
{
    public interface IKitapDetayService
    {
        Task<SayfalanmisListeModel<KitapDetayModel>> SearchPagedAsync(
            string? sortOrder,
            string? searchString,
            string? durumFilter,
            int pageIndex,
            int pageSize,
            CancellationToken ct = default);

        Task<KitapDetayModel?> GetByIdAsync(int id, CancellationToken ct = default);

        Task<int> AddAsync(KitapDetayModel model, CancellationToken ct = default);
        Task UpdateAsync(KitapDetayModel model, CancellationToken ct = default);

        Task TeslimAlAsync(int id, CancellationToken ct = default);

        Task<IReadOnlyList<KitapDetayModel>> GetFilteredListAsync(
            string? sortOrder,
            string? searchString,
            string? durumFilter,
            CancellationToken ct = default);

        Task<IReadOnlyList<SelectListItem>> GetKitapSelectListAsync(CancellationToken ct = default);
        Task<IReadOnlyList<SelectListItem>> GetOgrenciSelectListAsync(CancellationToken ct = default);
    }
}