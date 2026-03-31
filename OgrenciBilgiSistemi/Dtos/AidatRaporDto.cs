namespace OgrenciBilgiSistemi.Dtos
{
    public class AidatRaporDto
    {
        public int OgrenciId { get; set; }
        public int? Yil { get; set; }
        public string OgrenciAdSoyad { get; set; } = "";
        public string? OgrenciNo { get; set; }
        public string? OgrenciSinif { get; set; }

        public decimal BorcGosterim { get; set; }
        public decimal GosterilenOdenen { get; set; }
        public decimal Kalan { get; set; }

        public bool Muaf { get; set; }
        public bool Kapandi { get; set; }
    }
}
