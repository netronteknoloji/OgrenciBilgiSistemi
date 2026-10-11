namespace OgrenciBilgiSistemi.Api.Services.Interfaces
{
    /// <summary>Yoklama SMS'lerini GonderimKuyrugu'na alır (gönderim MVC'deki kuyruk işleyicisinde).</summary>
    public interface IYoklamaSmsBildirimService
    {
        Task<int> ServisYoklamaBildir(IReadOnlyList<(int OgrenciId, int DurumId)> yoklamaVerisi, int periyot, CancellationToken ct = default);
        Task<int> SinifYoklamaBildir(IReadOnlyList<(int OgrenciId, int DurumId)> yoklamaVerisi, int dersNumarasi, CancellationToken ct = default);

        /// <summary>Bugünkü, kuyruğa alınmamış tüm yoklama SMS'lerini kuyruğa alır (güvenlik ağı).</summary>
        Task<int> BekleyenleriKuyrugaAl(CancellationToken ct = default);
    }
}
