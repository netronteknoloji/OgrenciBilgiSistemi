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
        private List<ServisListeOgesi> _tumServisler = [];

        [ObservableProperty] private IReadOnlyList<ServisListeOgesi> filtreliServisler = [];
        [ObservableProperty] private string aramaMetni = string.Empty;
        [ObservableProperty] private string altBaslik = string.Empty;
        [ObservableProperty] private string bosDurum = "Yükleniyor...";

        public AdminServisListeGorunumModel(AdminService adminService, IServiceProvider serviceProvider)
        {
            _adminService = adminService;
            _serviceProvider = serviceProvider;
        }

        partial void OnAramaMetniChanged(string value) => Filtrele(value);

        [RelayCommand]
        async Task YukleAsync()
        {
            try
            {
                var liste = await _adminService.ServisListesiGetir();
                if (liste is null)
                {
                    _tumServisler = [];
                    FiltreliServisler = [];
                    BosDurum = "Liste alınamadı. Sunucuya ulaşılamıyor olabilir.";
                    return;
                }
                _tumServisler = liste;
                Filtrele(AramaMetni);
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

        private void Filtrele(string? arama)
        {
            var temiz = arama?.Trim() ?? string.Empty;
            IReadOnlyList<ServisListeOgesi> sonuc = string.IsNullOrEmpty(temiz)
                ? _tumServisler
                : _tumServisler.Where(s => Eslesir(s, temiz)).ToList();
            FiltreliServisler = sonuc;
            AltBaslik = string.IsNullOrEmpty(temiz)
                ? $"Toplam {_tumServisler.Count} servis"
                : $"{sonuc.Count} / {_tumServisler.Count} servis";
            BosDurum = _tumServisler.Count == 0
                ? "Kayıtlı servis bulunamadı."
                : sonuc.Count == 0 ? "Aramanızla eşleşen servis yok." : "";
        }

        private static bool Eslesir(ServisListeOgesi s, string arama)
        {
            if (!string.IsNullOrEmpty(s.Plaka) &&
                s.Plaka.Contains(arama, StringComparison.OrdinalIgnoreCase))
                return true;
            return !string.IsNullOrEmpty(s.KullaniciAdi) &&
                   s.KullaniciAdi.Contains(arama, StringComparison.OrdinalIgnoreCase);
        }
    }
}
