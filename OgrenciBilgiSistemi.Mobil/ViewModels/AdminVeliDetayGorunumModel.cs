using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OgrenciBilgiSistemi.Mobil.Models;
using OgrenciBilgiSistemi.Mobil.Services;
using OgrenciBilgiSistemi.Mobil.Views;

namespace OgrenciBilgiSistemi.Mobil.ViewModels
{
    public partial class AdminVeliDetayGorunumModel : ObservableObject
    {
        private readonly VeliListeService _veliListeService;
        private readonly OgrenciService _ogrenciService;
        private int _veliId;

        [ObservableProperty] private string adSoyad = string.Empty;
        [ObservableProperty] private string yakinlik = string.Empty;
        [ObservableProperty] private string telefon = "-";
        [ObservableProperty] private string email = "-";
        [ObservableProperty] private string adres = "-";
        [ObservableProperty] private string meslek = "-";
        [ObservableProperty] private string isYeri = "-";
        [ObservableProperty] private string cocuklarBaslik = "Çocuklar";
        [ObservableProperty] private IReadOnlyList<VeliDetayOgrenci> cocuklar = [];
        [ObservableProperty] private string durumYazisi = string.Empty;

        public AdminVeliDetayGorunumModel(VeliListeService veliListeService, OgrenciService ogrenciService)
        {
            _veliListeService = veliListeService;
            _ogrenciService = ogrenciService;
        }

        public void Initialize(int veliId) => _veliId = veliId;

        [RelayCommand]
        async Task YukleAsync()
        {
            try
            {
                var detay = await _veliListeService.VeliDetayGetir(_veliId);
                if (detay is null)
                {
                    DurumYazisi = "Veli bilgileri yüklenemedi.";
                    return;
                }
                AdSoyad = detay.KullaniciAdi;
                Yakinlik = detay.YakinlikMetni;
                Telefon = string.IsNullOrWhiteSpace(detay.Telefon) ? "-" : detay.Telefon;
                Email = string.IsNullOrWhiteSpace(detay.VeliEmail) ? "-" : detay.VeliEmail;
                Adres = string.IsNullOrWhiteSpace(detay.VeliAdres) ? "-" : detay.VeliAdres;
                Meslek = string.IsNullOrWhiteSpace(detay.VeliMeslek) ? "-" : detay.VeliMeslek;
                IsYeri = string.IsNullOrWhiteSpace(detay.VeliIsYeri) ? "-" : detay.VeliIsYeri;
                CocuklarBaslik = detay.Cocuklar.Count > 0 ? $"Çocuklar ({detay.Cocuklar.Count})" : "Çocuklar";
                Cocuklar = detay.Cocuklar;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AdminVeliDetay Yükleme Hatası: {ex.Message}");
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

        [RelayCommand]
        async Task CocukSecAsync(VeliDetayOgrenci ogrenci)
            => await Shell.Current.Navigation.PushAsync(new OgrenciDetayView(ogrenci.OgrenciId, _ogrenciService));
    }
}
