using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OgrenciBilgiSistemi.Models
{
    public class KullaniciModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int KullaniciId { get; set; }

        [Required(ErrorMessage = "Kullanıcı adı gereklidir.")]
        public string KullaniciAdi { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre gereklidir.")]
        [DataType(DataType.Password)]
        public string Sifre { get; set; } = string.Empty;

        public bool BeniHatirla { get; set; } = true;

        [Display(Name = "Rol")]
        public KullaniciRolu Rol { get; set; } = KullaniciRolu.Ogretmen;

        public bool KullaniciDurum { get; set; } = true;

        public int? BirimId { get; set; }

        [ForeignKey(nameof(BirimId))]
        [ValidateNever]
        public BirimModel? Birim { get; set; }

        [NotMapped]
        public List<SelectListItem> Birimler { get; set; } = new();

        public ICollection<KullaniciMenuModel> KullaniciMenuler { get; set; } = new List<KullaniciMenuModel>();
    }
}