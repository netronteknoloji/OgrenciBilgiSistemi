using OgrenciBilgiSistemi.Models;

namespace OgrenciBilgiSistemi.Services.Interfaces
{
    public interface IServisRaporService
    {
        // Filtre dropdown'ı için tüm servisler (plaka sıralı)
        Task<List<ServisProfilModel>> ServisleriGetirAsync(CancellationToken ct = default);

        // Şoför listesi (öğrenci sayısı dolu)
        Task<List<ServisProfilModel>> SoforListesiAsync(string? search, CancellationToken ct = default);

        // Servise kayıtlı aktif öğrenciler (Birim ve ServisKullanici dahil)
        Task<List<OgrenciModel>> SoforOgrenciListesiAsync(int? servisId, string? search, CancellationToken ct = default);

        // Servis yoklaması
        Task<SayfalanmisListeModel<ServisYoklamaModel>> ServisYoklamaListeleAsync(
            int? servisId, string? search, DateTime? startDate, DateTime? endDate, int page, CancellationToken ct = default);
        Task<List<ServisYoklamaModel>> TumServisYoklamaListeleAsync(
            int? servisId, string? search, DateTime? startDate, DateTime? endDate, CancellationToken ct = default);
    }
}
