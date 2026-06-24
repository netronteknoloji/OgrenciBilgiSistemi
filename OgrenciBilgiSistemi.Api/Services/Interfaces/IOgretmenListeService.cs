using OgrenciBilgiSistemi.Api.Models;

namespace OgrenciBilgiSistemi.Api.Services.Interfaces
{
    public interface IOgretmenListeService
    {
        Task<List<OgretmenListeModel>> AktifOgretmenleriGetir();
        Task<OgretmenDetayModel?> OgretmenDetayGetirAsync(int kullaniciId);
    }
}
