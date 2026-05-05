using OgrenciBilgiSistemi.Mobil.Models;
using OgrenciBilgiSistemi.Mobil.Services;

namespace OgrenciBilgiSistemi.Mobil.Views
{
    public partial class AdminOgrenciListeView : ContentPage
    {
        private readonly OgrenciService _ogrenciService;
        private List<Ogrenci> _tumOgrenciler = new();

        public AdminOgrenciListeView(OgrenciService ogrenciService)
        {
            InitializeComponent();
            _ogrenciService = ogrenciService;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                _tumOgrenciler = await _ogrenciService.TumOgrencileriGetirAsync();
                Filtrele(AramaCubugu.Text);

                if (_tumOgrenciler.Count == 0)
                    BosDurumLabel.Text = "Kayıtlı öğrenci bulunamadı.";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AdminOgrenciListe Yükleme Hatası: {ex.Message}");
                BosDurumLabel.Text = "Veriler yüklenemedi.";
            }
        }

        private void OnAramaMetniDegisti(object sender, TextChangedEventArgs e)
            => Filtrele(e.NewTextValue);

        private void Filtrele(string? arama)
        {
            var temiz = arama?.Trim() ?? string.Empty;

            IReadOnlyList<Ogrenci> sonuc = string.IsNullOrEmpty(temiz)
                ? _tumOgrenciler
                : _tumOgrenciler
                    .Where(o => Eslesir(o, temiz))
                    .ToList();

            OgrenciCollection.ItemsSource = sonuc;
            AltBaslikLabel.Text = string.IsNullOrEmpty(temiz)
                ? $"Toplam {_tumOgrenciler.Count} öğrenci"
                : $"{sonuc.Count} / {_tumOgrenciler.Count} öğrenci";

            BosDurumLabel.Text = _tumOgrenciler.Count == 0
                ? "Kayıtlı öğrenci bulunamadı."
                : sonuc.Count == 0
                    ? "Aramanızla eşleşen öğrenci yok."
                    : "";
        }

        private static bool Eslesir(Ogrenci o, string arama)
        {
            if (!string.IsNullOrEmpty(o.OgrenciAdSoyad) &&
                o.OgrenciAdSoyad.Contains(arama, StringComparison.OrdinalIgnoreCase))
                return true;

            if (o.OgrenciNo.ToString().Contains(arama, StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        private async void OnOgrenciSecildi(object sender, TappedEventArgs e)
        {
            if ((sender as Border)?.BindingContext is Ogrenci ogrenci)
                await Navigation.PushAsync(new OgrenciDetayView(ogrenci.OgrenciId, _ogrenciService));
        }
    }
}
