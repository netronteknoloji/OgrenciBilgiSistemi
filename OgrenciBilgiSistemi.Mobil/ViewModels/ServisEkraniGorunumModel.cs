using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OgrenciBilgiSistemi.Mobil.Models;
using OgrenciBilgiSistemi.Mobil.Services;
using System.Collections.ObjectModel;

namespace OgrenciBilgiSistemi.Mobil.ViewModels
{
    public partial class ServisEkraniGorunumModel : ObservableObject
    {
        private readonly ServisService _servisService;
        private int? _servisId;
        private List<OgrenciGorunumModel> _tumOgrenciler = [];

        [ObservableProperty] private ObservableCollection<OgrenciGorunumModel> filtreliOgrenciler = [];
        [ObservableProperty] private string aramaMetni = string.Empty;
        [ObservableProperty] private string karsilamaMetni = "Merhaba";
        [ObservableProperty] private string plakaMetni = "Servis Öğrencileri";
        [ObservableProperty] private string ogrenciSayisiMetni = "Yükleniyor...";
        [ObservableProperty] private int secilenPeriyotIndex = -1;
        [ObservableProperty] private bool statusUyariGorunur;
        [ObservableProperty] private bool kaydetGorunur = true;
        [ObservableProperty] private bool guncelleGorunur;

        public ServisEkraniGorunumModel(ServisService servisService)
        {
            _servisService = servisService;
        }

        partial void OnAramaMetniChanged(string value) => Filtrele(value);

        partial void OnSecilenPeriyotIndexChanged(int value) { _ = PeriyotDegistirAsync(value); }

        private void Filtrele(string filtre)
        {
            if (string.IsNullOrEmpty(filtre))
                FiltreliOgrenciler = new ObservableCollection<OgrenciGorunumModel>(_tumOgrenciler);
            else
                FiltreliOgrenciler = new ObservableCollection<OgrenciGorunumModel>(
                    _tumOgrenciler.Where(o => o.OgrenciData?.OgrenciAdSoyad?.Contains(filtre, StringComparison.OrdinalIgnoreCase) == true));
        }

        [RelayCommand]
        async Task YukleAsync()
        {
            KarsilamaMetni = $"Merhaba, {KullaniciOturum.AdSoyad}";
            _servisId = KullaniciOturum.ServisId;
            if (!_servisId.HasValue)
            {
                OgrenciSayisiMetni = "Servis ataması bulunamadı";
                return;
            }
            try
            {
                var servis = await _servisService.ServisGetir(_servisId.Value);
                if (servis != null) PlakaMetni = $"Plaka: {servis.Plaka}";

                var ogrenciler = await _servisService.ServisOgrencileriGetir(_servisId.Value);
                _tumOgrenciler = ogrenciler.Select(o => new OgrenciGorunumModel { OgrenciData = o, ServisDurumId = 0 }).ToList();
                FiltreliOgrenciler = new ObservableCollection<OgrenciGorunumModel>(_tumOgrenciler);
                OgrenciSayisiMetni = $"{_tumOgrenciler.Count} öğrenci";
            }
            catch
            {
                OgrenciSayisiMetni = "Veriler yüklenemedi";
            }
        }

        private async Task PeriyotDegistirAsync(int index)
        {
            if (index == -1 || !_servisId.HasValue) return;
            try
            {
                int periyot = index + 1;
                var mevcut = await _servisService.MevcutServisYoklamaGetir(_servisId.Value, periyot);
                bool kayitVar = mevcut != null && mevcut.Count > 0;
                StatusUyariGorunur = kayitVar;
                KaydetGorunur = !kayitVar;
                GuncelleGorunur = kayitVar;
                foreach (var vm in _tumOgrenciler)
                    vm.ServisDurumId = kayitVar && mevcut!.TryGetValue(vm.OgrenciData.OgrenciId, out int d) ? d : 1;
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
                await Application.Current!.MainPage!.DisplayAlert("Uyarı", "Lütfen önce periyot seçiniz!", "Tamam");
                return;
            }
            if (!_servisId.HasValue)
            {
                await Application.Current!.MainPage!.DisplayAlert("Hata", "Servis bilgisi bulunamadı.", "Tamam");
                return;
            }
            var secilmemis = _tumOgrenciler.Where(o => o.ServisDurumId == 0).ToList();
            if (secilmemis.Count > 0)
            {
                await Application.Current!.MainPage!.DisplayAlert("Uyarı",
                    $"{secilmemis.Count} öğrenci için durum seçilmemiş. Lütfen tüm öğrenciler için Bindi veya Binmedi seçiniz.", "Tamam");
                return;
            }
            try
            {
                int periyot = SecilenPeriyotIndex + 1;
                var yoklamaVerisi = _tumOgrenciler.Select(vm => (vm.OgrenciData.OgrenciId, vm.ServisDurumId)).ToList();
                await _servisService.ServisYoklamaKaydet(yoklamaVerisi, periyot);
                string mesaj = guncelleme ? "Yoklama güncellendi." : "Yoklama başarıyla kaydedildi.";
                await Application.Current!.MainPage!.DisplayAlert("Bilgi", mesaj, "Tamam");
                StatusUyariGorunur = true;
                KaydetGorunur = false;
                GuncelleGorunur = true;
            }
            catch (Exception ex)
            {
                await Application.Current!.MainPage!.DisplayAlert("Hata", $"İşlem sırasında sorun çıktı: {ex.Message}", "Tamam");
            }
        }
    }
}
