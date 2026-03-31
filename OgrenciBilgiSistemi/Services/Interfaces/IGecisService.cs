namespace OgrenciBilgiSistemi.Services.Interfaces
{
    public record GecisKayitSonucu(string GecisTipi, DateTime Zaman);
    public interface IGecisService
    {
        Task<GecisKayitSonucu> KaydetAsync(int cihazId, int ogrenciId, IstasyonTipi istasyon, DateTime now, CancellationToken ct = default, string? gecisTipi = null);
    }
}
