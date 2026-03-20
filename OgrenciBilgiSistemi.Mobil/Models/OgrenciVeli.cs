namespace OgrenciBilgiSistemi.Mobil.Models
{
    public class OgrenciVeli
    {
        public int OgrenciVeliId { get; set; }
        public string? VeliAdSoyad { get; set; }
        public string? VeliTelefon { get; set; }
        public string? VeliAdres { get; set; }
        public string? VeliMeslek { get; set; }
        public string? VeliIsYeri { get; set; }
        public string? VeliEmail { get; set; }
        public int? VeliYakinlik { get; set; }
        public bool VeliDurum { get; set; }
    }
}
