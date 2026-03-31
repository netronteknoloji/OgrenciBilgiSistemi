using System.ComponentModel.DataAnnotations;

namespace OgrenciBilgiSistemi.Api.Dtos
{
    // Servis yoklama toplu kaydetme isteği için kullanılan model
    // KullaniciId DTO'da yer almaz — controller JWT token'dan alır
    public class ServisYoklamaKaydetDto
    {
        [Required]
        [Range(1, 2, ErrorMessage = "Periyot 1 (Sabah) veya 2 (Akşam) olmalıdır.")]
        public int Periyot { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "En az bir yoklama kaydı gereklidir.")]
        public List<YoklamaKayitOgesiDto> Kayitlar { get; set; } = new();
    }
}
