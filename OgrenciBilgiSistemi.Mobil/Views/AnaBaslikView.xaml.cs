using OgrenciBilgiSistemi.Mobil.Services;
using System;

namespace OgrenciBilgiSistemi.Mobil.Views;

public partial class AnaBaslikView : ContentView
{
    #region Bindable Properties
    public static readonly BindableProperty ShowBackProperty =
        BindableProperty.Create(nameof(ShowBack), typeof(bool), typeof(AnaBaslikView), true);

    public static readonly BindableProperty ShowProfileProperty =
        BindableProperty.Create(nameof(ShowProfile), typeof(bool), typeof(AnaBaslikView), true);

    public static readonly BindableProperty UserInitialProperty =
        BindableProperty.Create(nameof(UserInitial), typeof(string), typeof(AnaBaslikView), string.Empty);

    public bool ShowBack { get => (bool)GetValue(ShowBackProperty); set => SetValue(ShowBackProperty, value); }
    public bool ShowProfile { get => (bool)GetValue(ShowProfileProperty); set => SetValue(ShowProfileProperty, value); }
    public string UserInitial { get => (string)GetValue(UserInitialProperty); set => SetValue(UserInitialProperty, value); }
    #endregion

    public AnaBaslikView()
    {
        InitializeComponent();
        UpdateInitial();
    }

    private void UpdateInitial()
    {
        if (string.IsNullOrEmpty(UserInitial))
        {
            var name = KullaniciOturum.AdSoyad;
            UserInitial = (!string.IsNullOrEmpty(name) && name != "Kullanıcı")
                          ? name.Trim().Substring(0, 1).ToUpper()
                          : "?";
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private async void OnProfileClicked(object sender, EventArgs e)
    {
        string action = await Shell.Current.CurrentPage.DisplayActionSheet(
            "Profil İşlemleri",
            "Vazgeç",
            null,
            "Çıkış Yap");

        if (action == "Çıkış Yap")
        {
            bool answer = await Shell.Current.CurrentPage.DisplayAlert(
                "Oturumu Kapat",
                "Uygulamadan çıkış yapmak istediğinize emin misiniz?",
                "Evet",
                "Hayır");

            if (answer)
            {
                await PerformLogoutAsync();
            }
        }
    }

    /// <summary>
    /// Oturum bilgilerini SecureStorage'dan temizler ve giriş ekranına yönlendirir.
    /// </summary>
    private async Task PerformLogoutAsync()
    {
        // Tüm oturum bilgilerini SecureStorage'dan ve bellekten temizle
        await KullaniciOturum.OturumTemizleAsync();

        // "Beni Hatırla" kapsamındaki kaydedilmiş kimlik bilgilerini de temizle
        SecureStorage.Default.Remove("SavedUsername");
        SecureStorage.Default.Remove("SavedPassword");
        Preferences.Default.Set("IsRemembered", false);

        // Giriş ekranına yönlendir (tüm navigasyon geçmişi temizlenir)
        await Shell.Current.GoToAsync("//GirisView");
    }
}
