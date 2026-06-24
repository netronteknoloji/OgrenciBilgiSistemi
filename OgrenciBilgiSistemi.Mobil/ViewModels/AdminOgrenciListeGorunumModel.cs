using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OgrenciBilgiSistemi.Mobil.Models;
using OgrenciBilgiSistemi.Mobil.Services;
using OgrenciBilgiSistemi.Mobil.Views;

namespace OgrenciBilgiSistemi.Mobil.ViewModels
{
    public partial class AdminOgrenciListeGorunumModel : ObservableObject
    {
        private readonly OgrenciService _ogrenciService;
        private List<Ogrenci> _tumOgrenciler = [];

        [ObservableProperty] private IReadOnlyList<Ogrenci> filtreliOgrenciler = [];
        [ObservableProperty] private string aramaMetni = string.Empty;
        [ObservableProperty] private string altBaslik = string.Empty;
        [ObservableProperty] private string bosDurum = "Yükleniyor...";

        public AdminOgrenciListeGorunumModel(OgrenciService ogrenciService)
        {
            _ogrenciService = ogrenciService;
        }

        partial void OnAramaMetniChanged(string value) => Filtrele(value);

        [RelayCommand]
        async Task YukleAsync()
        {
            try
            {
                _tumOgrenciler = await _ogrenciService.TumOgrencileriGetirAsync();
                Filtrele(AramaMetni);
                if (_tumOgrenciler.Count == 0)
                    BosDurum = "Kayıtlı öğrenci bulunamadı.";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AdminOgrenciListe Yükleme Hatası: {ex.Message}");
                BosDurum = "Veriler yüklenemedi.";
            }
        }

        [RelayCommand]
        async Task OgrenciSecAsync(Ogrenci ogrenci)
            => await Shell.Current.Navigation.PushAsync(new OgrenciDetayView(ogrenci.OgrenciId, _ogrenciService));

        private void Filtrele(string? arama)
        {
            var temiz = arama?.Trim() ?? string.Empty;
            IReadOnlyList<Ogrenci> sonuc = string.IsNullOrEmpty(temiz)
                ? _tumOgrenciler
                : _tumOgrenciler.Where(o => Eslesir(o, temiz)).ToList();
            FiltreliOgrenciler = sonuc;
            AltBaslik = string.IsNullOrEmpty(temiz)
                ? $"Toplam {_tumOgrenciler.Count} öğrenci"
                : $"{sonuc.Count} / {_tumOgrenciler.Count} öğrenci";
            BosDurum = _tumOgrenciler.Count == 0
                ? "Kayıtlı öğrenci bulunamadı."
                : sonuc.Count == 0 ? "Aramanızla eşleşen öğrenci yok." : "";
        }

        private static bool Eslesir(Ogrenci o, string arama)
        {
            if (!string.IsNullOrEmpty(o.OgrenciAdSoyad) &&
                o.OgrenciAdSoyad.Contains(arama, StringComparison.OrdinalIgnoreCase))
                return true;
            return o.OgrenciNo.ToString().Contains(arama, StringComparison.OrdinalIgnoreCase);
        }
    }
}
