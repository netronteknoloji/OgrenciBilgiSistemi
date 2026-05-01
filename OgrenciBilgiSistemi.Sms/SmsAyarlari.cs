namespace OgrenciBilgiSistemi.Sms;

public sealed class SmsAyarlari
{
    public const string SectionName = "SmsAyarlari";

    public string ApiUrl { get; set; } = "";
    public string KullaniciAdi { get; set; } = "";
    public string Sifre { get; set; } = "";
    public string Baslik { get; set; } = "";
    public int ZamanAsimiSaniye { get; set; } = 30;
    public bool TurkceKarakter { get; set; } = true;
    public bool Aktif { get; set; } = true;

    // Toplu gönderimde aynı anda kaç SMS gönderilecek (SemaphoreSlim limiti).
    // Sağlayıcının pratik rate limit'i bilinmediği için ihtiyatlı default.
    public int MaxParalelGonderim { get; set; } = 5;
}
