using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OgrenciBilgiSistemi.Models
{
    public class ServisProfilModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int KullaniciId { get; set; }

        [Required(ErrorMessage = "Plaka zorunludur!")]
        [StringLength(20)]
        [Display(Name = "Plaka")]
        public string Plaka { get; set; } = string.Empty;

        [Display(Name = "Durum (Aktif)")]
        public bool ServisDurum { get; set; } = true;

        [NotMapped]
        public int OgrenciSayisi { get; set; }

        [ForeignKey(nameof(KullaniciId))]
        [ValidateNever]
        public virtual KullaniciModel Kullanici { get; set; } = null!;
    }
}
