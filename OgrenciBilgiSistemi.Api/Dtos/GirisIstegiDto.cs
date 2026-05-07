using System.ComponentModel.DataAnnotations;

namespace OgrenciBilgiSistemi.Api.Dtos
{
    public class GirisIstegiDto
    {
        [Required(ErrorMessage = "Kullanıcı adı gereklidir.")]
        public string KullaniciAdi { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre gereklidir.")]
        [StringLength(50, MinimumLength = 4, ErrorMessage = "Şifre 4-50 karakter olmalıdır.")]
        public string Sifre { get; set; } = string.Empty;

        [Required(ErrorMessage = "Okul kodu gereklidir.")]
        public string OkulKodu { get; set; } = string.Empty;
    }
}
