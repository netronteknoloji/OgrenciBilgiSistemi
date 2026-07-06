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
        private List<OgretmenBilgi> _tumOgretmenler = [];

        [ObservableProperty] private IReadOnlyList<OgretmenBilgi> filtreliOgretmenler = [];
        [ObservableProperty] private string aramaMetni = string.Empty;
        [ObservableProperty] private string altBaslik = string.Empty;
        [ObservableProperty] private string bosDurum = "Yükleniyor...";

        public AdminOgretmenListeGorunumModel(OgretmenListeService ogretmenListeService, IServiceProvider serviceProvider)
        {
            _ogretmenListeService = ogretmenListeService;
            _serviceProvider = serviceProvider;
        }

        partial void OnAramaMetniChanged(string value) => Filtrele(value);

        [RelayCommand]
        async Task YukleAsync()
        {
            try
            {
                _tumOgretmenler = await _ogretmenListeService.AktifOgretmenleriGetir();
                Filtrele(AramaMetni);
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

        private void Filtrele(string? arama)
        {
            var temiz = arama?.Trim() ?? string.Empty;
            IReadOnlyList<OgretmenBilgi> sonuc = string.IsNullOrEmpty(temiz)
                ? _tumOgretmenler
                : _tumOgretmenler.Where(o => Eslesir(o, temiz)).ToList();
            FiltreliOgretmenler = sonuc;
            AltBaslik = string.IsNullOrEmpty(temiz)
                ? $"Toplam {_tumOgretmenler.Count} öğretmen"
                : $"{sonuc.Count} / {_tumOgretmenler.Count} öğretmen";
            BosDurum = _tumOgretmenler.Count == 0
                ? "Kayıtlı öğretmen bulunamadı."
                : sonuc.Count == 0 ? "Aramanızla eşleşen öğretmen yok." : "";
        }

        private static bool Eslesir(OgretmenBilgi o, string arama)
            => !string.IsNullOrEmpty(o.KullaniciAdi) &&
               o.KullaniciAdi.Contains(arama, StringComparison.OrdinalIgnoreCase);
    }
}
