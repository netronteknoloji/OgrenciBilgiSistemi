namespace OgrenciBilgiSistemi.Api.Models
{
    public class KullaniciModel
    {
        public int KullaniciId { get; set; }
        public string KullaniciAdi { get; set; } = string.Empty;
        public bool AdminMi { get; set; }
        public bool KullaniciDurum { get; set; }
        public int? BirimId { get; set; }
    }
}
