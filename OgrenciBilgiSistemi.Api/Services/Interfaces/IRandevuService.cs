using OgrenciBilgiSistemi.Api.Models;
using OgrenciBilgiSistemi.Shared.Enums;

namespace OgrenciBilgiSistemi.Api.Services.Interfaces
{
    public interface IRandevuService
    {
        Task<List<RandevuModel>> KullanicininRandevulariniGetir(int kullaniciId, string rol, int sayfaNo = 1, int sayfaBoyutu = 5);
        Task<RandevuModel?> RandevuGetir(int randevuId);
        Task<int> OgretmenRandevuOlustur(int ogretmenId, int veliId, int? ogrenciId, DateTime tarih, int sureDakika, string? not);
        Task<int> VeliRandevuOlustur(int veliId, int ogretmenId, int? ogrenciId, DateTime tarih, int sureDakika, string? not);
        Task<bool> DurumGuncelle(int randevuId, int kullaniciId, string rol, RandevuDurumu yeniDurum);
        Task<bool> IptalEt(int randevuId, int kullaniciId);
        Task<string?> CakismaMesajiAl(int ogretmenId, int veliId, DateTime tarih, int sureDakika);
    }
}
