#region Kütüphane Referanslarý
using StudentTrackingSystem.Services;
using StudentTrackingSystem.Models;
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
#endregion

namespace StudentTrackingSystem.Views
{
    #region Giriþ Ekraný Görünüm Mantýðý
    public partial class LoginView : ContentPage
    {
        #region Özel Deðiþkenler
        private readonly LoginService _loginService;
        #endregion

        #region Yapýcý Metot ve Hazýrlýk
        public LoginView()
        {
            try
            {
                InitializeComponent();
                // LoginService artýk BaseApiService'den miras alýr ve HttpClient kullanýr.
                _loginService = new LoginService();
                LoadSavedCredentials();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoginView Init Hatasý: {ex.Message}");
            }
        }
        #endregion

        #region Þifre Görünürlük Yönetimi
        private void OnPasswordToggleClicked(object sender, EventArgs e)
        {
            try
            {
                TxtPassword.IsPassword = !TxtPassword.IsPassword;
                // Ýkon isimlerinin projenizdeki Resources/Images klasörüyle eþleþtiðinden emin olun.
                BtnPasswordToggle.Source = TxtPassword.IsPassword ? "eye_off.png" : "eye_on.png";
            }
            catch { /**/ }
        }
        #endregion

        #region Giriþ Ýþlemi ve Doðrulama
        private async void BtnLogin_Clicked(object sender, EventArgs e)
        {
            try
            {
                // 1. Giriþ Kontrolleri
                string username = TxtUsername.Text?.Trim();
                string password = TxtPassword.Text?.Trim();

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    await DisplayAlert("Uyarý", "Lütfen kullanýcý adý ve þifre giriniz.", "Tamam");
                    return;
                }

                // 2. Görsel Geri Bildirim
                BtnLogin.IsEnabled = false;
                BtnLogin.Text = "Giriþ Yapýlýyor...";

                // Varsa LoadingIndicator (ActivityIndicator) baþlatýlabilir.
                // LoadingIndicator.IsRunning = true;

                // 3. API Servisini Çaðýrma
                // Arka planda API'ye POST isteði atýlýr ve UserSession doldurulur.
                bool isSuccess = await _loginService.LoginAsUserAsync(username, password);

                if (isSuccess)
                {
                    // "Beni Hatýrla" tercihini kaydet
                    ManageRememberMe(username, password);

                    // 4. Baþarýlý Giriþ: ClassListView sayfasýna yönlendir.
                    // AppShell.xaml içinde ClassListView tanýmlý olmalýdýr.
                    await Shell.Current.GoToAsync("///ClassListView");
                }
                else
                {
                    await DisplayAlert("Hata", "Kullanýcý adý veya þifre hatalý. Lütfen tekrar deneyin.", "Tamam");
                }
            }
            catch (Exception ex)
            {
                // API eriþilemez durumdaysa veya internet yoksa burasý tetiklenir.
                await DisplayAlert("Baðlantý Hatasý",
                    "Sunucuya eriþilemedi. Lütfen internet baðlantýnýzý kontrol edin veya daha sonra tekrar deneyin.", "Tamam");
                System.Diagnostics.Debug.WriteLine($"Login Hatasý: {ex.Message}");
            }
            finally
            {
                // 5. Bileþenleri eski haline getir
                BtnLogin.IsEnabled = true;
                BtnLogin.Text = "Giriþ Yap";
                // LoadingIndicator.IsRunning = false;
            }
        }
        #endregion

        #region Yerel Hafýza (Beni Hatýrla) Ýþlemleri
        private void ManageRememberMe(string user, string pass)
        {
            try
            {
                if (ChkRememberMe.IsChecked)
                {
                    Preferences.Default.Set("SavedUsername", user);
                    Preferences.Default.Set("SavedPassword", pass);
                    Preferences.Default.Set("IsRemembered", true);
                }
                else
                {
                    Preferences.Default.Remove("SavedUsername");
                    Preferences.Default.Remove("SavedPassword");
                    Preferences.Default.Set("IsRemembered", false);
                }
            }
            catch { /**/ }
        }

        private void LoadSavedCredentials()
        {
            try
            {
                if (Preferences.Default.Get("IsRemembered", false))
                {
                    TxtUsername.Text = Preferences.Default.Get("SavedUsername", "");
                    TxtPassword.Text = Preferences.Default.Get("SavedPassword", "");
                    ChkRememberMe.IsChecked = true;
                }
            }
            catch { /**/ }
        }

        private void OnRememberMeLabelTapped(object sender, EventArgs e)
        {
            try { ChkRememberMe.IsChecked = !ChkRememberMe.IsChecked; }
            catch { /**/ }
        }
        #endregion
    }
    #endregion
}