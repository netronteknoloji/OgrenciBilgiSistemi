using CommunityToolkit.Mvvm.ComponentModel;
using OgrenciBilgiSistemi.Mobil.Models;

namespace OgrenciBilgiSistemi.Mobil.ViewModels
{
    public partial class SinifGorunumModel : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Ad))]
        private Birim sinifVerisi = new();

        [ObservableProperty]
        private int ogrenciSayisi;

        public string Ad =>
            !string.IsNullOrEmpty(SinifVerisi?.BirimAd)
                ? SinifVerisi.BirimAd
                : "Tanımsız Sınıf";
    }
}
