using OgrenciBilgiSistemi.Shared.Enums;

namespace OgrenciBilgiSistemi.Api.Models
{
    public class KullaniciModel
    {
        public int KullaniciId { get; set; }
        public string KullaniciAdi { get; set; } = string.Empty;
        public KullaniciRolu Rol { get; set; }
        public bool KullaniciDurum { get; set; }
        public string? AdSoyad { get; set; }
        public bool VeliProfilVar { get; set; }
        public bool ServisProfilVar { get; set; }
    }
}
