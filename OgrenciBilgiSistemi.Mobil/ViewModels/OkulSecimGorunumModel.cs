using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OgrenciBilgiSistemi.Mobil.Models;
using OgrenciBilgiSistemi.Mobil.Services;
// Constants OgrenciBilgiSistemi.Mobil root namespace'inde — implicit global using ile erismek yeterli.

namespace OgrenciBilgiSistemi.Mobil.ViewModels
{
    public partial class OkulSecimGorunumModel : ObservableObject
    {
        private readonly GirisService _girisService;
        private readonly OkulKayitServisi _okulKayitServisi;
        private readonly GenelAdminGirisGecisService _gecis;
        private string? _sifre;

        [ObservableProperty] private IReadOnlyList<OkulBilgi> okulListesi = [];
        [ObservableProperty] private bool yukleniyor;
        [ObservableProperty] private string bosMesaj = "Yükleniyor...";

        public OkulSecimGorunumModel(
            GirisService girisService,
            OkulKayitServisi okulKayitServisi,
            GenelAdminGirisGecisService gecis)
        {
            _girisService = girisService;
            _okulKayitServisi = okulKayitServisi;
            _gecis = gecis;
        }

        [RelayCommand]
        async Task YukleAsync()
        {
            _sifre = _gecis.TukutVeAl();
            if (string.IsNullOrEmpty(_sifre))
            {
                await Application.Current!.MainPage!.DisplayAlert("Uyarı", "Oturum bilgisi bulunamadı. Lütfen tekrar giriş yapınız.", "Tamam");
                await Shell.Current.GoToAsync("///GirisView");
                return;
            }

            try
            {
                var okullar = await _okulKayitServisi.OkullariGetirAsync();
                if (okullar.Count == 0) { BosMesaj = "Okul listesi alınamadı."; return; }
                OkulListesi = okullar;
            }
            catch
            {
                BosMesaj = "Okul listesi alınamadı.";
            }
        }

        [RelayCommand]
        async Task OkulSecAsync(OkulBilgi okul)
        {
            if (string.IsNullOrEmpty(_sifre))
            {
                await Application.Current!.MainPage!.DisplayAlert("Uyarı", "Oturum bilgisi yok. Lütfen tekrar giriş yapınız.", "Tamam");
                await Shell.Current.GoToAsync("///GirisView");
                return;
            }

            Yukleniyor = true;
            try
            {
                Preferences.Default.Set("AktifOkulApiUrl", okul.ApiUrl);
                bool basarili = await _girisService.KullaniciGirisYapAsync(
                    Constants.GenelAdminKullaniciAdi, _sifre, okul.OkulKodu);
                _sifre = null;

                if (basarili)
                    await Shell.Current.GoToAsync("///AdminAnaSayfaView");
                else
                {
                    await Application.Current!.MainPage!.DisplayAlert("Hata", "Genel Yönetici girişi başarısız. Lütfen tekrar deneyiniz.", "Tamam");
                    await Shell.Current.GoToAsync("///GirisView");
                }
            }
            catch
            {
                await Application.Current!.MainPage!.DisplayAlert("Bağlantı Hatası", "Sunucuya erişilemedi. Lütfen tekrar deneyiniz.", "Tamam");
            }
            finally
            {
                Yukleniyor = false;
            }
        }

        public void Temizle()
        {
            _sifre = null;
            _gecis.Temizle();
        }
    }
}
