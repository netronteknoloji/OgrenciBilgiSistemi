namespace OgrenciBilgiSistemi.Services.Interfaces
{
    /// <summary>
    /// Öğrenci ↔ ZKTeco cihaz senkron kuyruğu (outbox). Öğrenciyi değiştiren her yer yalnızca kuyruğa ekler;
    /// cihaza yazma/silme CihazSenkronIsleyici'dedir. Doğrudan CihazService ile öğrenci yazma.
    /// </summary>
    public interface ICihazSenkronService
    {
        /// <summary>
        /// Öğrenciyi tüm ZKTeco cihazlarıyla eşitlenmek üzere kuyruğa alır (çağıranın transaction'ına katılır).
        /// Aynı cihaz için bekleyen satır varsa yenisi eklenmez. Eklenen satır sayısını döner.
        /// </summary>
        Task<int> KuyrugaEkleAsync(int ogrenciId, CancellationToken ct = default);

        /// <summary>Cihazı tüm öğrencilerle eşitlemek üzere kuyruğa alır; bekleyenleri hemen denenecek hale getirir.</summary>
        Task<int> CihaziEsitleAsync(int cihazId, CancellationToken ct = default);

        /// <summary>Cihazın başarısız satırlarını yeniden denenmek üzere kuyruğa döndürür.</summary>
        Task<int> HatalariYenidenDeneAsync(int cihazId, CancellationToken ct = default);

        Task<Dictionary<int, CihazSenkronOzetDto>> OzetlerAsync(IReadOnlyCollection<int> cihazIdler, CancellationToken ct = default);

        /// <summary>İşleyiciyi uyandırır; transaction commit edildikten sonra çağrılmalıdır.</summary>
        void Tetikle();
    }

    public sealed record CihazSenkronOzetDto(int Bekleyen, int Hatali, DateTime? SonBasarili, string? SonHata);
}
