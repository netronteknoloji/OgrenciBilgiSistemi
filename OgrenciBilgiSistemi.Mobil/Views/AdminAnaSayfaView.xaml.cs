using OgrenciBilgiSistemi.Mobil.Services;

namespace OgrenciBilgiSistemi.Mobil.Views
{
    public partial class AdminAnaSayfaView : ContentPage
    {
        private readonly AdminService _adminService;

        public AdminAnaSayfaView(AdminService adminService)
        {
            try
            {
                InitializeComponent();
                _adminService = adminService;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AdminAnaSayfaView Init Hatası: {ex.Message}");
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                WelcomeLabel.Text = $"Merhaba, {KullaniciOturum.AdSoyad}";

                var ozet = await _adminService.OkulOzetGetir();
                if (ozet is null)
                {
                    OkulAdiLabel.Text = "Veriler yüklenemedi";
                    return;
                }

                OkulAdiLabel.Text = string.IsNullOrWhiteSpace(ozet.OkulAdi) ? "Okul Özeti" : ozet.OkulAdi;
                OgrenciSayisiLabel.Text = ozet.ToplamOgrenci.ToString();
                OgretmenSayisiLabel.Text = ozet.ToplamOgretmen.ToString();
                SinifSayisiLabel.Text = ozet.ToplamSinif.ToString();
                VeliSayisiLabel.Text = ozet.ToplamVeli.ToString();
                YemekhaneSayisiLabel.Text = ozet.BugunYemekhaneGiris.ToString();
                AnakapiSayisiLabel.Text = ozet.BugunAnakapiCikis.ToString();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AdminAnaSayfa Yükleme Hatası: {ex.Message}");
                OkulAdiLabel.Text = "Veriler yüklenemedi";
            }
        }

        private async void OnOgrencilerTapped(object sender, TappedEventArgs e)
            => await YakindaUyarisi();

        private async void OnOgretmenlerTapped(object sender, TappedEventArgs e)
            => await YakindaUyarisi();

        private async void OnSiniflarTapped(object sender, TappedEventArgs e)
            => await YakindaUyarisi();

        private async void OnVelilerTapped(object sender, TappedEventArgs e)
            => await YakindaUyarisi();

        private async void OnYemekhaneTapped(object sender, TappedEventArgs e)
            => await YakindaUyarisi();

        private async void OnAnakapiTapped(object sender, TappedEventArgs e)
            => await YakindaUyarisi();

        private Task YakindaUyarisi()
            => DisplayAlert("Bilgi", "Bu özellik yakında eklenecek.", "Tamam");
    }
}
