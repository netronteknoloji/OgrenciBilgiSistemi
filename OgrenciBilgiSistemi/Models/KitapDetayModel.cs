using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;


namespace OgrenciBilgiSistemi.Models
{
    public class KitapDetayModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int KitapDetayId { get; set; }

        [Required(ErrorMessage = "Kitap alma tarihi zorunludur!")]
        public DateTime KitapAlTarih { get; set; } = DateTime.Now;

        public DateTime? KitapVerTarih { get; set; }

        [Required]
        public KitapDurumu KitapDurum { get; set; } = KitapDurumu.Alındı;

        // --- İlişkiler (required) ---
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Lütfen bir kitap seçiniz.")]
        public int KitapId { get; set; }
        [ValidateNever]
        [ForeignKey(nameof(KitapId))]
        public virtual KitapModel Kitap { get; set; } = null!;

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Lütfen bir öğrenci seçiniz.")]
        public int OgrenciId { get; set; }
        [ValidateNever]
        [ForeignKey(nameof(OgrenciId))]
        public virtual OgrenciModel Ogrenci { get; set; } = null!;

        // --- UI yardımcıları (persist edilmez) ---
        [NotMapped] public IReadOnlyList<SelectListItem>? Kitaplar { get; set; }
        [NotMapped] public IReadOnlyList<SelectListItem>? Ogrenciler { get; set; }
    }

    public enum KitapDurumu
    {
        Alındı = 1,
        TeslimEdildi = 0
    }
}
