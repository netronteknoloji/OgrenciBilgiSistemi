// ViewModels/YemekhaneOzetVm.cs

namespace OgrenciBilgiSistemi.ViewModels
{
    public class YemekhaneOzetAyVm
    {
        // Akademik sıra: 1=Eylül ... 12=Ağustos (UI için)
        public int AkAyIndex { get; set; }         // 1..12
        public string AyAd { get; set; } = "";     // "Eylül" vb.

        // Veritabanındaki gerçek değerler (takvim)
        public int YilGercek { get; set; }         // 2025 vb. (Eyl–Ara = Yil, Oca–Ağu = Yil+1)
        public int AyGercek { get; set; }          // 1..12

        public bool Aktif { get; set; }
        public decimal AylikTutar { get; set; }
        public decimal Borc { get; set; }
        public decimal Odenen { get; set; }
        public decimal Kalan => Math.Max(0, Borc - Odenen);
        public string? Not { get; set; }
    }

    public class YemekhaneOzetVm
    {
        public int OgrenciId { get; set; }
        // Akademik Yıl BAŞLANGICI (2025 => 2025-2026)
        public int Yil { get; set; }

        public decimal AylikTutar { get; set; }
        public decimal ToplamBorc { get; set; }
        public decimal ToplamOdenen { get; set; }
        public decimal ToplamKalan => Math.Max(0, ToplamBorc - ToplamOdenen);

        public List<YemekhaneOzetAyVm> Aylar { get; set; } = new();
    }
}
