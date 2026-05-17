namespace OgrenciBilgiSistemi.Push;

public interface IBildirimTokenDeposu
{
    Task UpsertAsync(BildirimCihazKaydi kayit, CancellationToken ct = default);

    Task IptalAsync(IEnumerable<string> tokenlar, CancellationToken ct = default);

    Task<IReadOnlyList<BildirimTokenKaydi>> AktifTokenleriGetirAsync(int kullaniciId, CancellationToken ct = default);
}
