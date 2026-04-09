using OgrenciBilgiSistemi.Mobil.Services;
using OgrenciBilgiSistemi.Mobil.Models;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace OgrenciBilgiSistemi.Mobil.Views
{
    public partial class GirisView : ContentPage
    {
        private readonly GirisService _girisService;
        private readonly OkulKayitServisi _okulKayitServisi;
        private CancellationTokenSource _aramaIptalToken;
        private bool _oneridenSecildi;
        private List<OkulBilgi> _okullar = new();

        public GirisView(GirisService girisService, OkulKayitServisi okulKayitServisi)
        {
            try
            {
                InitializeComponent();
                _girisService = girisService;
                _okulKayitServisi = okulKayitServisi;
                _ = IlkYuklemeAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GirisView Init Hatası: {ex.Message}");
            }
        }

        private async Task IlkYuklemeAsync()
        {
            await LoadSavedCredentialsAsync();
            await OkullariYukleAsync();
        }

        private async Task OkullariYukleAsync()
        {
            try
            {
                _okullar = await _okulKayitServisi.OkullariGetirAsync();
                PckOkul.ItemsSource = _okullar.Select(o => o.OkulAdi).ToList();

                // Okul seçimi değiştiğinde aktif API URL'ini güncelle
                PckOkul.SelectedIndexChanged += (s, e) =>
                {
                    if (PckOkul.SelectedIndex >= 0 && PckOkul.SelectedIndex < _okullar.Count)
                    {
                        var okul = _okullar[PckOkul.SelectedIndex];
                        Preferences.Default.Set("AktifOkulApiUrl", okul.ApiUrl);
                    }
                };

                // Kayıtlı okul seçimini geri yükle
                var savedOkulKodu = await SecureStorage.Default.GetAsync("SavedOkulKodu");
                if (!string.IsNullOrEmpty(savedOkulKodu))
                {
                    var idx = _okullar.FindIndex(o => o.OkulKodu == savedOkulKodu);
                    if (idx >= 0) PckOkul.SelectedIndex = idx;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OkullariYukle Hatası: {ex.Message}");
            }
        }

        private void OnPasswordToggleClicked(object sender, EventArgs e)
        {
            try
            {
                TxtPassword.IsPassword = !TxtPassword.IsPassword;
                BtnPasswordToggle.Source = TxtPassword.IsPassword ? "eye_off.png" : "eye_on.png";
            }
            catch { /**/ }
        }

        private async void BtnLogin_Clicked(object sender, EventArgs e)
        {
            try
            {
                if (PckOkul.SelectedIndex < 0)
                {
                    await DisplayAlert("Uyarı", "Lütfen okul seçiniz.", "Tamam");
                    return;
                }

                string username = TxtUsername.Text?.Trim();
                string password = TxtPassword.Text?.Trim();

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    await DisplayAlert("Uyarı", "Lütfen kullanıcı adı ve şifre giriniz.", "Tamam");
                    return;
                }

                var secilenOkul = _okullar[PckOkul.SelectedIndex];

                // Seçilen okulun API sunucu adresini aktif yap
                Preferences.Default.Set("AktifOkulApiUrl", secilenOkul.ApiUrl);

                BtnLogin.IsEnabled = false;
                BtnLogin.Text = "Giriş Yapılıyor...";

                bool isSuccess = await _girisService.KullaniciGirisYapAsync(username, password, secilenOkul.OkulKodu);

                if (isSuccess)
                {
                    // Mobilde admin girişi desteklenmez
                    if (KullaniciOturum.Rol == OgrenciBilgiSistemi.Shared.Enums.KullaniciRolu.Admin)
                    {
                        await KullaniciOturum.OturumTemizleAsync();
                        await DisplayAlert("Uyarı", "Bu uygulama yönetici girişi desteklememektedir.", "Tamam");
                        return;
                    }

                    // "Beni Hatırla" bilgilerini SecureStorage'a kaydet
                    await ManageRememberMeAsync(username, password, secilenOkul.OkulKodu);

                    // Role göre yönlendirme
                    if (KullaniciOturum.VeliMi)
                        await Shell.Current.GoToAsync("///VeliAnaSayfaView");
                    else if (KullaniciOturum.ServisMi)
                        await Shell.Current.GoToAsync("///ServisEkraniView");
                    else
                        await Shell.Current.GoToAsync("///SinifListeView");
                }
                else
                {
                    await DisplayAlert("Hata", "Kullanıcı adı veya şifre hatalı. Lütfen tekrar deneyin.", "Tamam");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Bağlantı Hatası",
                    "Sunucuya erişilemedi. Lütfen internet bağlantınızı kontrol edin veya daha sonra tekrar deneyin.", "Tamam");
                System.Diagnostics.Debug.WriteLine($"Login Hatası: {ex.Message}");
            }
            finally
            {
                BtnLogin.IsEnabled = true;
                BtnLogin.Text = "Giriş Yap";
            }
        }

        /// <summary>
        /// "Beni Hatırla" bilgilerini SecureStorage ile şifreli olarak saklar.
        /// </summary>
        private async Task ManageRememberMeAsync(string user, string pass, string okulKodu)
        {
            try
            {
                // Okul seçimi her başarılı girişte kalıcı olarak hatırlanır
                await SecureStorage.Default.SetAsync("SavedOkulKodu", okulKodu);

                if (ChkRememberMe.IsChecked)
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RememberMe Hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// Kayıtlı kimlik bilgilerini SecureStorage'dan güvenli şekilde yükler.
        /// </summary>
        private async Task LoadSavedCredentialsAsync()
        {
            try
            {
                if (Preferences.Default.Get("IsRemembered", false))
                {
                    var savedUser = await SecureStorage.Default.GetAsync("SavedUsername");
                    var savedPass = await SecureStorage.Default.GetAsync("SavedPassword");

                    if (!string.IsNullOrEmpty(savedUser))
                        TxtUsername.Text = savedUser;
                    if (!string.IsNullOrEmpty(savedPass))
                        TxtPassword.Text = savedPass;

                    ChkRememberMe.IsChecked = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadCredentials Hatası: {ex.Message}");
            }
        }

        private void OnRememberMeLabelTapped(object sender, EventArgs e)
        {
            try { ChkRememberMe.IsChecked = !ChkRememberMe.IsChecked; }
            catch { /**/ }
        }

        private async void OnUsernameTextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (_oneridenSecildi)
                {
                    _oneridenSecildi = false;
                    return;
                }

                var metin = e.NewTextValue?.Trim();

                if (string.IsNullOrEmpty(metin) || metin.Length < 3)
                {
                    OneriKutusu.IsVisible = false;
                    OneriListesi.ItemsSource = null;
                    return;
                }

                // Okul seçilmemişse arama yapma
                if (PckOkul.SelectedIndex < 0)
                    return;

                var secilenOkul = _okullar[PckOkul.SelectedIndex];

                _aramaIptalToken?.Cancel();
                _aramaIptalToken = new CancellationTokenSource();
                var token = _aramaIptalToken.Token;

                await Task.Delay(300, token);

                if (token.IsCancellationRequested)
                    return;

                var sonuclar = await _girisService.KullaniciAdiAraAsync(metin, secilenOkul.OkulKodu);

                if (token.IsCancellationRequested)
                    return;

                if (sonuclar.Count > 0)
                {
                    OneriListesi.ItemsSource = sonuclar;
                    OneriKutusu.IsVisible = true;
                }
                else
                {
                    OneriKutusu.IsVisible = false;
                }
            }
            catch (TaskCanceledException)
            {
                // Debounce iptal — beklenen durum
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"KullaniciArama Hatası: {ex.Message}");
            }
        }

        private void OnOneriSecildi(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (e.CurrentSelection.FirstOrDefault() is string secilenKullaniciAdi)
                {
                    _oneridenSecildi = true;
                    TxtUsername.Text = secilenKullaniciAdi;
                    OneriKutusu.IsVisible = false;
                    OneriListesi.SelectedItem = null;
                    TxtPassword.Focus();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OneriSecim Hatası: {ex.Message}");
            }
        }

    }
}
