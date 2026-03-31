namespace OgrenciBilgiSistemi.ViewModels
{
    /// <summary>
    /// Ortak sayfalama partial view'ı (_SayfalamaPartial) için kullanılan ViewModel.
    /// </summary>
    public class SayfalamaVm
    {
        public int SayfaIndeks { get; set; }
        public int ToplamSayfa { get; set; }
        public int ToplamSayi { get; set; }
        public bool OncekiSayfaVar { get; set; }
        public bool SonrakiSayfaVar { get; set; }

        /// <summary>Sayfa route parametre adı (varsayılan "page", bazı view'lar "pageNumber" kullanıyor).</summary>
        public string SayfaParametreAdi { get; set; } = "page";

        /// <summary>Hedef action adı (varsayılan "Index").</summary>
        public string Action { get; set; } = "Index";

        /// <summary>Sayfa numarası dışındaki korunacak route parametreleri.</summary>
        public Dictionary<string, string?> RouteValues { get; set; } = new();

        /// <summary>
        /// SayfalanmisListeModel'den kolayca SayfalamaViewModel oluşturur.
        /// </summary>
        public static SayfalamaVm Olustur<T>(
            SayfalanmisListeModel<T> liste,
            string sayfaParametreAdi = "page",
            string action = "Index",
            Dictionary<string, string?>? routeValues = null)
        {
            return new SayfalamaVm
            {
                SayfaIndeks = liste.SayfaIndeks,
                ToplamSayfa = liste.ToplamSayfa,
                ToplamSayi = liste.ToplamSayi,
                OncekiSayfaVar = liste.OncekiSayfaVar,
                SonrakiSayfaVar = liste.SonrakiSayfaVar,
                SayfaParametreAdi = sayfaParametreAdi,
                Action = action,
                RouteValues = routeValues ?? new()
            };
        }
    }
}
