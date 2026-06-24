using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OgrenciBilgiSistemi.Mobil.Models;
using OgrenciBilgiSistemi.Mobil.Services;
using OgrenciBilgiSistemi.Mobil.Views;

namespace OgrenciBilgiSistemi.Mobil.ViewModels
{
    public partial class AdminAnakapiCikisBugunGorunumModel : ObservableObject
    {
        private readonly AdminService _adminService;
        private readonly OgrenciService _ogrenciService;

        [ObservableProperty] private IReadOnlyList<AnakapiCikisBugunOgesi> ogeler = [];
        [ObservableProperty] private string altBaslik = string.Empty;
        [ObservableProperty] private string bosDurum = "Yükleniyor...";

        public AdminAnakapiCikisBugunGorunumModel(AdminService adminService, OgrenciService ogrenciService)
        {
            _adminService = adminService;
            _ogrenciService = ogrenciService;
        }

        [RelayCommand]
        async Task YukleAsync()
        {
            try
            {
                var liste = await _adminService.AnakapiCikisBugunGetir();
                if (liste is null)
                {
                    Ogeler = [];
                    BosDurum = "Liste alınamadı. Sunucuya ulaşılamıyor olabilir.";
                    return;
                }
                Ogeler = liste;
                AltBaslik = $"Bugün {liste.Count} öğrenci";
                BosDurum = liste.Count == 0 ? "Bugün ana kapıdan çıkış yapan öğrenci yok." : "";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AdminAnakapiCikisBugun Yükleme Hatası: {ex.Message}");
                BosDurum = "Veriler yüklenemedi.";
            }
        }

        [RelayCommand]
        async Task OgrenciSecAsync(AnakapiCikisBugunOgesi oge)
            => await Shell.Current.Navigation.PushAsync(new OgrenciDetayView(oge.OgrenciId, _ogrenciService));
    }
}
