using OgrenciBilgiSistemi.Shared.Enums;

namespace OgrenciBilgiSistemi.Mobil.Models
{
    public class Kullanici
    {
        public int KullaniciId { get; set; }
        public string KullaniciAdi { get; set; }
        public string? AdSoyad { get; set; }
        public KullaniciRolu Rol { get; set; }
        public bool IsDeleted { get; set; }
        public int? BirimId { get; set; }
        public bool VeliProfilVar { get; set; }
        public bool ServisProfilVar { get; set; }
    }
}
