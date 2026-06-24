namespace OgrenciBilgiSistemi.Api.Services.Interfaces
{
    public interface IYoklamaSmsBildirimService
    {
        Task ServisYoklamaBildir(IReadOnlyList<(int OgrenciId, int DurumId)> yoklamaVerisi, int periyot, CancellationToken ct = default);
        Task SinifYoklamaBildir(IReadOnlyList<(int OgrenciId, int DurumId)> yoklamaVerisi, int dersNumarasi, CancellationToken ct = default);
    }
}
