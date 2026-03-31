using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.ViewModels;

namespace OgrenciBilgiSistemi.Services.Interfaces
{
    public interface IOgretmenProfilService
    {
        Task<SayfalanmisListeModel<OgretmenProfilModel>> SearchPagedAsync(
            string? searchString, int page, int pageSize = 20, CancellationToken ct = default);

        Task<int> EkleKullaniciVeProfilAsync(OgretmenEkleVm vm, CancellationToken ct = default);
        Task GuncelleAsync(OgretmenProfilModel model, CancellationToken ct = default);
        Task SilAsync(int kullaniciId, CancellationToken ct = default);
        Task<OgretmenProfilModel?> GetByIdAsync(int kullaniciId, CancellationToken ct = default);
    }
}
