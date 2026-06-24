using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OgrenciBilgiSistemi.Mobil.Models;
using OgrenciBilgiSistemi.Mobil.Services;
using OgrenciBilgiSistemi.Mobil.Views;

namespace OgrenciBilgiSistemi.Mobil.ViewModels
{
    public partial class AdminOgretmenListeGorunumModel : ObservableObject
    {
        private readonly OgretmenListeService _ogretmenListeService;
        private readonly IServiceProvider _serviceProvider;

        [ObservableProperty] private IReadOnlyList<OgretmenBilgi> ogretmenler = [];
        [ObservableProperty] private string altBaslik = string.Empty;
        [ObservableProperty] private string bosDurum = "Yükleniyor...";

        public AdminOgretmenListeGorunumModel(OgretmenListeService ogretmenListeService, IServiceProvider serviceProvider)
        {
            _ogretmenListeService = ogretmenListeService;
            _serviceProvider = serviceProvider;
        }

        [RelayCommand]
        async Task YukleAsync()
        {
            try
            {
                var liste = await _ogretmenListeService.AktifOgretmenleriGetir();
                Ogretmenler = liste;
                AltBaslik = $"Toplam {liste.Count} öğretmen";
                BosDurum = liste.Count == 0 ? "Kayıtlı öğretmen bulunamadı." : "";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AdminOgretmenListe Yükleme Hatası: {ex.Message}");
                BosDurum = "Veriler yüklenemedi.";
            }
        }

        [RelayCommand]
        async Task OgretmenSecAsync(OgretmenBilgi ogretmen)
        {
            var vm = _serviceProvider.GetRequiredService<AdminOgretmenDetayGorunumModel>();
            vm.Initialize(ogretmen.KullaniciId);
            await Shell.Current.Navigation.PushAsync(new AdminOgretmenDetayView(vm));
        }
    }
}
