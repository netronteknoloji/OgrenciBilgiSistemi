using OgrenciBilgiSistemi.Shared.Models;
using System.ComponentModel.DataAnnotations;

namespace OgrenciBilgiSistemi.Dtos
{
    // Login formu için ayrı DTO — entity model (KullaniciModel) form binding'den korunur
    public class GirisIstegiDto
    {
        [Required(ErrorMessage = "Kullanıcı adı gereklidir.")]
        public string KullaniciAdi { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre gereklidir.")]
        [StringLength(50, MinimumLength = 4, ErrorMessage = "Şifre 4-50 karakter olmalıdır.")]
        [DataType(DataType.Password)]
        public string Sifre { get; set; } = string.Empty;

        public bool BeniHatirla { get; set; }

        public string? OkulKodu { get; set; }

        public List<OkulBilgiAyari> Okullar { get; set; } = new();
    }
}
