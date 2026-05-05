namespace OgrenciBilgiSistemi.Mobil.Models
{
    public sealed class YemekhaneBugunOgesi
    {
        public int OgrenciId { get; set; }
        public string OgrenciAdSoyad { get; set; } = "";
        public int OgrenciNo { get; set; }
        public string? SinifAdi { get; set; }
        public string GirisSaati { get; set; } = "";

        public string SinifGosterim => string.IsNullOrWhiteSpace(SinifAdi) ? "—" : SinifAdi!;
    }
}
