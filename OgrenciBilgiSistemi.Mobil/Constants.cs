namespace OgrenciBilgiSistemi.Mobil;

// Uygulama genelinde kullanılan sabitler
public static class Constants
{
    // API baz URL'i - değiştirmek için buradan güncelle
    public const string ApiBaseUrl = "http://81.214.75.22:5196/api/";

    /// <summary>
    /// Sunucu kök URL'i (API prefix'i olmadan). Resim URL'leri için kullanılır.
    /// </summary>
    public static string SunucuBaseUrl
    {
        get
        {
            var url = Preferences.Default.Get("ApiBaseUrl", ApiBaseUrl);
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
