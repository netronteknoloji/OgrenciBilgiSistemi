namespace OgrenciBilgiSistemi.Mobil.Models
{
    public class Kullanici
    {
        public int KullaniciId { get; set; }
        public string KullaniciAdi { get; set; }
        public string? AdSoyad { get; set; }
        public int Rol { get; set; }
        public bool KullaniciDurum { get; set; }
        public int? BirimId { get; set; }
        public int? ServisId { get; set; }
    }
}
