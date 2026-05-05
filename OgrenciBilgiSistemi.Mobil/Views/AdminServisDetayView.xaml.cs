using OgrenciBilgiSistemi.Mobil.Models;
using OgrenciBilgiSistemi.Mobil.Services;

namespace OgrenciBilgiSistemi.Mobil.Views
{
    public partial class AdminServisDetayView : ContentPage
    {
        private readonly ServisService _servisService;
        private readonly ServisListeOgesi _servis;

        public AdminServisDetayView(ServisListeOgesi servis, ServisService servisService)
        {
            InitializeComponent();
            _servis = servis;
            _servisService = servisService;

            AdSoyadLabel.Text = servis.KullaniciAdi;
            DurumLabel.Text = servis.DurumMetni;
            DurumLabel.TextColor = servis.ServisDurum
                ? Color.FromArgb("#16A085")
                : Color.FromArgb("#C0392B");
            PlakaLabel.Text = servis.PlakaGosterim;
            TelefonLabel.Text = servis.TelefonGosterim;
            OgrenciSayisiLabel.Text = $"{servis.OgrenciSayisi} öğrenci";
            Title = $"Servis: {servis.KullaniciAdi}";
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await OgrencileriYukle();
        }

        private async Task OgrencileriYukle()
        {
            try
            {
                var ogrenciler = await _servisService.ServisOgrencileriGetir(_servis.KullaniciId);

                BindableLayout.SetItemsSource(OgrencilerStack, ogrenciler);
                OgrencilerBaslik.Text = ogrenciler.Count > 0
                    ? $"Öğrenciler ({ogrenciler.Count})"
                    : "Öğrenciler";

                if (ogrenciler.Count == 0)
                {
                    DurumYazisi.Text = "Bu servise atanmış öğrenci yok.";
                    DurumYazisi.IsVisible = true;
                }
                else
                {
                    DurumYazisi.IsVisible = false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AdminServisDetay Yükleme Hatası: {ex.Message}");
                DurumYazisi.Text = "Öğrenci listesi yüklenemedi.";
                DurumYazisi.IsVisible = true;
            }
        }

        private void OnTelefonTapped(object sender, EventArgs e)
        {
            try
            {
                var no = TelefonLabel.Text?.Trim();
                if (PhoneDialer.Default.IsSupported && !string.IsNullOrEmpty(no) && no != "—")
                    PhoneDialer.Default.Open(no);
            }
            catch { }
        }

        private async void OnOgrenciSecildi(object sender, TappedEventArgs e)
        {
            if ((sender as Border)?.BindingContext is Ogrenci ogrenci)
            {
                var ogrenciService = Servis<OgrenciService>();
                if (ogrenciService is null) return;
                await Navigation.PushAsync(new OgrenciDetayView(ogrenci.OgrenciId, ogrenciService));
            }
        }

        private static T? Servis<T>() where T : class
            => Application.Current?.MainPage?.Handler?.MauiContext?.Services.GetService<T>();
    }
}
