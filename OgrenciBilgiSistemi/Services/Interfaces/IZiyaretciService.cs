using OgrenciBilgiSistemi.Dtos;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.ViewModels;

namespace OgrenciBilgiSistemi.Services.Interfaces
{
    public interface IZiyaretciService
    {
        Task<int> EkleAsync(ZiyaretciModel model, CancellationToken ct = default);
        Task GuncelleAsync(ZiyaretciModel model, CancellationToken ct = default);
        Task CikisYapAsync(int ziyaretciId, CancellationToken ct = default);
        Task SilAsync(int ziyaretciId, CancellationToken ct = default);

        Task<ZiyaretciModel?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<ZiyaretciModel?> GetAktifByKartAsync(string kartNo, CancellationToken ct = default);

        Task<List<ZiyaretciModel>> GetZiyaretGecmisiAsync(
            string? tcKimlikNo,
            string adSoyad,
            CancellationToken ct = default);

        Task<SayfalanmisListeModel<ZiyaretciModel>> SearchPagedAsync(
            string? searchString,
            int page,
            int pageSize,
            bool sadeceAktif = true,
            int? kullaniciId = null,
            CancellationToken ct = default);

        // Kart okuma ekranında kullanılacak model
        Task<ZiyaretciKartOkumaViewModel> KartOkutAsync(string kartNo, CancellationToken ct = default);

        // 🔹 Rapor (liste)
        Task<List<ZiyaretciRaporDto>> GetRaporAsync(
            string? query,
            DateTime? startDate,
            DateTime? endDate,
            CancellationToken ct = default);

        // 🔹 Rapor (Excel)
        Task<byte[]> GetRaporExcelAsync(
            string? query,
            DateTime? startDate,
            DateTime? endDate,
            CancellationToken ct = default);
    }
}