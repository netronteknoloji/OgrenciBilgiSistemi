using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OgrenciBilgiSistemi.Models
{
    public class ServisModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ServisId { get; set; }

        [Required(ErrorMessage = "Plaka zorunludur!")]
        [StringLength(20)]
        [Display(Name = "Plaka")]
        public string Plaka { get; set; } = string.Empty;

        [Display(Name = "Kullanıcı")]
        public int? KullaniciId { get; set; }

        [ForeignKey(nameof(KullaniciId))]
        [ValidateNever]
        public virtual KullaniciModel? Kullanici { get; set; }

        [Display(Name = "Durum (Aktif)")]
        public bool ServisDurum { get; set; } = true;

        [ValidateNever]
        public virtual List<OgrenciModel> Ogrenciler { get; set; } = new();
    }
}
