using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OgrenciBilgiSistemi.Models
{
    public class OgretmenProfilModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int KullaniciId { get; set; }

        [Display(Name = "Birimi")]
        public int? BirimId { get; set; }

        [ForeignKey(nameof(BirimId))]
        [ValidateNever]
        [Display(Name = "Birimi")]
        public virtual BirimModel? Birim { get; set; }

        [StringLength(120)]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz!")]
        [Display(Name = "E-posta")]
        public string? Email { get; set; }

        [Display(Name = "Fotoğraf")]
        public string? GorselPath { get; set; }

        [NotMapped]
        [ValidateNever]
        [Display(Name = "Fotoğraf Yükle")]
        public IFormFile? GorselFile { get; set; }

        [Display(Name = "Durum (Aktif)")]
        public bool OgretmenDurum { get; set; } = true;

        [ForeignKey(nameof(KullaniciId))]
        [ValidateNever]
        public virtual KullaniciModel Kullanici { get; set; } = null!;
    }
}
