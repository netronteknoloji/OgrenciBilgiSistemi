using OgrenciBilgiSistemi.Mobil.Models;
using OgrenciBilgiSistemi.Mobil.Services;

namespace OgrenciBilgiSistemi.Mobil.Views
{
    public partial class VeliAnaSayfaView : ContentPage
    {
        private readonly VeliService _veliService;
        private readonly BildirimService _bildirimService;

        public VeliAnaSayfaView(VeliService veliService, BildirimService bildirimService)
        {
            try
            {
                InitializeComponent();
                _veliService = veliService;
                _bildirimService = bildirimService;
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

                // Okunmamış bildirim sayısını güncelle
                await BildirimBadgeGuncelle();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"VeliAnaSayfa Yükleme Hatası: {ex.Message}");
                CocukSayisiLabel.Text = "Veriler yüklenemedi";
            }
        }

        private async Task BildirimBadgeGuncelle()
        {
            try
            {
                var sayi = await _bildirimService.OkunmamisSayisiGetir();
                if (sayi > 0)
                {
                    BildirimBadge.IsVisible = true;
                    BildirimSayiLabel.Text = sayi > 9 ? "9+" : sayi.ToString();
                }
                else
                {
                    BildirimBadge.IsVisible = false;
                }
            }
            catch { }
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

        private async void OnRandevularTapped(object sender, TappedEventArgs e)
        {
            await Navigation.PushAsync(new RandevuListeView(
                Application.Current.MainPage.Handler.MauiContext.Services.GetService<RandevuService>()));
        }

        private async void OnBildirimlerTapped(object sender, TappedEventArgs e)
        {
            await Navigation.PushAsync(new BildirimListeView(
                Application.Current.MainPage.Handler.MauiContext.Services.GetService<BildirimService>()));
        }

        private async void OnDuyurularTapped(object sender, TappedEventArgs e)
        {
            await Navigation.PushAsync(new VeliDuyurularView(
                Application.Current.MainPage.Handler.MauiContext.Services.GetService<DuyuruService>()));
        }
    }
}
