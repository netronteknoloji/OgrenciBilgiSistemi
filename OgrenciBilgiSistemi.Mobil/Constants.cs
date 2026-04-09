namespace OgrenciBilgiSistemi.Mobil;

// Uygulama genelinde kullanılan sabitler
public static class Constants
{
    // Merkezi okul kayıt sunucusu URL'i
    public const string KayitSunucuUrl = "http://www.netronyazilim.com/okullar.json";

    // Kayıt sunucusuna ulaşılamazsa ve cache yoksa kullanılacak varsayılan API URL'i
    public const string VarsayilanApiUrl = "http://81.214.75.22:5196/api/";

    // Aktif okulun API URL'i (Preferences'dan dinamik olarak okunur)
    public static string AktifApiUrl => Preferences.Default.Get("AktifOkulApiUrl", VarsayilanApiUrl);

    /// <summary>
    /// Sunucu kök URL'i (API prefix'i olmadan). Resim URL'leri için kullanılır.
    /// </summary>
    public static string SunucuBaseUrl
    {
        get
        {
            var url = AktifApiUrl;
            // "http://host:port/api/" → "http://host:port"
            var idx = url.IndexOf("/api", StringComparison.OrdinalIgnoreCase);
            return idx >= 0 ? url[..idx] : url.TrimEnd('/');
        }
    }

    /// <summary>
    /// Veritabanındaki görsel yolunu (/uploads/xxx.jpg) tam URL'ye çevirir.
    /// </summary>
    public static string GorselUrl(string? gorselYol)
    {
        if (string.IsNullOrWhiteSpace(gorselYol) || gorselYol == "user_icon.png")
            return "user_icon.png";

        // Zaten tam URL ise dokunma
        if (gorselYol.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return gorselYol;

        return SunucuBaseUrl + gorselYol;
    }
}
