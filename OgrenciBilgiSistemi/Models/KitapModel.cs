using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OgrenciBilgiSistemi.Models
{
    public class KitapModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int KitapId { get; set; }

        [Required(ErrorMessage = "Kitap adı zorunludur!")]
        [StringLength(50, ErrorMessage = "Kitap adı en fazla 50 karakter olabilir.")]
        [Column(TypeName = "nvarchar(50)")]
        public string KitapAd { get; set; } = string.Empty;

        public string? KitapGorsel { get; set; } // 📌 Görsel URL'si saklanır (nullable olabilir)

        [StringLength(30, ErrorMessage = "Kitap türü en fazla 30 karakter olabilir.")]
        public string? KitapTurAd { get; set; }

        [Range(1, 365, ErrorMessage = "Kitap gün sayısı 1 ile 365 arasında olmalıdır.")]
        public int KitapGun { get; set; }

        public bool KitapDurum { get; set; } = true; // 📌 Varsayılan olarak aktif

        [NotMapped]
        public IFormFile? KitapGorselFile { get; set; } // 📌 Kullanıcıdan gelen görsel dosyası (Veritabanına kaydedilmez)

    }

}
