using CommunityToolkit.Mvvm.ComponentModel;
using OgrenciBilgiSistemi.Mobil.Models;

namespace OgrenciBilgiSistemi.Mobil.ViewModels
{
    public partial class DuyuruGorunumModel : ObservableObject
    {
        public Duyuru Duyuru { get; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(OkunmadiMi))]
        [NotifyPropertyChangedFor(nameof(ArkaplanRenk))]
        [NotifyPropertyChangedFor(nameof(SolCizgiRenk))]
        private bool okundu;

        public DuyuruGorunumModel(Duyuru duyuru)
        {
            Duyuru = duyuru;
            okundu = duyuru.Okundu;
        }

        public bool OkunmadiMi => !Okundu;
        public Color ArkaplanRenk => Okundu ? Colors.White : Color.FromArgb("#FFF8F0");
        public Color SolCizgiRenk => Okundu ? Color.FromArgb("#ECF0F1") : Color.FromArgb("#E67E22");

        public void IsaretleOkundu()
        {
            Duyuru.Okundu = true;
            Okundu = true;
        }
    }
}
