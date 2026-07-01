namespace OgrenciBilgiSistemi.Api.Services.Interfaces
{
    public interface IBildirimService
    {
        Task Olustur(int aliciKullaniciId, int tur, string mesaj, int? randevuId, CancellationToken ct = default);
        Task<List<BildirimModel>> KullanicininBildirimleriniGetir(int kullaniciId, int sayfaNo = 1, int sayfaBoyutu = 20);
        Task<int> OkunmamisSayisi(int kullaniciId);
        Task<bool> OkunduIsaretle(int bildirimId, int kullaniciId);
        Task<bool> TumunuOkunduIsaretle(int kullaniciId);
    }
}
