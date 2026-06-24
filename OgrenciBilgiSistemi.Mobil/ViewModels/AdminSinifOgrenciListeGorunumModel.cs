using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OgrenciBilgiSistemi.Mobil.Models;
using OgrenciBilgiSistemi.Mobil.Services;
using OgrenciBilgiSistemi.Mobil.Views;

namespace OgrenciBilgiSistemi.Mobil.ViewModels
{
    public partial class AdminSinifOgrenciListeGorunumModel : ObservableObject
    {
        private readonly OgrenciService _ogrenciService;
        private int _sinifId;

        [ObservableProperty] private string sinifAdi = string.Empty;
        [ObservableProperty] private string altBaslik = string.Empty;
        [ObservableProperty] private string bosDurum = "Yükleniyor...";
        [ObservableProperty] private IReadOnlyList<SinifYoklamaOzet> ogrenciler = [];
        [ObservableProperty] private DateTime secilenTarih = DateTime.Today;

        public AdminSinifOgrenciListeGorunumModel(OgrenciService ogrenciService)
        {
            _ogrenciService = ogrenciService;
        }

        public void Initialize(int sinifId, string sinifAdi)
        {
            _sinifId = sinifId;
            SinifAdi = string.IsNullOrWhiteSpace(sinifAdi) ? "Sınıf" : sinifAdi;
        }

        partial void OnSecilenTarihChanged(DateTime value) => _ = YukleAsync();

        [RelayCommand]
        async Task YukleAsync()
        {
            try
            {
                BosDurum = "Yükleniyor...";
                Ogrenciler = [];
                var liste = await _ogrenciService.SinifYoklamaOzetiGetirAsync(_sinifId, SecilenTarih);
                Ogrenciler = liste;
                var yoklananSayi = liste.Count(o => o.KullaniciId.HasValue);
                AltBaslik = liste.Count == 0
                    ? "Öğrenci bulunamadı"
                    : $"{liste.Count} öğrenci · {yoklananSayi} yoklama kaydı";
                BosDurum = liste.Count == 0 ? "Bu sınıfta kayıtlı öğrenci yok." : "";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AdminSinifYoklama Yükleme Hatası: {ex.Message}");
                BosDurum = "Veriler yüklenemedi.";
                AltBaslik = "";
            }
        }

        [RelayCommand]
        async Task OgrenciSecAsync(SinifYoklamaOzet item)
        {
            if (item.OgrenciId > 0)
                await Shell.Current.Navigation.PushAsync(new OgrenciDetayView(item.OgrenciId, _ogrenciService));
        }
    }
}
