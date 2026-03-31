using Microsoft.AspNetCore.Mvc.Rendering;
using OgrenciBilgiSistemi.Dtos;
using OgrenciBilgiSistemi.Models;

namespace OgrenciBilgiSistemi.Services.Interfaces
{
    public interface IBirimService
    {
        Task AddAsync(BirimModel model, CancellationToken ct = default);
        Task UpdateAsync(BirimModel model, CancellationToken ct = default);
        Task DeleteAsync(int id, CancellationToken ct = default);

        Task<IReadOnlyList<BirimDto>> GetAllAsync(bool? sinifMi = null, CancellationToken ct = default);
        Task<List<SelectListItem>> GetSelectListAsync(int? selectedId = null, bool? sinifMi = null,
                        BirimFiltre filtre = BirimFiltre.Aktif, CancellationToken ct = default);

        Task<SayfalanmisListeModel<BirimModel>> SearchPagedAsync(
                        string? searchString, int page, int pageSize,
                        BirimFiltre filtre = BirimFiltre.Aktif, bool? sinifMi = null, CancellationToken ct = default);

        Task<BirimModel?> GetByIdAsync(int id, bool tumBirimler = false, CancellationToken ct = default);
        Task<bool> ExistsWithNameAsync(string ad, int? excludeId = null, CancellationToken ct = default);
    }
}