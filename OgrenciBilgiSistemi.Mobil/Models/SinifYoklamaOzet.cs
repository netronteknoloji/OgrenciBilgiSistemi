using Microsoft.Maui.Graphics;
using OgrenciBilgiSistemi.Shared.Constants;

namespace OgrenciBilgiSistemi.Mobil.Models
{
    public class SinifYoklamaOzet
    {
        public int OgrenciId { get; set; }
        public string OgrenciAdSoyad { get; set; } = string.Empty;
        public int OgrenciNo { get; set; }

        public int? Ders1 { get; set; }
        public int? Ders2 { get; set; }
        public int? Ders3 { get; set; }
        public int? Ders4 { get; set; }
        public int? Ders5 { get; set; }
        public int? Ders6 { get; set; }
        public int? Ders7 { get; set; }
        public int? Ders8 { get; set; }

        public int? KullaniciId { get; set; }
        public string? KullaniciAdi { get; set; }

        // UI binding'leri için her ders saatinin arka plan rengi (XAML Border BackgroundColor için)
        public Color Ders1Renk => DurumRengi(Ders1);
        public Color Ders2Renk => DurumRengi(Ders2);
        public Color Ders3Renk => DurumRengi(Ders3);
        public Color Ders4Renk => DurumRengi(Ders4);
        public Color Ders5Renk => DurumRengi(Ders5);
        public Color Ders6Renk => DurumRengi(Ders6);
        public Color Ders7Renk => DurumRengi(Ders7);
        public Color Ders8Renk => DurumRengi(Ders8);

        public string OgretmenSatiri =>
            string.IsNullOrWhiteSpace(KullaniciAdi) ? "Yoklama alınmadı" : $"Yoklayan: {KullaniciAdi}";

        // YoklamaDurumu enum (Shared): 1=Geldi, 2=Gelmedi, 3=GecGeldi, 4=Izinli,
        // 5=Raporlu, 6=Nobetci, 7=Gorevli. NULL = yoklama hiç alınmamış.
        // Renkler Shared YoklamaRenkleri'nden gelir — öğrenci detay matrisi ve web
        // ile aynı palet (AdminSinifOgrenciListeView lejantı da bu palete bakar).
        private static Color DurumRengi(int? durum) =>
            Color.FromArgb(YoklamaRenkleri.HexGetir(durum ?? 0));
    }
}
