using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OgrenciBilgiSistemi.Mobil.Models;
using OgrenciBilgiSistemi.Mobil.Services;
using OgrenciBilgiSistemi.Mobil.Views;
using System.Collections.ObjectModel;

namespace OgrenciBilgiSistemi.Mobil.ViewModels
{
    public partial class OgrenciListeGorunumModel : ObservableObject
    {
        private readonly OgrenciService _ogrenciService;
        private readonly int _birimId;
        private List<OgrenciGorunumModel> _tumOgrenciler = [];

        public string BirimAd { get; }

        [ObservableProperty] private ObservableCollection<OgrenciGorunumModel> filtreliOgrenciler = [];
        [ObservableProperty] private string aramaMetni = string.Empty;
        [ObservableProperty] private int secilenPeriyotIndex = -1;
        [ObservableProperty] private bool statusUyariGorunur;
        [ObservableProperty] private bool kaydetGorunur = true;
        [ObservableProperty] private bool guncelleGorunur;

        public OgrenciListeGorunumModel(int birimId, string birimAd, OgrenciService ogrenciService)
        {
            _birimId = birimId;
            BirimAd = birimAd;
            _ogrenciService = ogrenciService;
        }

        partial void OnAramaMetniChanged(string value) => Filtrele(value);

        partial void OnSecilenPeriyotIndexChanged(int value) { _ = PeriyotDegistirAsync(value); }

        private void Filtrele(string aramaMetni)
        {
            if (string.IsNullOrEmpty(aramaMetni))
            {
                FiltreliOgrenciler = new ObservableCollection<OgrenciGorunumModel>(_tumOgrenciler);
                return;
            }
            var lower = aramaMetni.ToLower();
            FiltreliOgrenciler = new ObservableCollection<OgrenciGorunumModel>(_tumOgrenciler.Where(vm =>
                (vm.OgrenciData.OgrenciAdSoyad?.ToLower().Contains(lower) == true) ||
                vm.OgrenciData.OgrenciNo.ToString().Contains(aramaMetni)));
        }

        [RelayCommand]
        async Task YukleAsync()
        {
            try
            {
                var students = await _ogrenciService.SinifaGoreOgrencileriGetirAsync(_birimId);
                if (students == null) return;
                _tumOgrenciler = students.Select(s => new OgrenciGorunumModel { OgrenciData = s, SecilenDurumId = 0 }).ToList();
                FiltreliOgrenciler = new ObservableCollection<OgrenciGorunumModel>(_tumOgrenciler);
            }
            catch (Exception ex)
            {
                await Application.Current!.MainPage!.DisplayAlert("Hata", $"Öğrenci listesi yüklenemedi:\n{ex.Message}", "Tamam");
            }
        }

        private async Task PeriyotDegistirAsync(int index)
        {
            if (index == -1) return;
            try
            {
                int lessonNumber = index + 1;
                var existing = await _ogrenciService.MevcutYoklamaGetirAsync(_birimId, lessonNumber);
                bool hasData = existing != null && existing.Count > 0;
                StatusUyariGorunur = hasData;
                KaydetGorunur = !hasData;
                GuncelleGorunur = hasData;
                foreach (var vm in _tumOgrenciler)
                    vm.SecilenDurumId = hasData && existing!.TryGetValue(vm.OgrenciData.OgrenciId, out int s) ? s : 1;
            }
            catch { }
        }

        [RelayCommand]
        async Task KaydetAsync() => await YoklamaIsleAsync(guncelleme: false);

        [RelayCommand]
        async Task GuncelleAsync()
        {
            bool onay = await Application.Current!.MainPage!.DisplayAlert(
                "Onay", "Mevcut yoklama kaydını değiştirmek istediğinize emin misiniz?", "Evet", "Hayır");
            if (onay) await YoklamaIsleAsync(guncelleme: true);
        }

        private async Task YoklamaIsleAsync(bool guncelleme)
        {
            if (SecilenPeriyotIndex == -1)
            {
                await Application.Current!.MainPage!.DisplayAlert("Uyarı", "Lütfen önce ders saatini seçiniz!", "Tamam");
                return;
            }
            try
            {
                int lessonNumber = SecilenPeriyotIndex + 1;
                var attendanceData = _tumOgrenciler.Select(vm => (vm.OgrenciData.OgrenciId, vm.SecilenDurumId)).ToList();
                await _ogrenciService.TopluYoklamaKaydetAsync(attendanceData, _birimId, lessonNumber);
                string msg = guncelleme ? "Yoklama güncellendi." : "Yoklama başarıyla kaydedildi.";
                await Application.Current!.MainPage!.DisplayAlert("Bilgi", msg, "Tamam");
                await Shell.Current.Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                await Application.Current!.MainPage!.DisplayAlert("Hata", $"İşlem sırasında sorun çıktı: {ex.Message}", "Tamam");
            }
        }

        [RelayCommand]
        async Task OgrenciDetayAsync(OgrenciGorunumModel vm)
        {
            if (vm?.OgrenciData != null)
                await Shell.Current.Navigation.PushAsync(new OgrenciDetayView(vm.OgrenciData.OgrenciId));
        }
    }
}
