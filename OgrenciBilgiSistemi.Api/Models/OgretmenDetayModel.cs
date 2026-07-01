namespace OgrenciBilgiSistemi.Api.Models
{
    public class OgretmenDetayModel
    {
        public int KullaniciId { get; set; }
        public string KullaniciAdi { get; set; } = string.Empty;
        public string? Telefon { get; set; }
        public string? Email { get; set; }
        public string? GorselPath { get; set; }
        public int? BirimId { get; set; }
        public string? BirimAd { get; set; }
        public bool IsDeleted { get; set; }
    }
}
