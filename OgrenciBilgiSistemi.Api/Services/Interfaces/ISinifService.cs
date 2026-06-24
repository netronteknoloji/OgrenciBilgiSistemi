using OgrenciBilgiSistemi.Api.Models;

namespace OgrenciBilgiSistemi.Api.Services.Interfaces
{
    public interface ISinifService
    {
        Task<List<BirimOgrenciSayisiModel>> TumSiniflariOgrenciSayisiIleGetirAsync();
    }
}
