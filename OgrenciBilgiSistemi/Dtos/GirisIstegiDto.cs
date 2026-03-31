using System.ComponentModel.DataAnnotations;

namespace OgrenciBilgiSistemi.Dtos
{
    // Login formu için ayrı DTO — entity model (KullaniciModel) form binding'den korunur
    public class GirisIstegiDto
    {
        [Required(ErrorMessage = "Kullanıcı adı gereklidir.")]
        public string KullaniciAdi { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre gereklidir.")]
        [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
        [DataType(DataType.Password)]
        public string Sifre { get; set; } = string.Empty;

        public bool BeniHatirla { get; set; }
    }
}
