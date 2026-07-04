using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Storage;
using OgrenciBilgiSistemi.Mobil.Models;
using OgrenciBilgiSistemi.Mobil.Services;

namespace OgrenciBilgiSistemi.Mobil.ViewModels
{
    public partial class GirisGorunumModel : ObservableObject
    {
        private readonly GirisService _girisService;
        private readonly OkulKayitServisi _okulKayitServisi;
        private readonly GenelAdminGirisGecisService _genelAdminGecis;
        private List<OkulBilgi> _okullar = [];
        private CancellationTokenSource? _aramaIptalToken;
        private bool _oneridenSecildi;
        private bool _yuklendiMi;

        [ObservableProperty] private IReadOnlyList<string> okulAdlari = [];
        [ObservableProperty] private int secilenOkulIndex = -1;
        [ObservableProperty] private string kullaniciAdi = string.Empty;
        [ObservableProperty] private string sifre = string.Empty;
        [ObservableProperty] private bool beniHatirla;
        [ObservableProperty] private IReadOnlyList<string> oneriListesi = [];
        [ObservableProperty] private bool oneriListesiGorunur;
        [ObservableProperty] private bool sifreGizli = true;
        [ObservableProperty] private bool girisButonEtkin = true;
        [ObservableProperty] private string girisButonMetni = "Giriş Yap";

        public string SifreIkonKaynagi => SifreGizli ? "eye_off.png" : "eye_on.png";

        // Öneri kutusu yüksekliği öğe sayısına göre ayarlanır (altta boşluk kalmasın);
        // en çok 4 satır görünür, fazlası kaydırılır.
        private const double OneriSatirYuksekligi = 48;
        public double OneriKutusuYuksekligi => Math.Min(OneriListesi.Count, 4) * OneriSatirYuksekligi;

        public GirisGorunumModel(
            GirisService girisService,
            OkulKayitServisi okulKayitServisi,
            GenelAdminGirisGecisService genelAdminGecis)
        {
            _girisService = girisService;
            _okulKayitServisi = okulKayitServisi;
            _genelAdminGecis = genelAdminGecis;
        }

        partial void OnSecilenOkulIndexChanged(int value)
        {
            if (value >= 0 && value < _okullar.Count)
                Preferences.Default.Set("AktifOkulApiUrl", _okullar[value].ApiUrl);
        }

        partial void OnSifreGizliChanged(bool value) => OnPropertyChanged(nameof(SifreIkonKaynagi));

        partial void OnOneriListesiChanged(IReadOnlyList<string> value) => OnPropertyChanged(nameof(OneriKutusuYuksekligi));

        partial void OnKullaniciAdiChanged(string value)
        {
            if (_oneridenSecildi) { _oneridenSecildi = false; return; }
            _ = AramaDebounceAsync(value);
        }

        [RelayCommand]
        async Task YukleAsync()
        {
            if (_yuklendiMi) return;
            _yuklendiMi = true;
            await KayitliKimlikYukleAsync();
            await OkullariYukleAsync();
        }

        private async Task OkullariYukleAsync()
        {
            try
            {
                _okullar = await _okulKayitServisi.OkullariGetirAsync();
                OkulAdlari = _okullar.Select(o => o.OkulAdi).ToList();

                var savedOkulKodu = await SecureStorage.Default.GetAsync("SavedOkulKodu");
                if (!string.IsNullOrEmpty(savedOkulKodu))
                {
                    var idx = _okullar.FindIndex(o => o.OkulKodu == savedOkulKodu);
                    if (idx >= 0) SecilenOkulIndex = idx;
                }
            }
            catch { }
        }

        private async Task KayitliKimlikYukleAsync()
        {
            try
            {
                if (Preferences.Default.Get("IsRemembered", false))
                {
                    var savedUser = await SecureStorage.Default.GetAsync("SavedUsername");
                    var savedPass = await SecureStorage.Default.GetAsync("SavedPassword");
                    if (!string.IsNullOrEmpty(savedUser)) KullaniciAdi = savedUser;
                    if (!string.IsNullOrEmpty(savedPass)) Sifre = savedPass;
                    BeniHatirla = true;
                }
            }
            catch { }
        }

        [RelayCommand]
        void SifreToggle() => SifreGizli = !SifreGizli;

        [RelayCommand]
        void BeniHatirlaToggle() => BeniHatirla = !BeniHatirla;

        [RelayCommand]
        void OneriSec(string secilen)
        {
            _oneridenSecildi = true;
            KullaniciAdi = secilen;
            OneriListesiGorunur = false;
            OneriListesi = [];
        }

        [RelayCommand]
        async Task GirisAsync()
        {
            string username = KullaniciAdi.Trim();
            string password = Sifre.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                await Application.Current!.MainPage!.DisplayAlert("Uyarı", "Lütfen kullanıcı adı ve şifre giriniz.", "Tamam");
                return;
            }

            if (password.Length < 4 || password.Length > 50)
            {
                await Application.Current!.MainPage!.DisplayAlert("Uyarı", "Şifre 4-50 karakter olmalıdır.", "Tamam");
                return;
            }

            if (string.Equals(username, Constants.GenelAdminKullaniciAdi, StringComparison.OrdinalIgnoreCase))
            {
                _genelAdminGecis.Ayarla(password);
                await Shell.Current.GoToAsync("///OkulSecimView");
                return;
            }

            if (SecilenOkulIndex < 0)
            {
                await Application.Current!.MainPage!.DisplayAlert("Uyarı", "Lütfen okul seçiniz.", "Tamam");
                return;
            }

            var secilenOkul = _okullar[SecilenOkulIndex];
            Preferences.Default.Set("AktifOkulApiUrl", secilenOkul.ApiUrl);

            GirisButonEtkin = false;
            GirisButonMetni = "Giriş Yapılıyor...";

            try
            {
                bool isSuccess = await _girisService.KullaniciGirisYapAsync(username, password, secilenOkul.OkulKodu);

                if (isSuccess)
                {
                    await BeniHatirlaYonetAsync(username, password, secilenOkul.OkulKodu);

                    if (KullaniciOturum.AdminMi)
                        await Shell.Current.GoToAsync("///AdminAnaSayfaView");
                    else if (KullaniciOturum.VeliMi)
                        await Shell.Current.GoToAsync("///VeliAnaSayfaView");
                    else if (KullaniciOturum.ServisMi)
                        await Shell.Current.GoToAsync("///ServisEkraniView");
                    else
                        await Shell.Current.GoToAsync("///SinifListeView");
                }
                else
                {
                    await Application.Current!.MainPage!.DisplayAlert("Hata", "Kullanıcı adı veya şifre hatalı. Lütfen tekrar deneyin.", "Tamam");
                }
            }
            catch
            {
                await Application.Current!.MainPage!.DisplayAlert("Bağlantı Hatası",
                    "Sunucuya erişilemedi. Lütfen internet bağlantınızı kontrol edin veya daha sonra tekrar deneyin.", "Tamam");
            }
            finally
            {
                GirisButonEtkin = true;
                GirisButonMetni = "Giriş Yap";
            }
        }

        private async Task BeniHatirlaYonetAsync(string user, string pass, string okulKodu)
        {
            try
            {
                await SecureStorage.Default.SetAsync("SavedOkulKodu", okulKodu);
                if (BeniHatirla)
                {
                    await SecureStorage.Default.SetAsync("SavedUsername", user);
                    await SecureStorage.Default.SetAsync("SavedPassword", pass);
                    Preferences.Default.Set("IsRemembered", true);
                }
                else
                {
                    SecureStorage.Default.Remove("SavedUsername");
                    SecureStorage.Default.Remove("SavedPassword");
                    Preferences.Default.Set("IsRemembered", false);
                }
            }
            catch { }
        }

        private async Task AramaDebounceAsync(string metin)
        {
            try
            {
                if (string.IsNullOrEmpty(metin) || metin.Length < 3 || SecilenOkulIndex < 0)
                {
                    OneriListesiGorunur = false;
                    OneriListesi = [];
                    return;
                }

                _aramaIptalToken?.Cancel();
                _aramaIptalToken = new CancellationTokenSource();
                var token = _aramaIptalToken.Token;

                await Task.Delay(300, token);
                if (token.IsCancellationRequested) return;

                var secilenOkul = _okullar[SecilenOkulIndex];
                var sonuclar = await _girisService.KullaniciAdiAraAsync(metin, secilenOkul.OkulKodu);

                if (token.IsCancellationRequested) return;

                if (sonuclar.Count > 0)
                {
                    OneriListesi = sonuclar;
                    OneriListesiGorunur = true;
                }
                else
                {
                    OneriListesiGorunur = false;
                    OneriListesi = [];
                }
            }
            catch (TaskCanceledException) { }
            catch { }
        }
    }
}
