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
        private List<SinifGorunumModel> _tumSiniflar = [];

        [ObservableProperty] private IReadOnlyList<SinifGorunumModel> filtreliSiniflar = [];
        [ObservableProperty] private string aramaMetni = string.Empty;
        [ObservableProperty] private string altBaslik = string.Empty;
        [ObservableProperty] private string bosDurum = "Yükleniyor...";

        public AdminSinifListeGorunumModel(SinifService sinifService, IServiceProvider serviceProvider)
        {
            _sinifService = sinifService;
            _serviceProvider = serviceProvider;
        }

        partial void OnAramaMetniChanged(string value) => Filtrele(value);

        [RelayCommand]
        async Task YukleAsync()
        {
            try
            {
                _tumSiniflar = await _sinifService.TumSiniflariOgrenciSayisiIleGetirAsync();
                Filtrele(AramaMetni);
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

        private void Filtrele(string? arama)
        {
            var temiz = arama?.Trim() ?? string.Empty;
            IReadOnlyList<SinifGorunumModel> sonuc = string.IsNullOrEmpty(temiz)
                ? _tumSiniflar
                : _tumSiniflar.Where(s => s.Ad.Contains(temiz, StringComparison.OrdinalIgnoreCase)).ToList();
            FiltreliSiniflar = sonuc;
            AltBaslik = string.IsNullOrEmpty(temiz)
                ? $"Toplam {_tumSiniflar.Count} sınıf"
                : $"{sonuc.Count} / {_tumSiniflar.Count} sınıf";
            BosDurum = _tumSiniflar.Count == 0
                ? "Kayıtlı sınıf bulunamadı."
                : sonuc.Count == 0 ? "Aramanızla eşleşen sınıf yok." : "";
        }
    }
}
