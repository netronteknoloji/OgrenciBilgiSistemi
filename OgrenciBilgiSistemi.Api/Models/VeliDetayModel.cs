namespace OgrenciBilgiSistemi.Api.Models
{
    public class VeliDetayModel
    {
        public int KullaniciId { get; set; }
        public string KullaniciAdi { get; set; } = string.Empty;
        public string? Telefon { get; set; }
        public string? VeliEmail { get; set; }
        public string? VeliAdres { get; set; }
        public string? VeliMeslek { get; set; }
        public string? VeliIsYeri { get; set; }
        public int? VeliYakinlik { get; set; }
        public bool IsDeleted { get; set; }

        public List<VeliDetayOgrenciModel> Cocuklar { get; set; } = new();
    }

    public class VeliDetayOgrenciModel
    {
        public int OgrenciId { get; set; }
        public string OgrenciAdSoyad { get; set; } = string.Empty;
        public int OgrenciNo { get; set; }
        public string? BirimAd { get; set; }
    }
}
