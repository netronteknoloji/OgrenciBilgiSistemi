using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace OgrenciBilgiSistemi.Models
{
    public class SmsGonderimGecmisiModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SmsGonderimGecmisiId { get; set; }

        // null = sistem SMS'i (öğrenciye bağlı değil)
        public int? OgrenciId { get; set; }

        [ForeignKey(nameof(OgrenciId))]
        [ValidateNever]
        public virtual OgrenciModel? Ogrenci { get; set; }

        [Required]
        [StringLength(20)]
        public string Telefon { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Mesaj { get; set; } = string.Empty;

        // "AnaKapi", "Yemekhane", "ServisYoklamasi", "SinifYoklamasi"
        [Required]
        [StringLength(50)]
        public string Tip { get; set; } = string.Empty;

        public DateTime GonderimZamani { get; set; }

        public bool Basarili { get; set; }

        // SmsHataKategorisi enum int değeri (Yok=0, Gecici=1, Kalici=2, Bilinmiyor=3)
        public int HataKategorisi { get; set; }

        [StringLength(500)]
        public string? Hata { get; set; }

        // Sağlayıcı ham yanıtı (HTTP body) - uzun olabilir
        public string? HamCevap { get; set; }

        public int? HttpDurumKodu { get; set; }

        // 1=ilk gönderim, 2+ = retry denemesi
        public int DenemeNumarasi { get; set; } = 1;
    }
}
