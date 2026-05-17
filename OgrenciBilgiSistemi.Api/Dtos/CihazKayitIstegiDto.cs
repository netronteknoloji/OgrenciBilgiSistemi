using System.ComponentModel.DataAnnotations;
using OgrenciBilgiSistemi.Push;

namespace OgrenciBilgiSistemi.Api.Dtos
{
    public class CihazKayitIstegiDto
    {
        [Required]
        [StringLength(512, MinimumLength = 1)]
        public string FcmToken { get; set; } = string.Empty;

        [Required]
        public PushPlatformu Platform { get; set; }

        [StringLength(32)]
        public string? UygulamaSurumu { get; set; }

        [StringLength(64)]
        public string? CihazModeli { get; set; }
    }
}
