using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OgrenciBilgiSistemi.Mobil.Services;
using OgrenciBilgiSistemi.Mobil.Views;

namespace OgrenciBilgiSistemi.Mobil.ViewModels
{
    public partial class AdminAnaSayfaGorunumModel : ObservableObject
    {
        private readonly AdminService _adminService;
        private readonly IServiceProvider _serviceProvider;

        [ObservableProperty] private string karsilamaMetni = string.Empty;
        [ObservableProperty] private string okulAdi = "Yükleniyor...";
        [ObservableProperty] private string ogrenciSayisi = "-";
        [ObservableProperty] private string ogretmenSayisi = "-";
        [ObservableProperty] private string sinifSayisi = "-";
        [ObservableProperty] private string veliSayisi = "-";
        [ObservableProperty] private string yemekhaneSayisi = "-";
        [ObservableProperty] private string anakapiSayisi = "-";
        [ObservableProperty] private string servisSayisi = "-";

        public AdminAnaSayfaGorunumModel(AdminService adminService, IServiceProvider serviceProvider)
        {
            _adminService = adminService;
            _serviceProvider = serviceProvider;
        }

        [RelayCommand]
        async Task YukleAsync()
        {
            KarsilamaMetni = $"Merhaba, {KullaniciOturum.AdSoyad}";
            try
            {
                var ozet = await _adminService.OkulOzetGetir();
                if (ozet is null)
                {
                    OkulAdi = "Veriler yüklenemedi";
                    ServisSayisi = "!";
                    return;
                }
                OkulAdi = string.IsNullOrWhiteSpace(ozet.OkulAdi) ? "Okul Özeti" : ozet.OkulAdi;
                OgrenciSayisi = ozet.ToplamOgrenci.ToString();
                OgretmenSayisi = ozet.ToplamOgretmen.ToString();
                SinifSayisi = ozet.ToplamSinif.ToString();
                VeliSayisi = ozet.ToplamVeli.ToString();
                ServisSayisi = ozet.ToplamServis.ToString();
                YemekhaneSayisi = ozet.BugunYemekhaneGiris.ToString();
                AnakapiSayisi = ozet.BugunAnakapiCikis.ToString();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AdminAnaSayfa Yükleme Hatası: {ex.Message}");
                OkulAdi = "Veriler yüklenemedi";
            }
        }

        [RelayCommand]
        async Task OgrencilereGitAsync()
        {
            var view = _serviceProvider.GetRequiredService<AdminOgrenciListeView>();
            await Shell.Current.Navigation.PushAsync(view);
        }

        [RelayCommand]
        async Task OgretmenlerGitAsync()
        {
            var view = _serviceProvider.GetRequiredService<AdminOgretmenListeView>();
            await Shell.Current.Navigation.PushAsync(view);
        }

        [RelayCommand]
        async Task SiniflaraGitAsync()
        {
            var view = _serviceProvider.GetRequiredService<AdminSinifListeView>();
            await Shell.Current.Navigation.PushAsync(view);
        }

        [RelayCommand]
        async Task VelilereGitAsync()
        {
            var view = _serviceProvider.GetRequiredService<AdminVeliListeView>();
            await Shell.Current.Navigation.PushAsync(view);
        }

        [RelayCommand]
        async Task YemakhaneGitAsync()
        {
            var view = _serviceProvider.GetRequiredService<AdminYemekhaneBugunView>();
            await Shell.Current.Navigation.PushAsync(view);
        }

        [RelayCommand]
        async Task AnakapiGitAsync()
        {
            var view = _serviceProvider.GetRequiredService<AdminAnakapiCikisBugunView>();
            await Shell.Current.Navigation.PushAsync(view);
        }

        [RelayCommand]
        async Task ServisGitAsync()
        {
            try
            {
                var view = _serviceProvider.GetRequiredService<AdminServisListeView>();
                await Shell.Current.Navigation.PushAsync(view);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AdminAnaSayfa] Servis listesi açılamadı: {ex}");
                await Shell.Current.DisplayAlert("Hata", $"Servis listesi açılamadı: {ex.Message}", "Tamam");
            }
        }
    }
}
