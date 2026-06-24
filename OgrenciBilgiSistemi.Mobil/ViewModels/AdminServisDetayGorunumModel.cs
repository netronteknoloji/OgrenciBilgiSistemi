using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OgrenciBilgiSistemi.Mobil.Models;
using OgrenciBilgiSistemi.Mobil.Services;
using OgrenciBilgiSistemi.Mobil.Views;

namespace OgrenciBilgiSistemi.Mobil.ViewModels
{
    public partial class AdminServisDetayGorunumModel : ObservableObject
    {
        private readonly AdminService _adminService;
        private readonly OgrenciService _ogrenciService;
        private int _servisKullaniciId;

        [ObservableProperty] private string adSoyad = string.Empty;
        [ObservableProperty] private string plaka = "-";
        [ObservableProperty] private string telefon = "-";
        [ObservableProperty] private string ogrenciSayisiMetni = string.Empty;
        [ObservableProperty] private string ogrencilerBaslik = "Öğrenciler";
        [ObservableProperty] private IReadOnlyList<Ogrenci> ogrenciler = [];
        [ObservableProperty] private string durumYazisi = string.Empty;
        [ObservableProperty] private bool durumYazisiGorulur = false;

        public AdminServisDetayGorunumModel(AdminService adminService, OgrenciService ogrenciService)
        {
            _adminService = adminService;
            _ogrenciService = ogrenciService;
        }

        public void Initialize(ServisListeOgesi servis)
        {
            _servisKullaniciId = servis.KullaniciId;
            AdSoyad = servis.KullaniciAdi;
            Plaka = servis.PlakaGosterim;
            Telefon = servis.TelefonGosterim;
            OgrenciSayisiMetni = $"{servis.OgrenciSayisi} öğrenci";
        }

        [RelayCommand]
        async Task YukleAsync()
        {
            try
            {
                var liste = await _adminService.ServisOgrencileriGetir(_servisKullaniciId);
                Ogrenciler = liste;
                OgrencilerBaslik = liste.Count > 0 ? $"Öğrenciler ({liste.Count})" : "Öğrenciler";
                DurumYazisi = liste.Count == 0 ? "Bu servise atanmış öğrenci yok." : "";
                DurumYazisiGorulur = liste.Count == 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AdminServisDetay Yükleme Hatası: {ex.Message}");
                DurumYazisi = "Öğrenci listesi yüklenemedi.";
                DurumYazisiGorulur = true;
            }
        }

        [RelayCommand]
        void TelefonAra()
        {
            try
            {
                if (PhoneDialer.Default.IsSupported && !string.IsNullOrEmpty(Telefon) && Telefon != "—")
                    PhoneDialer.Default.Open(Telefon);
            }
            catch { }
        }

        [RelayCommand]
        async Task OgrenciSecAsync(Ogrenci ogrenci)
            => await Shell.Current.Navigation.PushAsync(new OgrenciDetayView(ogrenci.OgrenciId, _ogrenciService));
    }
}
