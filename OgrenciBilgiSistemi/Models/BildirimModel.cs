using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace OgrenciBilgiSistemi.Models
{
    public class BildirimModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int BildirimId { get; set; }

        [Required]
        public int AliciKullaniciId { get; set; }

        [Required]
        public BildirimTuru Tur { get; set; }

        [Required]
        [StringLength(500)]
        public string Mesaj { get; set; } = string.Empty;

        public int? RandevuId { get; set; }

        public bool Okundu { get; set; } = false;

        public DateTime OlusturulmaTarihi { get; set; } = DateTime.Now;

        public bool IsDeleted { get; set; } = false;

        // Navigation
        [ForeignKey(nameof(AliciKullaniciId))]
        [ValidateNever]
        public virtual KullaniciModel Alici { get; set; } = null!;

        [ForeignKey(nameof(RandevuId))]
        [ValidateNever]
        public virtual RandevuModel? Randevu { get; set; }
    }
}
