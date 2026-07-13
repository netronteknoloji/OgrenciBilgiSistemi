using OgrenciBilgiSistemi.Shared.Enums;

namespace OgrenciBilgiSistemi.Shared.Constants;

// Yoklama durumlarına karşılık gelen Bootstrap 5 renk paleti.
// Mobil hex çeker, MVC CSS class çeker; iki katman görsel olarak hizalı.
public static class YoklamaRenkleri
{
    public static string HexGetir(YoklamaDurumu durum) => durum switch
    {
        YoklamaDurumu.Geldi    => "#198754",
        YoklamaDurumu.Gelmedi  => "#DC3545",
        YoklamaDurumu.GecGeldi => "#FFC107",
        YoklamaDurumu.Izinli   => "#0D6EFD",
        YoklamaDurumu.Raporlu  => "#6F42C1",
        YoklamaDurumu.Nobetci  => "#212529",
        YoklamaDurumu.Gorevli  => "#6C757D",
        _ => "#F8F9FA"
    };

    public static string HexGetir(int durumDegeri)
        => HexGetir((YoklamaDurumu)durumDegeri);

    public static string CssSinifGetir(YoklamaDurumu durum) => durum switch
    {
        YoklamaDurumu.Geldi    => "bg-success",
        YoklamaDurumu.Gelmedi  => "bg-danger",
        YoklamaDurumu.GecGeldi => "bg-warning text-dark",
        YoklamaDurumu.Izinli   => "bg-primary",
        YoklamaDurumu.Raporlu  => "bg-purple text-white",
        YoklamaDurumu.Nobetci  => "bg-dark",
        YoklamaDurumu.Gorevli  => "bg-secondary",
        _ => "bg-light text-dark"
    };

    public static string CssSinifGetir(int? durumDegeri)
    {
        if (durumDegeri is null || durumDegeri.Value < 1) return "bg-light text-dark";
        return CssSinifGetir((YoklamaDurumu)durumDegeri.Value);
    }

    // ServisYoklamaModel.DurumId: 1 = Bindi, 2 = Binmedi.
    public static string CssServisGetir(int durumId) => durumId switch
    {
        1 => "bg-success",
        2 => "bg-danger",
        _ => "bg-light text-dark"
    };

    // OgrenciDetayModel.OgrenciGecisTipi DB'de büyük harf tutuluyor: "GİRİŞ" / "ÇIKIŞ".
    public static string CssGecisGetir(string? gecisTipi) => gecisTipi switch
    {
        "GİRİŞ" => "bg-success",
        "ÇIKIŞ" => "bg-danger",
        _ => "bg-light text-dark"
    };

    public const string GirisHex = "#198754";
    public const string CikisHex = "#DC3545";
}
