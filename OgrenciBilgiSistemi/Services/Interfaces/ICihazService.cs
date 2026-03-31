using OgrenciBilgiSistemi.Dtos;
using OgrenciBilgiSistemi.Models;

public interface ICihazService
{
    Task YenileCihazListesiAsync(CancellationToken ct = default);
    Task<List<CihazModel>> GetCihazlarAsync(CancellationToken ct = default);
    List<CihazModel> GetCihazlar();
    Task<bool> CihazaOgrenciEkleAsync(OgrenciModel ogrenci, CancellationToken ct = default);
    Task<bool> CihazaOgrenciGuncelleAsync(OgrenciModel ogrenci, CancellationToken ct = default);
    Task<bool> CihazaOgrenciSilAsync(int ogrenciId, CancellationToken ct = default);
    Task<bool> CihazaOgrencileriGonderAsync(int cihazId, List<OgrenciModel> ogrenciListesi, CancellationToken ct = default);
    Task<bool> CihazdakiTumKullanicilariSilAsync(int cihazId, CancellationToken ct = default);
    Task<List<ZkUserDto>> CihazdanKullanicilariListeleAsync(int cihazId, CancellationToken ct = default);
    Task<CihazModel?> CihazGetByIdAsync(int id, CancellationToken ct = default);
    Task<bool> CihazEkleAsync(CihazModel model, CancellationToken ct = default);
    Task<bool> CihazGuncelleAsync(CihazModel model, CancellationToken ct = default);
    Task<bool> CihazSilAsync(int id, CancellationToken ct = default);
}

