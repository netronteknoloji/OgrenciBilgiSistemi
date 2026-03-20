namespace OgrenciBilgiSistemi.Api.Models
{
    public class KullaniciModel
    {
        public int KullaniciId { get; set; }
        public string KullaniciAdi { get; set; } = string.Empty;
        public int Rol { get; set; }
        public bool KullaniciDurum { get; set; }
        public int? BirimId { get; set; }
        public int? ServisId { get; set; }
    }
}
