using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZKTecoWindowsService.Models
{
    public class BirimModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] 
        public int BirimId { get; set; }

        [Required(ErrorMessage = "Birim adı zorunludur.")]
        [StringLength(50, ErrorMessage = "Birim adı en fazla 50 karakter olabilir.")]
        [Display(Name = "Birim Adı")]
        public string BirimAd { get; set; } = string.Empty; //  Null hatalarını önlemek için default değer

        public bool BirimDurum { get; set; } = true;
        public virtual List<OgretmenModel> Ogretmenler { get; set; } = new();
        public virtual List<KullaniciModel> Kullanicilar { get; set; } = new();
        public virtual List<OgrenciModel> Ogrenciler { get; set; } = new();

    }
}
