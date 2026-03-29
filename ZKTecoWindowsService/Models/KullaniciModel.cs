using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZKTecoWindowsService.Models
{
    public class KullaniciModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int KullaniciId { get; set; }

        [Required(ErrorMessage = "Kullanıcı adı gereklidir.")]
        public string KullaniciAdi { get; set; }

        [Required(ErrorMessage = "Şifre gereklidir.")]
        [DataType(DataType.Password)]
        public string Sifre { get; set; }

        public bool BeniHatirla { get; set; } = true;

        public bool AdminMi { get; set; } = false;

        public bool KullaniciDurum { get; set; } = true;
        public int? BirimId { get; set; }

        [ForeignKey("BirimId")]
        public virtual BirimModel? BirimModel { get; set; }

        public ICollection<KullaniciMenuModel> KullaniciMenuler { get; set; } = new List<KullaniciMenuModel>();
    }
}
