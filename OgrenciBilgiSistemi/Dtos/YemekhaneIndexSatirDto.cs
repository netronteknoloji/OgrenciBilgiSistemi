namespace OgrenciBilgiSistemi.Dtos
{
    public class YemekhaneIndexSatirDto
    {
        public int OgrenciId { get; set; }
        public int Yil { get; set; }
        public string OgrenciAdSoyad { get; set; } = "";
        public string? OgrenciNo { get; set; }
        public string? OgrenciSinif { get; set; }

        public decimal AylikTarife { get; set; }
        public int AktifAySayisi { get; set; }
        public decimal Borc { get; set; }
        public decimal Odenen { get; set; }
        public decimal Kalan { get; set; }

        public bool TarifeVarMi { get; set; }
        public bool BuAyAktif { get; set; }
    }

    public class YemekhaneIndexRaporSonucDto
    {
        public SayfalanmisListeModel<YemekhaneIndexSatirDto> Satirlar { get; set; } = null!;
        public decimal ToplamBorc { get; set; }
        public decimal ToplamOdenen { get; set; }
        public decimal ToplamKalan { get; set; }
        public List<int> KullanilabilirYillar { get; set; } = new();
    }
}
