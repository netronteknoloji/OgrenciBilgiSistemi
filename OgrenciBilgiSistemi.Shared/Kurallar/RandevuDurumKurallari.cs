using OgrenciBilgiSistemi.Shared.Enums;

namespace OgrenciBilgiSistemi.Shared.Kurallar
{
    /// <summary>
    /// Randevu durum geçişleri (MVC ve API ortak). Onay/red yalnızca beklemedeki randevuya,
    /// iptal yalnızca beklemede veya onaylanmış randevuya yapılabilir; reddedilmiş, iptal edilmiş
    /// veya tamamlanmış randevu yeniden açılmaz.
    /// </summary>
    public static class RandevuDurumKurallari
    {
        public static bool YanitlanabilirMi(RandevuDurumu mevcut) => mevcut == RandevuDurumu.Beklemede;

        public static bool IptalEdilebilirMi(RandevuDurumu mevcut)
            => mevcut is RandevuDurumu.Beklemede or RandevuDurumu.Onaylandi;

        // SQL koşulları için (API ham SQL)
        public const string YanitlanabilirSqlKosulu = "Durum = 0";
        public const string IptalEdilebilirSqlKosulu = "Durum IN (0, 1)";
    }
}
