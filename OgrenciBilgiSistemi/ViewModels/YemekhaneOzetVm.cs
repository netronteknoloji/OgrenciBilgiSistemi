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
        public string? Not { get; set; }
    }

    public class YemekhaneOzetVm
    {
        public int OgrenciId { get; set; }
        // Akademik Yıl BAŞLANGICI (2025 => 2025-2026)
        public int Yil { get; set; }

        public List<YemekhaneOzetAyVm> Aylar { get; set; } = new();
    }
}
