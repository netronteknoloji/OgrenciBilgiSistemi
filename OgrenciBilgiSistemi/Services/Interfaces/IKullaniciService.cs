using Microsoft.AspNetCore.Mvc.Rendering;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Shared.Enums;
using OgrenciBilgiSistemi.ViewModels;

namespace OgrenciBilgiSistemi.Services.Interfaces
{
    public interface IKullaniciService
    {
        Task<SayfalanmisListeModel<KullaniciModel>> SearchPagedAsync(
            string? searchString, int page, int pageSize = 10, CancellationToken ct = default);

        Task<KullaniciModel?> GetByIdAsync(int id, CancellationToken ct = default);

        Task EkleAsync(KullaniciModel model, CancellationToken ct = default);
        Task GuncelleAsync(KullaniciModel model, CancellationToken ct = default);
        Task SilAsync(int id, CancellationToken ct = default);

        Task<bool> KullaniciAdiVarMiAsync(string kullaniciAdi, int? excludeId = null, CancellationToken ct = default);

        Task<List<SelectListItem>> GetPersonellerSelectListAsync(CancellationToken ct = default);
        Task<List<SelectListItem>> GetServislerSelectListAsync(CancellationToken ct = default);
        Task<List<SelectListItem>> GetServislerByIdSelectListAsync(int? selectedId = null, CancellationToken ct = default);
        Task<List<SelectListItem>> GetBirimlerSelectListAsync(CancellationToken ct = default);
        Task<List<SelectListItem>> GetKullanicilarByRolSelectListAsync(KullaniciRolu rol, CancellationToken ct = default);

        // Yetki yönetimi
        Task<KullaniciMenuAtamaVm?> GetYetkiVmAsync(int kullaniciId, CancellationToken ct = default);
        Task YetkiGuncelleAsync(int kullaniciId, List<int>? selectedMenuIds, CancellationToken ct = default);
    }
}
