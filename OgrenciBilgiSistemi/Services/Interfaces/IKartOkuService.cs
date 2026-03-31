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
}