namespace OgrenciBilgiSistemi.Models
{
    public class KullaniciMenuModel
    {
        public int KullaniciId { get; set; }
        public KullaniciModel Kullanici { get; set; } = null!;

        public int MenuOgeId { get; set; }
        public MenuOgeModel MenuOge { get; set; } = null!;
    }
}
