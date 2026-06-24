using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OgrenciBilgiSistemi.Mobil.Services;
using System.Collections.ObjectModel;

namespace OgrenciBilgiSistemi.Mobil.ViewModels
{
    public partial class VeliDuyurularGorunumModel : ObservableObject
    {
        private readonly DuyuruService _duyuruService;

        [ObservableProperty] private ObservableCollection<DuyuruGorunumModel> duyurular = [];
        [ObservableProperty] private string altBaslik = string.Empty;

        public VeliDuyurularGorunumModel(DuyuruService duyuruService)
        {
            _duyuruService = duyuruService;
        }

        [RelayCommand]
        async Task YukleAsync()
        {
            try
            {
                Duyurular.Clear();
                var liste = await _duyuruService.BenimDuyurular();
                foreach (var d in liste)
                    Duyurular.Add(new DuyuruGorunumModel(d));
                AltBaslikGuncelle();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DUYURU LISTE HATASI]: {ex.Message}");
            }
        }

        [RelayCommand]
        async Task DuyuruTapAsync(DuyuruGorunumModel gorunum)
        {
            if (gorunum.Okundu) return;
            var basarili = await _duyuruService.OkunduIsaretle(gorunum.Duyuru.DuyuruId);
            if (!basarili) return;
            gorunum.IsaretleOkundu();
            AltBaslikGuncelle();
        }

        [RelayCommand]
        async Task TumunuOkunduAsync()
        {
            var sonuc = await _duyuruService.TumunuOkunduIsaretle();
            if (!sonuc) return;
            foreach (var d in Duyurular)
                d.IsaretleOkundu();
            AltBaslik = "Tüm duyurular okundu";
        }

        private void AltBaslikGuncelle()
        {
            var okunmamis = Duyurular.Count(d => d.OkunmadiMi);
            AltBaslik = okunmamis > 0
                ? $"{okunmamis} okunmamış duyuru"
                : "Tüm duyurular okundu";
        }
    }
}
