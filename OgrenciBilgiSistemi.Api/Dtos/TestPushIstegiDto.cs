using System.ComponentModel.DataAnnotations;

namespace OgrenciBilgiSistemi.Api.Dtos
{
    public class TestPushIstegiDto
    {
        [Required]
        public int AliciKullaniciId { get; set; }

        [Required]
        [StringLength(100)]
        public string Baslik { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Govde { get; set; } = string.Empty;
    }
}
