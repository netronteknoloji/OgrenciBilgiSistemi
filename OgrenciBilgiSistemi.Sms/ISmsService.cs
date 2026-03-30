namespace OgrenciBilgiSistemi.Sms;

public sealed record SmsGonderimSonucu(bool Basarili, string? Hata = null, string? HamCevap = null);

public interface ISmsService
{
    Task<SmsGonderimSonucu> Gonder(string telefon, string mesaj, CancellationToken ct = default);
}
