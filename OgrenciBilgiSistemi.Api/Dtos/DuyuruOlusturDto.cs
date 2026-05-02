using System.ComponentModel.DataAnnotations;

namespace OgrenciBilgiSistemi.Api.Dtos
{
    public class DuyuruOlusturDto
    {
        [Required]
        [StringLength(200)]
        public string Baslik { get; set; } = string.Empty;

        [Required]
        [StringLength(2000)]
        public string Icerik { get; set; } = string.Empty;
    }
}
