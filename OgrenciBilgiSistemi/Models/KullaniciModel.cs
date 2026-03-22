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

        [StringLength(15)]
        [RegularExpression(@"^\d{10,15}$", ErrorMessage = "Telefon numarası yalnızca rakamlardan oluşmalıdır!")]
        public string? Telefon { get; set; }

        [StringLength(30, ErrorMessage = "En fazla 30 karakter yazabilirsiniz!")]
        [Display(Name = "Kart No")]
        public string? KartNo { get; set; }

        [Display(Name = "Fotoğraf")]
        public string? GorselPath { get; set; }

        [Display(Name = "Birimi")]
        public int? BirimId { get; set; }

        [ForeignKey(nameof(BirimId))]
        [ValidateNever]
        [Display(Name = "Birimi")]
        public virtual BirimModel? Birim { get; set; }

        [StringLength(120)]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz!")]
        [Display(Name = "E-posta")]
        public string? Email { get; set; }

        [NotMapped]
        public int? ServisId { get; set; }

        [NotMapped]
        [ValidateNever]
        [Display(Name = "Fotoğraf Yükle")]
        public IFormFile? GorselFile { get; set; }

        [NotMapped]
        [ValidateNever]
        public List<SelectListItem> Servisler { get; set; } = new();

        [NotMapped]
        [ValidateNever]
        public List<SelectListItem> Birimler { get; set; } = new();

        public ICollection<KullaniciMenuModel> KullaniciMenuler { get; set; } = new List<KullaniciMenuModel>();

        // Navigasyon koleksiyonları
        [ValidateNever]
        public virtual List<OgrenciModel> Ogrenciler { get; set; } = new();

        [ValidateNever]
        public virtual List<ZiyaretciModel> Ziyaretciler { get; set; } = new();

        [ValidateNever]
        public virtual List<SinifYoklamaModel> SinifYoklamalar { get; set; } = new();
    }
}