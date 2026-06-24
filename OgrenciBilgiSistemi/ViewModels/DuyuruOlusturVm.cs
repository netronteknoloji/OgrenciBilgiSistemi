using System.ComponentModel.DataAnnotations;

namespace OgrenciBilgiSistemi.ViewModels
{
    public class DuyuruOlusturVm
    {
        [Required(ErrorMessage = "Başlık zorunludur.")]
        [StringLength(200)]
        public string Baslik { get; set; } = string.Empty;

        [Required(ErrorMessage = "İçerik zorunludur.")]
        [StringLength(2000)]
        public string Icerik { get; set; } = string.Empty;
    }
}
