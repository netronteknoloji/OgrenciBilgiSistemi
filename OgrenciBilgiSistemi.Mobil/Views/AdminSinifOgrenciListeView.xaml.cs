using OgrenciBilgiSistemi.Mobil.Models;
using OgrenciBilgiSistemi.Mobil.Services;

namespace OgrenciBilgiSistemi.Mobil.Views
{
    public partial class AdminSinifOgrenciListeView : ContentPage
    {
        private readonly OgrenciService _ogrenciService;
        private readonly int _sinifId;
        private bool _yuklendi;

        public AdminSinifOgrenciListeView(OgrenciService ogrenciService, int sinifId, string sinifAdi)
        {
            InitializeComponent();
            _ogrenciService = ogrenciService;
            _sinifId = sinifId;
            SinifAdiLabel.Text = string.IsNullOrWhiteSpace(sinifAdi) ? "Sınıf" : sinifAdi;
            TarihSecici.Date = DateTime.Today;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (_yuklendi) return;
            _yuklendi = true;
            await YoklamaYukle(TarihSecici.Date.Value);
        }

        private async void OnTarihSecildi(object sender, DateChangedEventArgs e)
        {
            if (!_yuklendi) return;
            await YoklamaYukle(e.NewDate.Value);
        }

        private async Task YoklamaYukle(DateTime tarih)
        {
            try
            {
                BosDurumLabel.Text = "Yükleniyor...";
                OgrenciCollection.ItemsSource = null;

                var liste = await _ogrenciService.SinifYoklamaOzetiGetirAsync(_sinifId, tarih);
                OgrenciCollection.ItemsSource = liste;

                var yoklananSayi = liste.Count(o => o.KullaniciId.HasValue);
                AltBaslikLabel.Text = liste.Count == 0
                    ? "Öğrenci bulunamadı"
                    : $"{liste.Count} öğrenci · {yoklananSayi} yoklama kaydı";

                if (liste.Count == 0)
                    BosDurumLabel.Text = "Bu sınıfta kayıtlı öğrenci yok.";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AdminSinifYoklama Yükleme Hatası: {ex.Message}");
                BosDurumLabel.Text = "Veriler yüklenemedi.";
                AltBaslikLabel.Text = "";
            }
        }

        private async void OnOgrenciSecildi(object sender, TappedEventArgs e)
        {
            if ((sender as Border)?.BindingContext is SinifYoklamaOzet item && item.OgrenciId > 0)
                await Navigation.PushAsync(new OgrenciDetayView(item.OgrenciId, _ogrenciService));
        }
    }
}
