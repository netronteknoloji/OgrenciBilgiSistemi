using OgrenciBilgiSistemi.Models;
using Microsoft.AspNetCore.Http;

public interface IOgrenciService
{
    Task<int> EkleAsync(OgrenciModel model, IFormFile? gorsel, bool buAyYemekhaneAktif, CancellationToken ct = default);
    Task GuncelleAsync(OgrenciModel model, IFormFile? gorsel, bool? buAyYemekhaneAktif, CancellationToken ct = default);
    Task SilAsync(int ogrenciId, CancellationToken ct = default);
    Task<bool> CihazaGonderAsync(int cihazId, CancellationToken ct = default);

    Task<SayfalanmisListeModel<OgrenciModel>> SearchPagedAsync(
    string? sortOrder,
    string? searchString,
    int pageNumber,
    int? birimId,
    bool includePasif,
    int pageSize = 50,
    CancellationToken ct = default);

    Task<OgrenciModel?> GetByIdAsync(int id, bool includeVeli = true, CancellationToken ct = default);

}
