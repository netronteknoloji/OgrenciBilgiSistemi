namespace OgrenciBilgiSistemi.Mobil.Services
{
    // Aktif kullanıcı rolünün tema kaynaklarını Colors.xaml'deki
    // <Rol>Renk / <Rol>Soft / <Rol>BaslikGradyan anahtarlarından çözer.
    // Rol paylaşımlı ekranlar (RandevuListeView, BildirimListeView) buradan beslenir.
    public static class RolTema
    {
        private static string Onek => KullaniciOturum.OgretmenMi ? "Ogretmen"
                                    : KullaniciOturum.VeliMi ? "Veli"
                                    : KullaniciOturum.ServisMi ? "Servis"
                                    : KullaniciOturum.AdminMi ? "Admin"
                                    : "Marka";

        public static Color VurguRenk => Kaynak(Onek == "Marka" ? "Marka" : Onek + "Renk", Color.FromArgb("#4C6EF5"));

        public static Color SoftRenk => Kaynak(Onek + "Soft", Color.FromArgb("#EEF1FE"));

        public static Brush BaslikGradyan => Kaynak<Brush>(Onek == "Marka" ? "MarkaGradyan" : Onek + "BaslikGradyan",
                                                           new SolidColorBrush(VurguRenk));

        private static Color Kaynak(string anahtar, Color varsayilan) => Kaynak<Color>(anahtar, varsayilan);

        private static T Kaynak<T>(string anahtar, T varsayilan)
        {
            if (Application.Current?.Resources.TryGetValue(anahtar, out var deger) == true && deger is T bulunan)
                return bulunan;
            return varsayilan;
        }
    }
}
