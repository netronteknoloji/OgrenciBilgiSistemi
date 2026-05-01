namespace OgrenciBilgiSistemi.Sms;

public enum SmsHataKategorisi
{
    // Başarılı gönderim
    Yok = 0,

    // Yeniden denenebilir (timeout, 5xx, ağ hatası)
    Gecici = 1,

    // Yeniden denenmemeli (geçersiz numara, auth, validasyon)
    Kalici = 2,

    // Kategorize edilemedi - retry servisi ihtiyatlı olarak Gecici muamelesi yapar
    Bilinmiyor = 3
}
