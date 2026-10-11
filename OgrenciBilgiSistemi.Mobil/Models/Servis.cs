namespace OgrenciBilgiSistemi.Mobil.Models
{
    public class Servis
    {
        public int KullaniciId { get; set; }
        public string Plaka { get; set; }
        public string? ServisTelefon { get; set; }
        public string? SoforAdSoyad { get; set; }
        public string? HostesAdSoyad { get; set; }
        public string? HostesTelefon { get; set; }
        public bool IsDeleted { get; set; }
    }
}
