using CommunityToolkit.Mvvm.ComponentModel;
using OgrenciBilgiSistemi.Mobil.Models;

namespace OgrenciBilgiSistemi.Mobil.ViewModels
{
    public partial class BildirimGorunumModel : ObservableObject
    {
        public Bildirim Bildirim { get; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(OkunmadiMi))]
        [NotifyPropertyChangedFor(nameof(ArkaplanRenk))]
        [NotifyPropertyChangedFor(nameof(SolCizgiRenk))]
        private bool okundu;

        public BildirimGorunumModel(Bildirim bildirim)
        {
            Bildirim = bildirim;
            okundu = bildirim.Okundu;
        }

        public bool OkunmadiMi => !Okundu;
        // Okunmamış vurgusu: rolün soft zemini + rol rengi şerit; okunduysa nötr Cizgi (Colors.xaml değerleri)
        public Color ArkaplanRenk => Okundu ? Colors.White : Services.RolTema.SoftRenk;
        public Color SolCizgiRenk => Okundu ? Color.FromArgb("#E3E8EF") : Services.RolTema.VurguRenk;

        public void IsaretleOkundu()
        {
            Bildirim.Okundu = true;
            Okundu = true;
        }
    }
}
