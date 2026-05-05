using OgrenciBilgiSistemi.Mobil.Models;
using OgrenciBilgiSistemi.Mobil.Services;

namespace OgrenciBilgiSistemi.Mobil.Views
{
    public partial class AdminVeliListeView : ContentPage
    {
        private readonly VeliListeService _veliListeService;
        private List<Veli> _tumVeliler = new();

        public AdminVeliListeView(VeliListeService veliListeService)
        {
            InitializeComponent();
            _veliListeService = veliListeService;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                _tumVeliler = await _veliListeService.AktifVelileriGetir();
                Filtrele(AramaCubugu.Text);

                if (_tumVeliler.Count == 0)
                    BosDurumLabel.Text = "Kayıtlı veli bulunamadı.";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AdminVeliListe Yükleme Hatası: {ex.Message}");
                BosDurumLabel.Text = "Veriler yüklenemedi.";
            }
        }

        private void OnAramaMetniDegisti(object sender, TextChangedEventArgs e)
            => Filtrele(e.NewTextValue);

        private void Filtrele(string? arama)
        {
            var temiz = arama?.Trim() ?? string.Empty;

            IReadOnlyList<Veli> sonuc = string.IsNullOrEmpty(temiz)
                ? _tumVeliler
                : _tumVeliler
                    .Where(v => !string.IsNullOrEmpty(v.KullaniciAdi)
                                && v.KullaniciAdi.Contains(temiz, StringComparison.OrdinalIgnoreCase))
                    .ToList();

            VeliCollection.ItemsSource = sonuc;
            AltBaslikLabel.Text = string.IsNullOrEmpty(temiz)
                ? $"Toplam {_tumVeliler.Count} veli"
                : $"{sonuc.Count} / {_tumVeliler.Count} veli";

            BosDurumLabel.Text = _tumVeliler.Count == 0
                ? "Kayıtlı veli bulunamadı."
                : sonuc.Count == 0
                    ? "Aramanızla eşleşen veli yok."
                    : "";
        }

        private async void OnVeliSecildi(object sender, TappedEventArgs e)
        {
            if ((sender as Border)?.BindingContext is Veli veli)
                await Navigation.PushAsync(new AdminVeliDetayView(veli.KullaniciId, _veliListeService));
        }
    }
}
