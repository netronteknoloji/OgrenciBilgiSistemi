using System.ComponentModel.DataAnnotations;

namespace OgrenciBilgiSistemi.Api.Dtos
{
    public class SifreDegistirIstegiDto
    {
        [Required(ErrorMessage = "Yeni şifre zorunludur.")]
        [StringLength(50, MinimumLength = 4, ErrorMessage = "Şifre 4-50 karakter olmalıdır.")]
        public string YeniSifre { get; set; } = string.Empty;
    }
}
