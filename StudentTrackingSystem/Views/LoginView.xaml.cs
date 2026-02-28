using StudentTrackingSystem.Services;
using StudentTrackingSystem.Models;
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace StudentTrackingSystem.Views
{
    public partial class LoginView : ContentPage
    {
        private readonly LoginService _loginService;

        public LoginView()
        {
            try
            {
                InitializeComponent();
                _loginService = new LoginService();
                _ = LoadSavedCredentialsAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoginView Init Hatası: {ex.Message}");
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
                string username = TxtUsername.Text?.Trim();
                string password = TxtPassword.Text?.Trim();

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    await DisplayAlert("Uyarı", "Lütfen kullanıcı adı ve şifre giriniz.", "Tamam");
                    return;
                }

                BtnLogin.IsEnabled = false;
                BtnLogin.Text = "Giriş Yapılıyor...";

                bool isSuccess = await _loginService.LoginAsUserAsync(username, password);

                if (isSuccess)
                {
                    // "Beni Hatırla" bilgilerini SecureStorage'a kaydet
                    await ManageRememberMeAsync(username, password);

                    await Shell.Current.GoToAsync("///ClassListView");
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
        /// Preferences yerine SecureStorage kullanılarak şifre güvenliği sağlanır.
        /// </summary>
        private async Task ManageRememberMeAsync(string user, string pass)
        {
            try
            {
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
    }
}
