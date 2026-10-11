namespace OgrenciBilgiSistemi.Infrastructure
{
    /// <summary>
    /// Birden çok menüye bağlı controller'larda hangi action'ın hangi menüyü gerektirdiği.
    /// Değerler menünün Action adıdır (MenuOgeler.Action); listedeki menülerden biri yeterlidir.
    /// Haritada olmayan action, controller'ın işlem menüsünü ("Index") ister: böylece yalnızca
    /// rapor menüsü verilen kullanıcı öğrenci ekleme/silme, ödeme gibi işlemlere erişemez.
    /// Haritada olmayan controller'larda controller'a ait herhangi bir menü yeterlidir (eski davranış).
    /// </summary>
    public static class MenuIslemHaritasi
    {
        private const string Islem = "Index";

        private static readonly Dictionary<string, Dictionary<string, string[]>> _harita =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["Ogrenciler"] = new(StringComparer.OrdinalIgnoreCase)
                {
                    ["OgrenciVeliRapor"] = ["OgrenciVeliRapor"],
                    ["OgrenciVeliRaporExcel"] = ["OgrenciVeliRapor"],
                },
                ["Aidat"] = new(StringComparer.OrdinalIgnoreCase)
                {
                    ["AidatRapor"] = ["AidatRapor"],
                    ["AidatRaporExcel"] = ["AidatRapor"],
                    // Öğrencinin aidat özeti hem işlem hem rapor ekranından açılır (salt okuma;
                    // içindeki ödeme/tarife işlemleri ayrıca [MenuIslemYetki] ile işlem menüsü ister)
                    ["Ozet"] = [Islem, "AidatRapor"],
                },
                ["Yemekhane"] = new(StringComparer.OrdinalIgnoreCase)
                {
                    ["YemekRapor"] = ["YemekRapor"],
                    ["YemekRaporExcel"] = ["YemekRapor"],
                    ["Ozet"] = [Islem, "YemekRapor"],
                },
                ["Ziyaretciler"] = new(StringComparer.OrdinalIgnoreCase)
                {
                    ["ZiyaretciRapor"] = ["ZiyaretciRapor"],
                    ["RaporExcel"] = ["ZiyaretciRapor"],
                    ["Detay"] = [Islem, "ZiyaretciRapor"],
                },
            };

        /// <summary>Gereken menü action adları; null ise controller'a ait herhangi bir menü yeterli.</summary>
        public static IReadOnlyList<string>? GerekenMenuler(string controller, string? action)
        {
            if (!_harita.TryGetValue(controller, out var actionlar))
                return null;
            return action is not null && actionlar.TryGetValue(action, out var menuler) ? menuler : [Islem];
        }
    }
}
