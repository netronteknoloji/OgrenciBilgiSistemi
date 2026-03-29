using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZKTecoWindowsService.Models
{
    public class OgretmenModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OgretmenId { get; set; }

        [Required(ErrorMessage = "Öğretmen Adı Soyadı alanı zorunludur.")]
        [StringLength(100, ErrorMessage = "Ad Soyad en fazla 100 karakter olmalıdır.")]
        public string OgretmenAdSoyad { get; set; } = string.Empty;

        public string? OgretmenGorsel { get; set; } // Dosya yolu (nullable)

        public bool OgretmenDurum { get; set; } = true; // Varsayılan olarak aktif

        // Birime bağlı Foreign Key (nullable)
        public int? BirimId { get; set; }

        [ForeignKey("BirimId")]
        public virtual BirimModel? BirimModel { get; set; }

        public virtual List<OgrenciModel> Ogrenciler { get; set; } = new();

    }
}

