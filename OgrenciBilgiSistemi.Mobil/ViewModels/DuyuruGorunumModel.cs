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
        // Okunmamış vurgusu: VeliSoft zemin + VeliRenk şerit (Veli duyuru ekranının kimliği);
        // okunduysa nötr Cizgi (Colors.xaml değerleri)
        public Color ArkaplanRenk => Okundu ? Colors.White : Color.FromArgb("#F0EDFB");
        public Color SolCizgiRenk => Okundu ? Color.FromArgb("#E3E8EF") : Color.FromArgb("#6E56CF");

        public void IsaretleOkundu()
        {
            Duyuru.Okundu = true;
            Okundu = true;
        }
    }
}
