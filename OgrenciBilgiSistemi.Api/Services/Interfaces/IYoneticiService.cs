using OgrenciBilgiSistemi.Api.Models;

namespace OgrenciBilgiSistemi.Api.Services.Interfaces
{
    public interface IYoneticiService
    {
        Task<OkulOzetModel> OkulOzetGetirAsync();
        Task<List<ServisListeOgesiModel>> TumServisleriGetirAsync();
        Task<List<OgrenciModel>> ServisOgrencileriGetirAsync(int servisKullaniciId);
        Task<List<YemekhaneBugunOgesiModel>> BugunYemekhaneGirislerinAsync();
        Task<List<AnakapiCikisBugunOgesiModel>> BugunAnakapiCikislariAsync();
    }
}
