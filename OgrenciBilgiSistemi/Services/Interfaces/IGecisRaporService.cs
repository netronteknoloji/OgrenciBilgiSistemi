using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.ViewModels;

namespace OgrenciBilgiSistemi.Services.Interfaces
{
    public interface IGecisRaporService
    {
        // Genel listeler (Detay action)
        Task<SayfalanmisListeModel<SinifYoklamaModel>> SinifYoklamaListeleAsync(
            string? search, DateTime? startDate, DateTime? endDate, int page, CancellationToken ct = default);
        Task<SayfalanmisListeModel<ServisYoklamaModel>> ServisYoklamaListeleAsync(
            string? search, DateTime? startDate, DateTime? endDate, int page, CancellationToken ct = default);
        Task<SayfalanmisListeModel<OgrenciDetayModel>> GecisListeleAsync(
            string? sortOrder, string? search, DateTime? startDate, DateTime? endDate,
            IstasyonTipi? istasyon, int page, CancellationToken ct = default);

        // Genel Excel (DetayExcel action)
        Task<List<SinifYoklamaModel>> TumSinifYoklamaListeleAsync(
            string? search, DateTime? startDate, DateTime? endDate, CancellationToken ct = default);
        Task<List<ServisYoklamaModel>> TumServisYoklamaListeleAsync(
            string? search, DateTime? startDate, DateTime? endDate, CancellationToken ct = default);
        Task<List<OgrenciDetayModel>> TumGecislerGetirAsync(
            string? search, DateTime? startDate, DateTime? endDate, IstasyonTipi? istasyon, CancellationToken ct = default);

        // Tek öğrenci (GirisCikisDetay + DetayExportToExcel)
        Task<OgrenciModel?> OgrenciBulAsync(int ogrenciId, CancellationToken ct = default);
        Task<SayfalanmisListeModel<OgrenciGirisCikisVm>> OgrenciGecisListeleAsync(
            int ogrenciId, DateTime? startDate, DateTime? endDate, IstasyonTipi? istasyon, int page, CancellationToken ct = default);
        Task<List<SinifYoklamaModel>> OgrenciSinifYoklamaListeleAsync(
            int ogrenciId, DateTime? startDate, DateTime? endDate, CancellationToken ct = default);
        Task<List<ServisYoklamaModel>> OgrenciServisYoklamaListeleAsync(
            int ogrenciId, DateTime? startDate, DateTime? endDate, CancellationToken ct = default);
        Task<List<OgrenciDetayModel>> OgrenciGecislerGetirAsync(
            int ogrenciId, DateTime? startDate, DateTime? endDate, IstasyonTipi? istasyon, CancellationToken ct = default);
    }
}
