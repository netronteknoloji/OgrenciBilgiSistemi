using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ZKTecoWindowsService.Models.Enums;

namespace ZKTecoWindowsService.Models
{
    public class CihazModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CihazId { get; set; }

        [Required, StringLength(100)]
        public string CihazAdi { get; set; } = string.Empty;

        // Benzersiz kimlik (opsiyonel ama pratik)
        public Guid CihazKodu { get; set; } = Guid.NewGuid();

        [Required]
        public DonanimTipi DonanimTipi { get; set; } = DonanimTipi.UsbRfid;

        [Required]
        public IstasyonTipi IstasyonTipi { get; set; } = IstasyonTipi.AnaKapi;

        public bool Aktif { get; set; } = true;

        // ZKTeco için gerekli olabilir; RFID cihazlarda boş kalır
        public string? IpAdresi { get; set; }
        public int? PortNo { get; set; }

        public ICollection<OgrenciDetayModel> OgrenciDetaylar { get; set; } = new List<OgrenciDetayModel>();
    }
}
