using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OgrenciBilgiSistemi.Models
{
    public class ServisYoklamaModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ServisYoklamaId { get; set; }

        [Required]
        public int OgrenciId { get; set; }

        [ForeignKey(nameof(OgrenciId))]
        [ValidateNever]
        public virtual OgrenciModel Ogrenci { get; set; } = null!;

        [Required]
        [Display(Name = "Servis")]
        public int KullaniciId { get; set; }

        [ForeignKey(nameof(KullaniciId))]
        [ValidateNever]
        public virtual KullaniciModel Kullanici { get; set; } = null!;

        // 1 = Bindi, 2 = Binmedi
        [Required]
        public int DurumId { get; set; }

        // 1 = Sabah, 2 = Akşam
        [Required]
        public int Periyot { get; set; }

        public bool SmsGonderildi { get; set; }

        public DateTime OlusturulmaTarihi { get; set; }
        public DateTime? GuncellenmeTarihi { get; set; }
    }
}
