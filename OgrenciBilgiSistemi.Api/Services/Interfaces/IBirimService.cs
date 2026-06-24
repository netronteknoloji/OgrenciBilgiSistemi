using OgrenciBilgiSistemi.Api.Models;

namespace OgrenciBilgiSistemi.Api.Services.Interfaces
{
    public interface IBirimService
    {
        Task<BirimModel?> BirimGetirAsync(int birimId);
    }
}
