using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OgrenciBilgiSistemi.Mobil.Services;
using OgrenciBilgiSistemi.Mobil.Views;

namespace OgrenciBilgiSistemi.Mobil.ViewModels
{
    public partial class AdminSinifListeGorunumModel : ObservableObject
    {
        private readonly SinifService _sinifService;
        private readonly IServiceProvider _serviceProvider;

        [ObservableProperty] private IReadOnlyList<SinifGorunumModel> siniflar = [];
        [ObservableProperty] private string altBaslik = string.Empty;
        [ObservableProperty] private string bosDurum = "Yükleniyor...";

        public AdminSinifListeGorunumModel(SinifService sinifService, IServiceProvider serviceProvider)
        {
            _sinifService = sinifService;
            _serviceProvider = serviceProvider;
        }

        [RelayCommand]
        async Task YukleAsync()
        {
            try
            {
                var liste = await _sinifService.TumSiniflariOgrenciSayisiIleGetirAsync();
                Siniflar = liste;
                AltBaslik = $"Toplam {liste.Count} sınıf";
                BosDurum = liste.Count == 0 ? "Kayıtlı sınıf bulunamadı." : "";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AdminSinifListe Yükleme Hatası: {ex.Message}");
                BosDurum = "Veriler yüklenemedi.";
            }
        }

        [RelayCommand]
        async Task SinifSecAsync(SinifGorunumModel secilen)
        {
            try
            {
                var vm = _serviceProvider.GetRequiredService<AdminSinifOgrenciListeGorunumModel>();
                vm.Initialize(secilen.SinifVerisi.BirimId, secilen.Ad);
                await Shell.Current.Navigation.PushAsync(new AdminSinifOgrenciListeView(vm));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AdminSinifListe Sınıf Tıklama Hatası: {ex.Message}");
            }
        }
    }
}
