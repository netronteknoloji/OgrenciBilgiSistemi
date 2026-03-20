namespace OgrenciBilgiSistemi.Api.Models
{
    public class OgrenciModel
    {
        public int OgrenciId { get; set; }
        public string OgrenciAdSoyad { get; set; } = string.Empty;
        public int OgrenciNo { get; set; }
        public string? OgrenciGorsel { get; set; }
        public string? OgrenciKartNo { get; set; }
        public int OgrenciCikisDurumu { get; set; }
        public bool OgrenciDurum { get; set; }
        public int? BirimId { get; set; }
        public int? PersonelId { get; set; }
        public int? OgrenciVeliId { get; set; }
        public int? ServisId { get; set; }
        public string? SinifAdi { get; set; }
    }
}
