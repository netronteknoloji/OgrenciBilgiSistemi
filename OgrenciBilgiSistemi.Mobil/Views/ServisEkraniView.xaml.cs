using OgrenciBilgiSistemi.Mobil.Models;
using OgrenciBilgiSistemi.Mobil.Services;

namespace OgrenciBilgiSistemi.Mobil.Views
{
    public partial class ServisEkraniView : ContentPage
    {
        private readonly ServisService _servisService;
        private List<Ogrenci> _tumOgrenciler = new();

        public ServisEkraniView(ServisService servisService)
        {
            try
            {
                InitializeComponent();
                _servisService = servisService;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ServisEkraniView Init Hatası: {ex.Message}");
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                WelcomeLabel.Text = $"Merhaba, {KullaniciOturum.AdSoyad}";

                var servisId = KullaniciOturum.ServisId;
                if (!servisId.HasValue)
                {
                    OgrenciSayisiLabel.Text = "Servis ataması bulunamadı";
                    return;
                }

                // Servis bilgisini getir (plaka)
                var servis = await _servisService.ServisGetir(servisId.Value);
                if (servis != null)
                    PlakaLabel.Text = $"Plaka: {servis.Plaka}";

                // Öğrencileri getir
                _tumOgrenciler = await _servisService.ServisOgrencileriGetir(servisId.Value);
                OgrenciCollection.ItemsSource = _tumOgrenciler;
                OgrenciSayisiLabel.Text = $"Toplam {_tumOgrenciler.Count} öğrenci";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ServisEkrani Yükleme Hatası: {ex.Message}");
                OgrenciSayisiLabel.Text = "Veriler yüklenemedi";
            }
        }

        private void OnSearchBarTextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                var filtre = e.NewTextValue?.Trim();
                if (string.IsNullOrEmpty(filtre))
                {
                    OgrenciCollection.ItemsSource = _tumOgrenciler;
                }
                else
                {
                    OgrenciCollection.ItemsSource = _tumOgrenciler
                        .Where(o => o.OgrenciAdSoyad != null &&
                                    o.OgrenciAdSoyad.Contains(filtre, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }
            }
            catch { }
        }

        private async void OnOgrenciTapped(object sender, TappedEventArgs e)
        {
            try
            {
                if (e.Parameter is Ogrenci ogrenci)
                {
                    await Shell.Current.GoToAsync($"OgrenciDetayView?ogrenciId={ogrenci.OgrenciId}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Öğrenci Detay Hatası: {ex.Message}");
            }
        }
    }
}
