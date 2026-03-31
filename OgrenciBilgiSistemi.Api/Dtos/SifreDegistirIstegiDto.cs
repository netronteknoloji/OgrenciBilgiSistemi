using System.ComponentModel.DataAnnotations;

namespace OgrenciBilgiSistemi.Api.Dtos
{
    public class SifreDegistirIstegiDto
    {
        [Required(ErrorMessage = "Yeni şifre zorunludur.")]
        [MinLength(3, ErrorMessage = "Şifre en az 3 karakter olmalıdır.")]
        public string YeniSifre { get; set; } = string.Empty;
    }
}
