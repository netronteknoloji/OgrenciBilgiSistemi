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

        // Tasarım sistemi semantik renkleri: Basari / Tehlike / MetinSoluk
        public Color DurumRenk => ServisDurumId switch
        {
            1 => Color.FromArgb("#2F9E44"),
            2 => Color.FromArgb("#E5484D"),
            _ => Color.FromArgb("#8A96A8")
        };

        public string DurumMetin => ServisDurumId switch
        {
            1 => "Araca Bindi",
            2 => "Araca Binmedi",
            _ => "Bekliyor..."
        };
    }
}
