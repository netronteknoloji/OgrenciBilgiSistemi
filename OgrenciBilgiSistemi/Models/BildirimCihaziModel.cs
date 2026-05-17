using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using OgrenciBilgiSistemi.Push;

namespace OgrenciBilgiSistemi.Models
{
    public class BildirimCihaziModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int BildirimCihaziId { get; set; }

        [Required]
        public int KullaniciId { get; set; }

        [Required]
        [StringLength(512)]
        public string FcmToken { get; set; } = string.Empty;

        [Required]
        public PushPlatformu Platform { get; set; }

        [StringLength(32)]
        public string? UygulamaSurumu { get; set; }

        [StringLength(64)]
        public string? CihazModeli { get; set; }

        public DateTime OlusturulmaTarihi { get; set; } = DateTime.Now;

        public DateTime SonGuncelleme { get; set; } = DateTime.Now;

        public bool IsDeleted { get; set; } = false;

        [ForeignKey(nameof(KullaniciId))]
        [ValidateNever]
        public virtual KullaniciModel Kullanici { get; set; } = null!;
    }
}
