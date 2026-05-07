using OgrenciBilgiSistemi.Mobil.Models;
using OgrenciBilgiSistemi.Mobil.Services;

namespace OgrenciBilgiSistemi.Mobil.Views
{
    public partial class OkulSecimView : ContentPage
    {
        private readonly GirisService _girisService;
        private readonly OkulKayitServisi _okulKayitServisi;
        private readonly GenelAdminGirisGecisService _gecis;
        private string? _sifre;

        public OkulSecimView(
            GirisService girisService,
            OkulKayitServisi okulKayitServisi,
            GenelAdminGirisGecisService gecis)
        {
            InitializeComponent();
            _girisService = girisService;
            _okulKayitServisi = okulKayitServisi;
            _gecis = gecis;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            _sifre = _gecis.TukutVeAl();
            if (string.IsNullOrEmpty(_sifre))
            {
                await DisplayAlert("Uyarı", "Oturum bilgisi bulunamadı. Lütfen tekrar giriş yapınız.", "Tamam");
                await Shell.Current.GoToAsync("///GirisView");
                return;
            }

            try
            {
                var okullar = await _okulKayitServisi.OkullariGetirAsync();
                if (okullar.Count == 0)
                {
                    BosDurumLabel.Text = "Okul listesi alınamadı.";
                    return;
                }

                OkulCollection.ItemsSource = okullar;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OkulSecim Yükleme Hatası: {ex.Message}");
                BosDurumLabel.Text = "Okul listesi alınamadı.";
            }
        }

        private async void OnOkulSecildi(object sender, TappedEventArgs e)
        {
            if (sender is not VisualElement ve || ve.BindingContext is not OkulBilgi okul)
                return;

            if (string.IsNullOrEmpty(_sifre))
            {
                await DisplayAlert("Uyarı", "Oturum bilgisi yok. Lütfen tekrar giriş yapınız.", "Tamam");
                await Shell.Current.GoToAsync("///GirisView");
                return;
            }

            try
            {
                GirisYukleniyor.IsVisible = true;
                GirisYukleniyor.IsRunning = true;
                OkulCollection.IsEnabled = false;

                Preferences.Default.Set("AktifOkulApiUrl", okul.ApiUrl);

                bool basarili = await _girisService.KullaniciGirisYapAsync(
                    Constants.GenelAdminKullaniciAdi, _sifre, okul.OkulKodu);

                if (basarili)
                {
                    _sifre = null;
                    await Shell.Current.GoToAsync("///AdminAnaSayfaView");
                }
                else
                {
                    await DisplayAlert("Hata", "Genel Yönetici girişi başarısız. Lütfen tekrar deneyiniz.", "Tamam");
                    _sifre = null;
                    await Shell.Current.GoToAsync("///GirisView");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GenelAdmin OkulSec Hatası: {ex.Message}");
                await DisplayAlert("Bağlantı Hatası", "Sunucuya erişilemedi. Lütfen tekrar deneyiniz.", "Tamam");
            }
            finally
            {
                GirisYukleniyor.IsVisible = false;
                GirisYukleniyor.IsRunning = false;
                OkulCollection.IsEnabled = true;
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _sifre = null;
            _gecis.Temizle();
        }
    }
}
