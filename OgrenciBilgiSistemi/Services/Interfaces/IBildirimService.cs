namespace OgrenciBilgiSistemi.Services.Interfaces
{
    public interface IBildirimService
    {
        Task Olustur(int aliciKullaniciId, BildirimTuru tur, string mesaj, int? randevuId = null, CancellationToken ct = default);
    }
}
