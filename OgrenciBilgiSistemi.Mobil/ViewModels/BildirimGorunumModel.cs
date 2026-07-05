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
        // Okunmamış vurgusu: MarkaSoft zemin + Marka şerit; okunduysa nötr Cizgi (Colors.xaml değerleri)
        public Color ArkaplanRenk => Okundu ? Colors.White : Color.FromArgb("#EEF1FE");
        public Color SolCizgiRenk => Okundu ? Color.FromArgb("#E3E8EF") : Color.FromArgb("#4C6EF5");

        public void IsaretleOkundu()
        {
            Bildirim.Okundu = true;
            Okundu = true;
        }
    }
}
