using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using OgrenciBilgiSistemi.Shared.Enums;

namespace OgrenciBilgiSistemi.Models
{
    public class DuyuruModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int DuyuruId { get; set; }

        [Required]
        public int OlusturanKullaniciId { get; set; }

        [Required]
        public DuyuruHedefi Hedef { get; set; }

        [Required]
        [StringLength(200)]
        public string Baslik { get; set; } = string.Empty;

        [Required]
        [StringLength(2000)]
        public string Icerik { get; set; } = string.Empty;

        public DateTime OlusturulmaTarihi { get; set; } = DateTime.Now;

        public bool IsDeleted { get; set; } = false;

        [ForeignKey(nameof(OlusturanKullaniciId))]
        [ValidateNever]
        public virtual KullaniciModel Olusturan { get; set; } = null!;
    }
}
