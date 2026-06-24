using OgrenciBilgiSistemi.Api.Models;

namespace OgrenciBilgiSistemi.Api.Services.Interfaces
{
    public interface IVeliListeService
    {
        Task<List<VeliListeModel>> AktifVelileriGetirAsync();
        Task<VeliDetayModel?> VeliDetayGetirAsync(int kullaniciId);
    }
}
