using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ZKTecoWindowsService.Models.Enums;

namespace ZKTecoWindowsService.Models
{
    public class OgrenciDetayModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OgrenciDetayId { get; set; }

        [Required]
        public int OgrenciId { get; set; }
        [ForeignKey(nameof(OgrenciId))]
        public OgrenciModel OgrenciModel { get; set; } = null!;

        [Required]
        public IstasyonTipi IstasyonTipi { get; set; }

        public DateTime? OgrenciGTarih { get; set; }
        public DateTime? OgrenciCTarih { get; set; }

        // "GİRİŞ" / "ÇIKIŞ" gibi kısa metinler; NULL olabilir
        [StringLength(10)]
        public string? OgrenciGecisTipi { get; set; }

        public bool? OgrenciSmsGonderildi { get; set; } = false;

        [StringLength(255)]
        public string? OgrenciResimYolu { get; set; }

        public int? CihazId { get; set; }
        [ForeignKey(nameof(CihazId))]
        public CihazModel? CihazModel { get; set; }
    }
}