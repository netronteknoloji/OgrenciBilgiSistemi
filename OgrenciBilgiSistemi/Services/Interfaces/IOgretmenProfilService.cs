using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.ViewModels;

namespace OgrenciBilgiSistemi.Services.Interfaces
{
    public interface IOgretmenProfilService
    {
        Task<SayfalanmisListeModel<OgretmenProfilModel>> SearchPagedAsync(
            string? searchString, int page, int pageSize = 20,
            OgretmenFiltre filtre = OgretmenFiltre.Aktif, CancellationToken ct = default);

        Task<int> EkleKullaniciVeProfilAsync(OgretmenEkleVm vm, CancellationToken ct = default);
        Task GuncelleAsync(OgretmenProfilModel model, string? kullaniciAdi, string? telefon, string? sifre, CancellationToken ct = default);
        Task SilAsync(int kullaniciId, CancellationToken ct = default);
        Task<OgretmenProfilModel?> GetByIdAsync(int kullaniciId, CancellationToken ct = default);
    }
}
