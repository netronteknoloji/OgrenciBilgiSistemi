using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OgrenciBilgiSistemi.Models
{
    public class BirimModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] 
        public int BirimId { get; set; }

        [Required(ErrorMessage = "Birim adı zorunludur.")]
        [StringLength(50, ErrorMessage = "Birim adı en fazla 50 karakter olabilir.")]
        [Display(Name = "Birim Adı")]
        public string BirimAd { get; set; } = string.Empty;


        [Display(Name = "Aktif Mi?")]
        public bool BirimDurum { get; set; } = true;

        [Display(Name = "Sınıf Mı?")]
        public bool BirimSinifMi { get; set; } = true;

        [ValidateNever]
        public virtual List<OgrenciModel> Ogrenciler { get; set; } = new();

    }
}
