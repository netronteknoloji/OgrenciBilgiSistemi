using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OgrenciBilgiSistemi.Mobil.Services;

namespace OgrenciBilgiSistemi.Mobil.ViewModels
{
    public partial class AdminOgretmenDetayGorunumModel : ObservableObject
    {
        private readonly OgretmenListeService _ogretmenListeService;
        private int _ogretmenId;

        [ObservableProperty] private string adSoyad = string.Empty;
        [ObservableProperty] private string birim = string.Empty;
        [ObservableProperty] private string telefon = "-";
        [ObservableProperty] private string email = "-";
        [ObservableProperty] private ImageSource gorselKaynak = ImageSource.FromFile("ogretmen_default.png");
        [ObservableProperty] private Color durumRenk = Color.FromArgb("#27AE60");
        [ObservableProperty] private string durumMetin = "Aktif";
        [ObservableProperty] private string durumYazisi = string.Empty;

        public AdminOgretmenDetayGorunumModel(OgretmenListeService ogretmenListeService)
        {
            _ogretmenListeService = ogretmenListeService;
        }

        public void Initialize(int ogretmenId) => _ogretmenId = ogretmenId;

        [RelayCommand]
        async Task YukleAsync()
        {
            try
            {
                var detay = await _ogretmenListeService.OgretmenDetayGetir(_ogretmenId);
                if (detay is null)
                {
                    DurumYazisi = "Öğretmen bilgileri yüklenemedi.";
                    return;
                }
                AdSoyad = detay.KullaniciAdi;
                Birim = string.IsNullOrWhiteSpace(detay.BirimAd) ? "Birim atanmamış" : detay.BirimAd;
                Telefon = string.IsNullOrWhiteSpace(detay.Telefon) ? "-" : detay.Telefon;
                Email = string.IsNullOrWhiteSpace(detay.Email) ? "-" : detay.Email;
                GorselKaynak = Constants.GorselUrl(detay.GorselPath);
                DurumRenk = detay.OgretmenDurum ? Color.FromArgb("#27AE60") : Color.FromArgb("#95A5A6");
                DurumMetin = detay.OgretmenDurum ? "Aktif" : "Pasif";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AdminOgretmenDetay Yükleme Hatası: {ex.Message}");
                DurumYazisi = "Veriler yüklenemedi.";
            }
        }

        [RelayCommand]
        void TelefonAra()
        {
            try
            {
                if (PhoneDialer.Default.IsSupported && !string.IsNullOrEmpty(Telefon) && Telefon != "-")
                    PhoneDialer.Default.Open(Telefon);
            }
            catch { }
        }
    }
}
