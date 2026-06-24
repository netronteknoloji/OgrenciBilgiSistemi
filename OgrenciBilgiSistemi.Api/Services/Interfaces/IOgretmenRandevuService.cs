using OgrenciBilgiSistemi.Api.Models;

namespace OgrenciBilgiSistemi.Api.Services.Interfaces
{
    public interface IOgretmenRandevuService
    {
        Task<List<OgretmenRandevuTakvimModel>> OgretmeninRandevuTakviminiGetir(int ogretmenId);
        Task<int> Ekle(int ogretmenId, DateTime tarih, TimeSpan baslangic, TimeSpan bitis);
        Task<bool> Sil(int ogretmenRandevuId, int ogretmenId);
        Task<List<RandevuSlotModel>> RandevuSlotlariGetir(int ogretmenId, DateTime baslangicTarih, DateTime bitisTarih);
    }
}
