using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OgrenciBilgiSistemi.Models
{
    public class OgrenciVeliModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OgrenciVeliId { get; set; }

        [StringLength(50, ErrorMessage = "En fazla 50 karakter yazabilirsiniz!")]
        [Display(Name = "Veli Ad Soyad")]
        public string? VeliAdSoyad { get; set; }

        [StringLength(15)]
        [RegularExpression(@"^\d{10,15}$", ErrorMessage = "Telefon numarası yalnızca rakamlardan oluşmalıdır!")]
        [Display(Name = "Veli Telefon")]
        public string? VeliTelefon { get; set; }

        [StringLength(150)]
        [Display(Name = "Veli Adres")]
        public string? VeliAdres { get; set; }

        [StringLength(50)]
        [Display(Name = "Veli Meslek")]
        public string? VeliMeslek { get; set; }

        [StringLength(100)]
        [Display(Name = "Veli İş Yeri")]
        public string? VeliIsYeri { get; set; }

        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz!")]
        [StringLength(100)]
        [Display(Name = "E-posta")]
        public string? VeliEmail { get; set; }

        [Display(Name = "Yakınlık")]
        public YakinlikTipi? VeliYakinlik { get; set; }

        [Display(Name = "Durum (Aktif)")]
        public bool VeliDurum { get; set; } = true;

        [Display(Name = "Kullanıcı Hesabı")]
        public int? KullaniciId { get; set; }

        [ForeignKey(nameof(KullaniciId))]
        [ValidateNever]
        public virtual KullaniciModel? Kullanici { get; set; }

        // Bir veli birden fazla öğrenciye atanabilir
        [ValidateNever]
        public virtual List<OgrenciModel> Ogrenciler { get; set; } = new();
    }
}
