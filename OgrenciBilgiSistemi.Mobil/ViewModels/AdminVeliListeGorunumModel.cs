using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OgrenciBilgiSistemi.Mobil.Models;
using OgrenciBilgiSistemi.Mobil.Services;
using OgrenciBilgiSistemi.Mobil.Views;

namespace OgrenciBilgiSistemi.Mobil.ViewModels
{
    public partial class AdminVeliListeGorunumModel : ObservableObject
    {
        private readonly VeliListeService _veliListeService;
        private readonly IServiceProvider _serviceProvider;
        private List<Veli> _tumVeliler = [];

        [ObservableProperty] private IReadOnlyList<Veli> filtreliVeliler = [];
        [ObservableProperty] private string aramaMetni = string.Empty;
        [ObservableProperty] private string altBaslik = string.Empty;
        [ObservableProperty] private string bosDurum = "Yükleniyor...";

        public AdminVeliListeGorunumModel(VeliListeService veliListeService, IServiceProvider serviceProvider)
        {
            _veliListeService = veliListeService;
            _serviceProvider = serviceProvider;
        }

        partial void OnAramaMetniChanged(string value) => Filtrele(value);

        [RelayCommand]
        async Task YukleAsync()
        {
            try
            {
                _tumVeliler = await _veliListeService.AktifVelileriGetir();
                Filtrele(AramaMetni);
                if (_tumVeliler.Count == 0)
                    BosDurum = "Kayıtlı veli bulunamadı.";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AdminVeliListe Yükleme Hatası: {ex.Message}");
                BosDurum = "Veriler yüklenemedi.";
            }
        }

        [RelayCommand]
        async Task VeliSecAsync(Veli veli)
        {
            var vm = _serviceProvider.GetRequiredService<AdminVeliDetayGorunumModel>();
            vm.Initialize(veli.KullaniciId);
            await Shell.Current.Navigation.PushAsync(new AdminVeliDetayView(vm));
        }

        private void Filtrele(string? arama)
        {
            var temiz = arama?.Trim() ?? string.Empty;
            IReadOnlyList<Veli> sonuc = string.IsNullOrEmpty(temiz)
                ? _tumVeliler
                : _tumVeliler
                    .Where(v => !string.IsNullOrEmpty(v.KullaniciAdi) &&
                                v.KullaniciAdi.Contains(temiz, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            FiltreliVeliler = sonuc;
            AltBaslik = string.IsNullOrEmpty(temiz)
                ? $"Toplam {_tumVeliler.Count} veli"
                : $"{sonuc.Count} / {_tumVeliler.Count} veli";
            BosDurum = _tumVeliler.Count == 0
                ? "Kayıtlı veli bulunamadı."
                : sonuc.Count == 0 ? "Aramanızla eşleşen veli yok." : "";
        }
    }
}
