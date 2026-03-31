namespace OgrenciBilgiSistemi.Dtos
{
    public sealed class OgrenciAidatDto
    {
        public int Yil { get; set; }
        public int OgrenciId { get; set; }
        public string OgrenciAdSoyad { get; set; } = string.Empty;

        public decimal ToplamBorc { get; set; }
        public decimal ToplamOdenen { get; set; }
        public bool Muaf { get; set; }
        public DateTime? SonOdemeTarihi { get; set; }

        public List<OdemeSatiriDto> Odemeler { get; set; } = new();
        public TarifeDto? Tarife { get; set; }

        // Türetilmiş convenience alanlar isterseniz:
        public decimal Kalan => Muaf ? 0m : Math.Max(0m, ToplamBorc - ToplamOdenen);
        public bool Kapandi => Muaf || Kalan == 0m;
    }
}
