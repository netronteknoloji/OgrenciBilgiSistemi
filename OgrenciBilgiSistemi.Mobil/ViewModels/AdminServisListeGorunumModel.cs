using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OgrenciBilgiSistemi.Mobil.Models;
using OgrenciBilgiSistemi.Mobil.Services;
using OgrenciBilgiSistemi.Mobil.Views;

namespace OgrenciBilgiSistemi.Mobil.ViewModels
{
    public partial class AdminServisListeGorunumModel : ObservableObject
    {
        private readonly AdminService _adminService;
        private readonly IServiceProvider _serviceProvider;

        [ObservableProperty] private IReadOnlyList<ServisListeOgesi> servisler = [];
        [ObservableProperty] private string altBaslik = string.Empty;
        [ObservableProperty] private string bosDurum = "Yükleniyor...";

        public AdminServisListeGorunumModel(AdminService adminService, IServiceProvider serviceProvider)
        {
            _adminService = adminService;
            _serviceProvider = serviceProvider;
        }

        [RelayCommand]
        async Task YukleAsync()
        {
            try
            {
                var liste = await _adminService.ServisListesiGetir();
                if (liste is null)
                {
                    Servisler = [];
                    BosDurum = "Liste alınamadı. Sunucuya ulaşılamıyor olabilir.";
                    return;
                }
                Servisler = liste;
                AltBaslik = $"{liste.Count} servis";
                BosDurum = liste.Count == 0 ? "Kayıtlı servis bulunamadı." : "";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AdminServisListe Yükleme Hatası: {ex.Message}");
                BosDurum = "Veriler yüklenemedi.";
            }
        }

        [RelayCommand]
        async Task ServisSecAsync(ServisListeOgesi servis)
        {
            var vm = _serviceProvider.GetRequiredService<AdminServisDetayGorunumModel>();
            vm.Initialize(servis);
            await Shell.Current.Navigation.PushAsync(new AdminServisDetayView(vm));
        }
    }
}
