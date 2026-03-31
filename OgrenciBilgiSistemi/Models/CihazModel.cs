using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OgrenciBilgiSistemi.Models
{
    // Hızlı arama ve benzersizlik için indeksler
    [Index(nameof(CihazKodu), IsUnique = true)]
    [Index(nameof(CihazAdi), IsUnique = true)]
    [Index(nameof(IstasyonTipi))]
    public class CihazModel : IValidatableObject
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CihazId { get; set; }

        [Required, StringLength(100)]
        public string CihazAdi { get; set; } = string.Empty; // Örn: RFID-ANA-01, ZK-YMK-01

        /// <summary>Benzersiz cihaz kimliği (GUID).</summary>
        public Guid CihazKodu { get; set; } = Guid.NewGuid();

        /// <summary>Donanım türü (UsbRfid / ZKTeco / QrOkuyucu / Diger).</summary>
        [Required]
        public DonanimTipi DonanimTipi { get; set; } = DonanimTipi.UsbRfid;

        /// <summary>Raporlamada kapı/istasyon türü.</summary>
        [Required]
        public IstasyonTipi IstasyonTipi { get; set; } = IstasyonTipi.AnaKapi;

        /// <summary>Cihaz aktif/pasif.</summary>
        public bool Aktif { get; set; } = true;

        // --- ZKTeco (IP tabanlı bağlantı) ---
        /// <summary>ZKTeco için IPv4 adresi (USB-RFID için boş).</summary>
        [StringLength(45)]
        [RegularExpression(
            @"^$|^(?:(?:25[0-5]|2[0-4]\d|[01]?\d\d?)\.){3}(?:25[0-5]|2[0-4]\d|[01]?\d\d?)$",
            ErrorMessage = "Geçerli bir IPv4 adresi giriniz."
        )]
        public string? IpAdresi { get; set; }

        /// <summary>ZKTeco için port (örn: 4370) – USB için boş.</summary>
        [Range(1, 65535, ErrorMessage = "Port 1-65535 aralığında olmalıdır.")]
        public int? PortNo { get; set; }

        // --- Doğrulama: Donanım türüne göre zorunlular ---
        public IEnumerable<ValidationResult> Validate(ValidationContext _)
        {
            if (DonanimTipi == DonanimTipi.ZKTeco)
            {
                if (string.IsNullOrWhiteSpace(IpAdresi))
                    yield return new ValidationResult("ZKTeco için IP adresi zorunludur.", new[] { nameof(IpAdresi) });

                if (!PortNo.HasValue || PortNo.Value <= 0)
                    yield return new ValidationResult("ZKTeco için port numarası zorunludur.", new[] { nameof(PortNo) });
            }
        }
    }
}
