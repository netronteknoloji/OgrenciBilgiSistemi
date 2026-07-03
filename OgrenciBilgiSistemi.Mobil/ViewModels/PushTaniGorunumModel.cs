using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OgrenciBilgiSistemi.Mobil.Services;

namespace OgrenciBilgiSistemi.Mobil.ViewModels
{
    /// <summary>
    /// Push bildirim tanı ekranı: kayıt zincirinin teşhis günlüğünü gösterir,
    /// kaydı manuel yeniden tetikler ve günlüğü panoya kopyalar.
    /// </summary>
    public partial class PushTaniGorunumModel : ObservableObject
    {
        private readonly PushKayitServisi _pushKayitServisi;

        [ObservableProperty] private string taniMetni = string.Empty;
        [ObservableProperty] private string surumBilgisi = string.Empty;
        [ObservableProperty] private string girisDurumu = string.Empty;
        [ObservableProperty] private bool mesgul;

        public PushTaniGorunumModel(PushKayitServisi pushKayitServisi)
        {
            _pushKayitServisi = pushKayitServisi;
        }

        [RelayCommand]
        void Yenile()
        {
            SurumBilgisi = $"Sürüm {AppInfo.Current.VersionString} (build {AppInfo.Current.BuildString}) — {DeviceInfo.Current.Platform}";
            GirisDurumu = KullaniciOturum.GirisYapildiMi
                ? $"Oturum açık: {KullaniciOturum.AdSoyad}"
                : "Oturum kapalı";

            var gunluk = PushTaniGunlugu.Oku();
            TaniMetni = string.IsNullOrWhiteSpace(gunluk)
                ? "Henüz teşhis kaydı yok. \"Yeniden Kaydet\" ile kayıt akışını tetikleyebilirsiniz."
                : gunluk;
        }

        [RelayCommand]
        async Task YenidenKaydetAsync()
        {
            if (Mesgul) return;
            Mesgul = true;
            try
            {
                PushTaniGunlugu.Ekle("— Manuel yeniden kayıt tetiklendi —");
                await _pushKayitServisi.LoginSonrasiKaydetAsync();
            }
            finally
            {
                Mesgul = false;
                Yenile();
            }
        }

        [RelayCommand]
        async Task KopyalaAsync()
        {
            var metin = $"{SurumBilgisi}\n{GirisDurumu}\n---\n{TaniMetni}";
            await Clipboard.Default.SetTextAsync(metin);
            var sayfa = Shell.Current?.CurrentPage;
            if (sayfa != null)
                await sayfa.DisplayAlert("Bilgi", "Teşhis günlüğü panoya kopyalandı.", "Tamam");
        }
    }
}
