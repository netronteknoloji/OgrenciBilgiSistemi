using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.Graphics;
using OgrenciBilgiSistemi.Mobil.Models;

namespace OgrenciBilgiSistemi.Mobil.ViewModels
{
    public partial class OgrenciGorunumModel : ObservableObject
    {
        [ObservableProperty]
        private Ogrenci ogrenciData = new();

        [ObservableProperty]
        private int secilenDurumId = 1;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DurumIkon))]
        [NotifyPropertyChangedFor(nameof(DurumRenk))]
        [NotifyPropertyChangedFor(nameof(DurumMetin))]
        private int servisDurumId;

        public string DurumIkon => ServisDurumId switch
        {
            1 => "✓",
            2 => "X",
            _ => "?"
        };

        public Color DurumRenk => ServisDurumId switch
        {
            1 => Color.FromArgb("#2ECC71"),
            2 => Color.FromArgb("#E74C3C"),
            _ => Color.FromArgb("#BDC3C7")
        };

        public string DurumMetin => ServisDurumId switch
        {
            1 => "Araca Bindi",
            2 => "Araca Binmedi",
            _ => "Bekliyor..."
        };
    }
}
