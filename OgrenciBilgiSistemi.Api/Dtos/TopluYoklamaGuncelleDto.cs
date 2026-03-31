using System.ComponentModel.DataAnnotations;

namespace OgrenciBilgiSistemi.Api.Dtos
{
    // Toplu yoklama kaydetme isteği için kullanılan model
    // KullaniciId DTO'da yer almaz — controller JWT token'dan alır
    public class TopluYoklamaGuncelleDto
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int SinifId { get; set; }

        [Required]
        [Range(1, 8)]
        public int DersNumarasi { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "En az bir yoklama kaydı gereklidir.")]
        public List<YoklamaKayitOgesiDto> Kayitlar { get; set; } = new();
    }
}
