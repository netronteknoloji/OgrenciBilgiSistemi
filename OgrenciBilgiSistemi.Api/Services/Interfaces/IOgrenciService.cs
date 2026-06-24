using OgrenciBilgiSistemi.Api.Dtos;
using OgrenciBilgiSistemi.Api.Models;
using OgrenciBilgiSistemi.Shared.Dtos;

namespace OgrenciBilgiSistemi.Api.Services.Interfaces
{
    public interface IOgrenciService
    {
        Task<List<OgrenciModel>> SinifaGoreOgrencileriGetirAsync(int sinifId);
        Task<List<OgrenciModel>> TumAktifOgrencileriGetirAsync();
        Task<List<OgrenciModel>> VeliyeGoreOgrencileriGetirAsync(int veliId);
        Task<OgrenciModel?> OgrenciGetirAsync(int ogrenciId);
        Task<List<SinifYoklamaOzetModel>> SinifYoklamaOzetiGetirAsync(int sinifId, DateTime tarih);
        Task<Dictionary<int, int>> MevcutYoklamaGetirAsync(int sinifId, int dersNumarasi);
        Task TopluYoklamaKaydetAsync(IEnumerable<(int OgrenciId, int DurumId)> yoklamaVerisi, int sinifId, int ogretmenId, int dersNumarasi);
        Task<List<SinifYoklamaDto>> HaftalikYoklamaGetirAsync(int ogrenciId, DateTime baslangic, DateTime bitis);
        Task<OgrenciDetayDto?> OgrenciDetayGetirAsync(int ogrenciId);
        Task<int> EkleAsync(OgrenciKaydetDto dto);
        Task<bool> GuncelleAsync(int ogrenciId, OgrenciKaydetDto dto);
        Task<bool> SilAsync(int ogrenciId);
    }
}
