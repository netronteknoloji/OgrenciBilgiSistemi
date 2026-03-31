namespace OgrenciBilgiSistemi.Services.Interfaces;

public interface ISmsGonderimService
{
    Task GecisSmsBildir(int ogrenciId, string ogrenciAdSoyad, string gecisTipi, DateTime zaman, CancellationToken ct = default);
}
