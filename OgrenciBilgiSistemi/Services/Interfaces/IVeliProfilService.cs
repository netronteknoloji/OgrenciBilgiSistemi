using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.ViewModels;

namespace OgrenciBilgiSistemi.Services.Interfaces
{
    public interface IVeliProfilService
    {
        Task<SayfalanmisListeModel<VeliProfilModel>> SearchPagedAsync(
            string? searchString, int page, int pageSize = 20, CancellationToken ct = default);
        Task<int> EkleKullaniciVeProfilAsync(VeliEkleVm vm, CancellationToken ct = default);
        Task GuncelleAsync(VeliProfilModel model, CancellationToken ct = default);
        Task SilAsync(int kullaniciId, CancellationToken ct = default);
        Task<VeliProfilModel?> GetByIdAsync(int kullaniciId, CancellationToken ct = default);
        Task<List<OgrenciModel>> GetOgrencilerAsync(int kullaniciId, CancellationToken ct = default);
    }
}
