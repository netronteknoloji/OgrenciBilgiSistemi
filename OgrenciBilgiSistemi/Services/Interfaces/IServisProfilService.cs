using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.ViewModels;

namespace OgrenciBilgiSistemi.Services.Interfaces
{
    public interface IServisProfilService
    {
        Task<SayfalanmisListeModel<ServisProfilModel>> SearchPagedAsync(
            string? searchString, int page, int pageSize = 20, CancellationToken ct = default);

        Task<int> EkleKullaniciVeProfilAsync(ServisEkleVm vm, CancellationToken ct = default);
        Task GuncelleAsync(ServisProfilModel model, CancellationToken ct = default);
        Task SilAsync(int kullaniciId, CancellationToken ct = default);
        Task<ServisProfilModel?> GetByIdAsync(int kullaniciId, CancellationToken ct = default);
        Task<List<OgrenciModel>> GetOgrencilerAsync(int kullaniciId, CancellationToken ct = default);
    }
}
