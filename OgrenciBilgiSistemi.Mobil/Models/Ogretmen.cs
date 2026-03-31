namespace OgrenciBilgiSistemi.Mobil.Models
{
    public class Ogretmen
    {
        public int KullaniciId { get; set; }
        public string KullaniciAdi { get; set; }
        public string? GorselPath { get; set; }
        public bool KullaniciDurum { get; set; }
        public int? BirimId { get; set; }
        public string? Email { get; set; }
        public string? KartNo { get; set; }
    }
}
