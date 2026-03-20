using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Models.Enums;

namespace OgrenciBilgiSistemi.Services.Interfaces
{
    public interface IPersonelService
    {
        Task<int> AddAsync(PersonelModel model, IFormFile? gorsel, CancellationToken ct = default);
        Task UpdateAsync(PersonelModel model, IFormFile? gorsel, CancellationToken ct = default);
        Task DeleteAsync(int personelId, CancellationToken ct = default);
        Task<bool> CihazaGonderAsync(int cihazId, bool sadeceAktifler = true, CancellationToken ct = default);

        Task<List<SelectListItem>> GetSelectListAsync(
            PersonelTipi? tipi = null,
            PersonelFiltre filtre = PersonelFiltre.Aktif,
            CancellationToken ct = default);

        Task<PersonelModel?> GetByIdAsync(int id, bool tumPersoneller = false, CancellationToken ct = default);

        Task<SayfalanmisListeModel<PersonelModel>> SearchPagedAsync(
            string? searchString,
            int page,
            int pageSize,
            PersonelFiltre filtre = PersonelFiltre.Aktif,
            CancellationToken ct = default);
    }
}