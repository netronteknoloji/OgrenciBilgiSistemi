using System.Globalization;

namespace OgrenciBilgiSistemi.Sms;

/// <summary>
/// Sistemde gönderilen tüm veli SMS şablonlarının tek kaynağı.
/// MVC ve API tarafı bu sınıftaki metotları çağırır; mesaj metni
/// değişikliği yalnızca burada yapılır.
/// </summary>
public static class SmsMesajSablonlari
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    /// <summary>
    /// Ana kapı kart geçişi (Giriş/Çıkış) bildirimi.
    /// </summary>
    public static string AnaKapiGecis(string adSoyad, DateTime zaman, string gecisTipi)
    {
        var tip = gecisTipi.ToLower(Tr);
        var yon = string.Equals(gecisTipi, "Giriş", StringComparison.OrdinalIgnoreCase)
            ? "okula"
            : "okuldan";
        return $"Sayın Veli, {adSoyad} {zaman:HH:mm} saatinde {yon} {tip} yapmıştır.";
    }

    /// <summary>
    /// Yemekhane günlük tek giriş bildirimi.
    /// </summary>
    public static string YemekhaneGiris(string adSoyad, DateTime zaman) =>
        $"Sayın Veli, {adSoyad} {zaman:HH:mm} saatinde yemekhaneye giriş yapmıştır.";

    /// <summary>
    /// Servis yoklaması (sabah/akşam) sonrası veliye binme durumu bildirimi.
    /// periyot: 1=sabah, 2=akşam. durumId: 1=bindi, diğer=binmedi.
    /// </summary>
    public static string ServisYoklamasi(string adSoyad, int periyot, int durumId)
    {
        var periyotMetni = periyot == 1 ? "sabah" : "akşam";
        var durumMetni = durumId == 1 ? "binmiştir" : "binmemiştir";
        return $"Sayın Veli, {adSoyad} bugün {periyotMetni} servisine {durumMetni}.";
    }

    /// <summary>
    /// Sınıf yoklamasında devamsız işaretlenen öğrenci için veliye bildirim.
    /// </summary>
    public static string SinifYoklamasiDevamsiz(string adSoyad, int dersNumarasi) =>
        $"Sayın Veli, {adSoyad} bugün {dersNumarasi}. ders saatinde devamsız olarak işaretlenmiştir.";
}
