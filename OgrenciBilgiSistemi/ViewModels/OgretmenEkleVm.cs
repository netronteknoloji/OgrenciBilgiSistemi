using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace OgrenciBilgiSistemi.ViewModels
{
    public class OgretmenEkleVm
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
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz!")]
        [Display(Name = "E-posta")]
        public string? Email { get; set; }

        [Display(Name = "Birim")]
        public int? BirimId { get; set; }

        [Display(Name = "Fotoğraf")]
        public IFormFile? GorselFile { get; set; }
    }
}
