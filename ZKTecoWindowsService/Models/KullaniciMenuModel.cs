namespace ZKTecoWindowsService.Models
{
    public class KullaniciMenuModel
    {
        // Kullanıcı FK
        public int KullaniciId { get; set; }
        public virtual KullaniciModel Kullanici { get; set; }

        // Menü Öğesi FK
        public int MenuOgeId { get; set; }
        public virtual MenuOgeModel MenuOge { get; set; }
    }
}
