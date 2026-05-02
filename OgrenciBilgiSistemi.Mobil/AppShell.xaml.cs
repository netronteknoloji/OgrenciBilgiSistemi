using OgrenciBilgiSistemi.Mobil.Views;

namespace OgrenciBilgiSistemi.Mobil
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute(nameof(RandevuListeView), typeof(RandevuListeView));
            Routing.RegisterRoute(nameof(RandevuDetayView), typeof(RandevuDetayView));
            Routing.RegisterRoute(nameof(RandevuOlusturView), typeof(RandevuOlusturView));
            Routing.RegisterRoute(nameof(OgretmenRandevuYonetimView), typeof(OgretmenRandevuYonetimView));
            Routing.RegisterRoute(nameof(BildirimListeView), typeof(BildirimListeView));
            Routing.RegisterRoute(nameof(OgretmenDuyuruOlusturView), typeof(OgretmenDuyuruOlusturView));
            Routing.RegisterRoute(nameof(VeliDuyurularView), typeof(VeliDuyurularView));
        }
    }
}
