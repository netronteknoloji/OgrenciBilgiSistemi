using OgrenciBilgiSistemi.Shared.Enums;

namespace OgrenciBilgiSistemi.Shared.Constants;

// Yoklama durumlarının Türkçe ekran metinleri. Renk karşılığı: YoklamaRenkleri.
public static class YoklamaMetinleri
{
    public static string MetinGetir(YoklamaDurumu durum) => durum switch
    {
        YoklamaDurumu.Geldi    => "Geldi",
        YoklamaDurumu.Gelmedi  => "Gelmedi",
        YoklamaDurumu.GecGeldi => "Geç Geldi",
        YoklamaDurumu.Izinli   => "İzinli",
        YoklamaDurumu.Raporlu  => "Raporlu",
        YoklamaDurumu.Nobetci  => "Nöbetçi",
        YoklamaDurumu.Gorevli  => "Görevli",
        _ => "-"
    };

    public static string MetinGetir(int? durumDegeri)
    {
        if (durumDegeri is null or < 1 or > 7) return "-";
        return MetinGetir((YoklamaDurumu)durumDegeri.Value);
    }

    // ServisYoklamaModel.DurumId: 1 = Bindi, 2 = Binmedi.
    public static string ServisMetinGetir(int durumId) => durumId switch
    {
        1 => "Bindi",
        2 => "Binmedi",
        _ => "-"
    };

    // OgrenciDetayModel.OgrenciGecisTipi DB'de büyük harf tutuluyor: "GİRİŞ" / "ÇIKIŞ".
    public static string GecisMetinGetir(string? gecisTipi) => gecisTipi switch
    {
        "GİRİŞ" => "Giriş",
        "ÇIKIŞ" => "Çıkış",
        _ => "-"
    };
}
