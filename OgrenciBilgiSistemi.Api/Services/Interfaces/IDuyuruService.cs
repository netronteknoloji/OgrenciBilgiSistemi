namespace OgrenciBilgiSistemi.Api.Services.Interfaces
{
    public interface IDuyuruService
    {
        Task<int> OgretmenDuyuruOlustur(int ogretmenId, string baslik, string icerik);
        Task<List<DuyuruModel>> VeliDuyurulariGetir(int veliId, int sayfaNo = 1, int sayfaBoyutu = 20);
        Task<bool> OkunduIsaretle(int duyuruId, int veliId);
        Task<int> TumunuOkunduIsaretle(int veliId);
        Task<int> OkunmamisSayisi(int veliId);
        Task<DuyuruModel?> DuyuruGetir(int duyuruId);
    }
}
