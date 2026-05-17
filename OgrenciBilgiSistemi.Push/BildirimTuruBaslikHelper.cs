namespace OgrenciBilgiSistemi.Push;

/// <summary>
/// BildirimTuru değerlerini push notification başlığına dönüştürür.
/// Shared'deki BildirimTuru enum'ına Push projesinin doğrudan referansı olmadığı için
/// integer/string çevrim helper'ları MVC ve API kendi tarafında kullanır.
/// </summary>
public static class BildirimTuruBaslikHelper
{
    public static string BasligaCevir(int tur) => tur switch
    {
        1 => "Yeni Randevu",
        2 => "Randevu Onaylandı",
        3 => "Randevu Reddedildi",
        4 => "Randevu İptal Edildi",
        5 => "Randevu Hatırlatma",
        6 => "Yeni Duyuru",
        _ => "Bildirim"
    };
}
