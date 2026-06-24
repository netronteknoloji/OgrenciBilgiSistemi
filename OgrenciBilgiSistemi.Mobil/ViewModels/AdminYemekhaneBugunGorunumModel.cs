using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OgrenciBilgiSistemi.Mobil.Models;
using OgrenciBilgiSistemi.Mobil.Services;
using OgrenciBilgiSistemi.Mobil.Views;

namespace OgrenciBilgiSistemi.Mobil.ViewModels
{
    public partial class AdminYemekhaneBugunGorunumModel : ObservableObject
    {
        private readonly AdminService _adminService;
        private readonly OgrenciService _ogrenciService;

        [ObservableProperty] private IReadOnlyList<YemekhaneBugunOgesi> ogeler = [];
        [ObservableProperty] private string altBaslik = string.Empty;
        [ObservableProperty] private string bosDurum = "Yükleniyor...";

        public AdminYemekhaneBugunGorunumModel(AdminService adminService, OgrenciService ogrenciService)
        {
            _adminService = adminService;
            _ogrenciService = ogrenciService;
        }

        [RelayCommand]
        async Task YukleAsync()
        {
            try
            {
                var liste = await _adminService.YemekhaneBugunGetir();
                if (liste is null)
                {
                    Ogeler = [];
                    BosDurum = "Liste alınamadı. Sunucuya ulaşılamıyor olabilir.";
                    return;
                }
                Ogeler = liste;
                AltBaslik = $"Bugün {liste.Count} öğrenci";
                BosDurum = liste.Count == 0 ? "Bugün yemekhaneye giriş yapan öğrenci yok." : "";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AdminYemekhaneBugun Yükleme Hatası: {ex.Message}");
                BosDurum = "Veriler yüklenemedi.";
            }
        }

        [RelayCommand]
        async Task OgrenciSecAsync(YemekhaneBugunOgesi oge)
            => await Shell.Current.Navigation.PushAsync(new OgrenciDetayView(oge.OgrenciId, _ogrenciService));
    }
}
