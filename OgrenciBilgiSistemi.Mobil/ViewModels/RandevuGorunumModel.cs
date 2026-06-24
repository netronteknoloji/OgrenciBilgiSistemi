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

        public Color DurumRenk => Randevu.Durum switch
        {
            0 => Color.FromArgb("#F39C12"),
            1 => Color.FromArgb("#27AE60"),
            2 => Color.FromArgb("#E74C3C"),
            3 => Color.FromArgb("#95A5A6"),
            4 => Color.FromArgb("#3498DB"),
            _ => Color.FromArgb("#95A5A6")
        };

        public Color DurumArkaplanRenk => Randevu.Durum switch
        {
            0 => Color.FromArgb("#FEF9E7"),
            1 => Color.FromArgb("#EAFAF1"),
            2 => Color.FromArgb("#FDEDEC"),
            3 => Color.FromArgb("#F2F3F4"),
            4 => Color.FromArgb("#EBF5FB"),
            _ => Color.FromArgb("#F2F3F4")
        };
    }
}
