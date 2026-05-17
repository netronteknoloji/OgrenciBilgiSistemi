namespace OgrenciBilgiSistemi.Push;

public sealed record BildirimCihazKaydi(
    int KullaniciId,
    string FcmToken,
    PushPlatformu Platform,
    string? UygulamaSurumu,
    string? CihazModeli);
