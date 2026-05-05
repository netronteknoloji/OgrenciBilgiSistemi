using OgrenciBilgiSistemi.Mobil.Services;

namespace OgrenciBilgiSistemi.Mobil.Views
{
    public partial class AdminAnaSayfaView : ContentPage
    {
        private readonly AdminService _adminService;

        public AdminAnaSayfaView(AdminService adminService)
        {
            InitializeComponent();
            _adminService = adminService;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            WelcomeLabel.Text = $"Merhaba, {KullaniciOturum.AdSoyad}";
            try
            {
                var ozet = await _adminService.OkulOzetGetir();
                if (ozet is null)
                {
                    OkulAdiLabel.Text = "Veriler yüklenemedi";
                    return;
                }

                OkulAdiLabel.Text = string.IsNullOrWhiteSpace(ozet.OkulAdi) ? "Okul Özeti" : ozet.OkulAdi;
                OgrenciSayisiLabel.Text   = ozet.ToplamOgrenci.ToString();
                OgretmenSayisiLabel.Text  = ozet.ToplamOgretmen.ToString();
                SinifSayisiLabel.Text     = ozet.ToplamSinif.ToString();
                VeliSayisiLabel.Text      = ozet.ToplamVeli.ToString();
                YemekhaneSayisiLabel.Text = ozet.BugunYemekhaneGiris.ToString();
                AnakapiSayisiLabel.Text   = ozet.BugunAnakapiCikis.ToString();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AdminAnaSayfa Yükleme Hatası: {ex.Message}");
                OkulAdiLabel.Text = "Veriler yüklenemedi";
            }
        }

        private async void OnOgrencilerTapped(object sender, TappedEventArgs e)
        {
            var svc = Servis<OgrenciService>();
            if (svc is null) return;
            await Navigation.PushAsync(new AdminOgrenciListeView(svc));
        }

        private async void OnOgretmenlerTapped(object sender, TappedEventArgs e)
        {
            var svc = Servis<OgretmenListeService>();
            if (svc is null) return;
            await Navigation.PushAsync(new AdminOgretmenListeView(svc));
        }

        private async void OnSiniflarTapped(object sender, TappedEventArgs e)
        {
            var svc = Servis<SinifService>();
            if (svc is null) return;
            await Navigation.PushAsync(new AdminSinifListeView(svc));
        }

        private async void OnVelilerTapped(object sender, TappedEventArgs e)
        {
            var svc = Servis<VeliListeService>();
            if (svc is null) return;
            await Navigation.PushAsync(new AdminVeliListeView(svc));
        }

        private async void OnYemekhaneTapped(object sender, TappedEventArgs e)
            => await Navigation.PushAsync(new AdminYemekhaneBugunView(_adminService));

        private async void OnAnakapiTapped(object sender, TappedEventArgs e)
            => await Navigation.PushAsync(new AdminAnakapiCikisBugunView(_adminService));

        private static T? Servis<T>() where T : class
            => Application.Current?.MainPage?.Handler?.MauiContext?.Services.GetService<T>();
    }
}
