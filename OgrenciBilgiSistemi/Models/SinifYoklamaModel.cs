using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OgrenciBilgiSistemi.Models
{
    public class SinifYoklamaModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SinifYoklamaId { get; set; }

        [Required]
        public int OgrenciId { get; set; }

        [ForeignKey(nameof(OgrenciId))]
        [ValidateNever]
        public virtual OgrenciModel Ogrenci { get; set; } = null!;

        [Required]
        [Display(Name = "Öğretmen")]
        public int KullaniciId { get; set; }

        [ForeignKey(nameof(KullaniciId))]
        [ValidateNever]
        public virtual KullaniciModel Kullanici { get; set; } = null!;

        public int? Ders1 { get; set; }
        public int? Ders2 { get; set; }
        public int? Ders3 { get; set; }
        public int? Ders4 { get; set; }
        public int? Ders5 { get; set; }
        public int? Ders6 { get; set; }
        public int? Ders7 { get; set; }
        public int? Ders8 { get; set; }

        public DateTime OlusturulmaTarihi { get; set; }
        public DateTime? GuncellenmeTarihi { get; set; }
    }
}
