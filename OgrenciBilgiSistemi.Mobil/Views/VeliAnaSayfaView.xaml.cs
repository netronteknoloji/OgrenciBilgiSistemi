using OgrenciBilgiSistemi.Mobil.Models;
using OgrenciBilgiSistemi.Mobil.Services;

namespace OgrenciBilgiSistemi.Mobil.Views
{
    public partial class VeliAnaSayfaView : ContentPage
    {
        private readonly VeliService _veliService;

        public VeliAnaSayfaView(VeliService veliService)
        {
            try
            {
                InitializeComponent();
                _veliService = veliService;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"VeliAnaSayfaView Init Hatası: {ex.Message}");
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                WelcomeLabel.Text = $"Merhaba, {KullaniciOturum.AdSoyad}";

                var cocuklar = await _veliService.CocuklarimiGetir();
                CocukCollection.ItemsSource = cocuklar;
                CocukSayisiLabel.Text = cocuklar.Count > 0
                    ? $"{cocuklar.Count} çocuk kayıtlı"
                    : "Kayıtlı öğrenci bulunamadı";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"VeliAnaSayfa Yükleme Hatası: {ex.Message}");
                CocukSayisiLabel.Text = "Veriler yüklenemedi";
            }
        }

        private async void OnCocukTapped(object sender, TappedEventArgs e)
        {
            try
            {
                if (e.Parameter is Ogrenci ogrenci)
                {
                    await Navigation.PushAsync(new OgrenciDetayView(ogrenci.OgrenciId));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Öğrenci Detay Hatası: {ex.Message}");
            }
        }
    }
}
