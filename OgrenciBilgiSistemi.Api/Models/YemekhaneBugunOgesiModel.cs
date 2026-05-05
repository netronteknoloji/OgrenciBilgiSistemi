namespace OgrenciBilgiSistemi.Api.Models
{
    public sealed class YemekhaneBugunOgesiModel
    {
        public int OgrenciId { get; set; }
        public string OgrenciAdSoyad { get; set; } = "";
        public int OgrenciNo { get; set; }
        public string? SinifAdi { get; set; }
        public string GirisSaati { get; set; } = "";
    }
}
