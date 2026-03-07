namespace OgrenciBilgiSistemi.Api.Models
{
    public class KullaniciDto
    {
        public int KullaniciId { get; set; }
        public string KullaniciAdi { get; set; } = string.Empty;
        public string? AdSoyad { get; set; }
        public bool AdminMi { get; set; }
        public bool KullaniciDurum { get; set; }
        public int? BirimId { get; set; }
    }
}
