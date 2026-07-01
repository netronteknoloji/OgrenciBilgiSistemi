using OgrenciBilgiSistemi.Dtos;
using OgrenciBilgiSistemi.Models;

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
    OgrenciFiltre filtre = OgrenciFiltre.Aktif,
    int pageSize = 50,
    CancellationToken ct = default);

    Task<OgrenciModel?> GetByIdAsync(int id, bool includeVeli = true, CancellationToken ct = default);

    // Liste ekranındaki filtrelerle (arama + birim + sıralama) Excel çıktısı üretir
    Task<(byte[] Content, string FileName, string ContentType)> ExportOgrenciListesiExcelAsync(
        string? sortOrder,
        string? searchString,
        int? birimId,
        CancellationToken ct = default);

    // Öğrenci-Veli raporu (sayfalanmış)
    Task<SayfalanmisListeModel<OgrenciVeliRaporDto>> GetVeliRaporAsync(
        string? query,
        int? birimId,
        int page,
        int pageSize,
        CancellationToken ct = default);

    // Öğrenci-Veli raporu Excel çıktısı
    Task<(byte[] Content, string FileName, string ContentType)> ExportVeliRaporExcelAsync(
        string? query,
        int? birimId,
        CancellationToken ct = default);
}
