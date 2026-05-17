namespace OgrenciBilgiSistemi.Push;

public interface IPushBildirimGonderici
{
    Task<PushGonderimSonucu> GonderAsync(int aliciKullaniciId, PushBildirimYuku yuk, CancellationToken ct = default);

    Task<PushGonderimSonucu> GonderAsync(IReadOnlyList<BildirimTokenKaydi> tokenlar, PushBildirimYuku yuk, CancellationToken ct = default);
}
