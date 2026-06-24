using OgrenciBilgiSistemi.Dtos;
using OgrenciBilgiSistemi.Models;
using System.Threading;

public interface IKartOkuService
{
    Task<OgrenciModel?> GetOgrenciByKartNoAsync(string kartNo, CancellationToken ct = default);

    Task<OgrenciBilgisiDto> OgrenciDtoHazirla(
        OgrenciModel ogrenci,
        OgrenciDetayModel log,
        CancellationToken ct = default);

    Task<bool> YemekIzniVarMiAsync(int ogrenciId, int yil, int ay, CancellationToken ct = default);
    Task<bool> BugunYemekGirisiVarMiAsync(int ogrenciId, DateTime today, DateTime tomorrow, CancellationToken ct = default);
    Task<(bool CikisVarMi, bool GirisVarMi)> BugunAnaKapiHareketleriAsync(int ogrenciId, DateTime today, DateTime tomorrow, CancellationToken ct = default);
}