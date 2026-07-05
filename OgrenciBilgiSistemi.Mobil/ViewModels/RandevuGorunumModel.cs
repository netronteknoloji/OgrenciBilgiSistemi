using OgrenciBilgiSistemi.Mobil.Models;
using OgrenciBilgiSistemi.Mobil.Services;

namespace OgrenciBilgiSistemi.Mobil.ViewModels
{
    public class RandevuGorunumModel
    {
        public Randevu Randevu { get; }

        public RandevuGorunumModel(Randevu randevu) => Randevu = randevu;

        public string KarsiTarafAdi => KullaniciOturum.OgretmenMi
            ? Randevu.VeliAdSoyad
            : Randevu.OgretmenAdSoyad;

        public string? OgrenciAdSoyad => Randevu.OgrenciAdSoyad;
        public bool OgrenciGosterilsinMi => !string.IsNullOrEmpty(Randevu.OgrenciAdSoyad);
        public string TarihMetni => Randevu.RandevuTarihi.ToString("dd.MM.yyyy HH:mm");
        public string SureMetni => $"{Randevu.SureDakika} dk";
        public string DurumAdi => Randevu.DurumAdi;

        // Tasarım sistemi semantik renkleri: Uyari / Basari / Tehlike / MetinSoluk / Marka
        // (Resources/Styles/Colors.xaml ile aynı değerler)
        public Color DurumRenk => Randevu.Durum switch
        {
            0 => Color.FromArgb("#E8940C"), // Beklemede - Uyari
            1 => Color.FromArgb("#2F9E44"), // Onaylandı - Basari
            2 => Color.FromArgb("#E5484D"), // Reddedildi - Tehlike
            3 => Color.FromArgb("#8A96A8"), // İptal - MetinSoluk
            4 => Color.FromArgb("#4C6EF5"), // Tamamlandı - Marka
            _ => Color.FromArgb("#8A96A8")
        };

        public Color DurumArkaplanRenk => Randevu.Durum switch
        {
            0 => Color.FromArgb("#FCF3E3"),
            1 => Color.FromArgb("#E7F5EB"),
            2 => Color.FromArgb("#FCE9EA"),
            3 => Color.FromArgb("#EEF1F6"),
            4 => Color.FromArgb("#EEF1FE"),
            _ => Color.FromArgb("#EEF1F6")
        };
    }
}
