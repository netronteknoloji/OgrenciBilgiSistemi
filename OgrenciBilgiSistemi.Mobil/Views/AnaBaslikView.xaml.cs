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
            "Şifre Değiştir",
            "Çıkış Yap");

        if (action == "Şifre Değiştir")
        {
            await SifreDegistirAsync();
        }
        else if (action == "Çıkış Yap")
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

    private async Task SifreDegistirAsync()
    {
        var page = Shell.Current.CurrentPage;

        string yeniSifre = await page.DisplayPromptAsync(
            "Şifre Değiştir",
            "Yeni şifrenizi giriniz (en az 3 karakter):",
            "Değiştir",
            "Vazgeç",
            maxLength: 50);

        if (string.IsNullOrWhiteSpace(yeniSifre))
            return;

        if (yeniSifre.Length < 3)
        {
            await page.DisplayAlert("Uyarı", "Şifre en az 3 karakter olmalıdır.", "Tamam");
            return;
        }

        try
        {
            var girisService = IPlatformApplication.Current?.Services.GetService<GirisService>();
            if (girisService == null) return;
            bool sonuc = await girisService.SifreDegistirAsync(yeniSifre);

            if (sonuc)
                await page.DisplayAlert("Bilgi", "Şifreniz başarıyla değiştirildi.", "Tamam");
            else
                await page.DisplayAlert("Hata", "Şifre değiştirilemedi. Lütfen tekrar deneyiniz.", "Tamam");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ŞİFRE DEĞİŞTİRME HATASI]: {ex.Message}");
            await page.DisplayAlert("Hata", "Şifre değiştirilirken bir sorun oluştu.", "Tamam");
        }
    }

    /// <summary>
    /// Oturum bilgilerini SecureStorage'dan temizler ve giriş ekranına yönlendirir.
    /// </summary>
    private async Task PerformLogoutAsync()
    {
        // Sadece oturum token/bilgilerini temizle.
        // "Beni Hatırla" kapsamındaki kayıtlı kimlik bilgileri korunur — kullanıcı
        // tekrar giriş ekranına geldiğinde alanların dolu gelmesi beklenir.
        await KullaniciOturum.OturumTemizleAsync();

        // Giriş ekranına yönlendir (tüm navigasyon geçmişi temizlenir)
        await Shell.Current.GoToAsync("//GirisView");
    }
}
