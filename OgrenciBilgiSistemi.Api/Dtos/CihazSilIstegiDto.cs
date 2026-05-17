using System.ComponentModel.DataAnnotations;

namespace OgrenciBilgiSistemi.Api.Dtos
{
    public class CihazSilIstegiDto
    {
        [Required]
        [StringLength(512, MinimumLength = 1)]
        public string FcmToken { get; set; } = string.Empty;
    }
}
