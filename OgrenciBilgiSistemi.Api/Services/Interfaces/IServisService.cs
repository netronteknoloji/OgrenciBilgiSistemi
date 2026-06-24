using OgrenciBilgiSistemi.Api.Dtos;
using OgrenciBilgiSistemi.Api.Models;

namespace OgrenciBilgiSistemi.Api.Services.Interfaces
{
    public interface IServisService
    {
        Task<List<OgrenciModel>> ServisOgrencileriGetir(int servisKullaniciId);
        Task<ServisProfilModel?> ServisProfilGetir(int servisKullaniciId);
        Task<Dictionary<int, int>> MevcutServisYoklamaGetir(int servisKullaniciId, int periyot);
        Task<List<ServisYoklamaGecmisDto>> OgrenciYoklamaGecmisiGetir(int ogrenciId, DateTime? baslangic, DateTime? bitis);
        Task ServisYoklamaKaydet(IEnumerable<(int OgrenciId, int DurumId)> yoklamaVerisi, int kullaniciId, int periyot);
    }
}
