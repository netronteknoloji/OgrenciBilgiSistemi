using System.ComponentModel.DataAnnotations;

namespace OgrenciBilgiSistemi.ViewModels
{
    public class ServisEkleVm
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
        [Required(ErrorMessage = "Plaka gereklidir.")]
        [StringLength(20)]
        [Display(Name = "Plaka")]
        public string Plaka { get; set; } = string.Empty;
    }
}
