using System.ComponentModel.DataAnnotations;

namespace OgrenciBilgiSistemi.ViewModels
{
    public class VeliEkleVm
    {
        // Kullanıcı alanları
        [Required(ErrorMessage = "Kullanıcı adı gereklidir.")]
        [Display(Name = "Kullanıcı Adı")]
        public string KullaniciAdi { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre gereklidir.")]
        [DataType(DataType.Password)]
        [Display(Name = "Şifre")]
        public string Sifre { get; set; } = string.Empty;

        [StringLength(15)]
        [RegularExpression(@"^\d{10,15}$", ErrorMessage = "Telefon numarası yalnızca rakamlardan oluşmalıdır!")]
        [Display(Name = "Telefon")]
        public string? Telefon { get; set; }

        // Profil alanları
        [StringLength(150)]
        [Display(Name = "Adres")]
        public string? VeliAdres { get; set; }

        [StringLength(50)]
        [Display(Name = "Meslek")]
        public string? VeliMeslek { get; set; }

        [StringLength(100)]
        [Display(Name = "İş Yeri")]
        public string? VeliIsYeri { get; set; }

        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz!")]
        [StringLength(100)]
        [Display(Name = "E-posta")]
        public string? VeliEmail { get; set; }

        [Display(Name = "Yakınlık")]
        public YakinlikTipi? VeliYakinlik { get; set; }
    }
}
