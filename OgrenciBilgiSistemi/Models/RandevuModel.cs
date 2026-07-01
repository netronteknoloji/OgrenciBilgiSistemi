using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace OgrenciBilgiSistemi.Models
{
    public class RandevuModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RandevuId { get; set; }

        [Required]
        [Display(Name = "Öğretmen")]
        public int OgretmenKullaniciId { get; set; }

        [Required]
        [Display(Name = "Veli")]
        public int VeliKullaniciId { get; set; }

        [Display(Name = "Öğrenci")]
        public int? OgrenciId { get; set; }

        [Required]
        [Display(Name = "Randevu Tarihi")]
        public DateTime RandevuTarihi { get; set; }

        [Display(Name = "Süre (dk)")]
        public int SureDakika { get; set; } = 30;

        [Display(Name = "Durum")]
        public RandevuDurumu Durum { get; set; } = RandevuDurumu.Beklemede;

        [StringLength(500)]
        [Display(Name = "Not")]
        public string? Not { get; set; }

        public bool OgretmenTarafindanOlusturuldu { get; set; }

        public DateTime OlusturulmaTarihi { get; set; } = DateTime.Now;
        public DateTime? GuncellenmeTarihi { get; set; }
        public bool IsDeleted { get; set; } = false;

        // Navigation
        [ForeignKey(nameof(OgretmenKullaniciId))]
        [ValidateNever]
        public virtual KullaniciModel Ogretmen { get; set; } = null!;

        [ForeignKey(nameof(VeliKullaniciId))]
        [ValidateNever]
        public virtual KullaniciModel Veli { get; set; } = null!;

        [ForeignKey(nameof(OgrenciId))]
        [ValidateNever]
        public virtual OgrenciModel? Ogrenci { get; set; }
    }
}
