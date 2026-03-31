using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace OgrenciBilgiSistemi.Models
{
    [Index(nameof(OgrenciId), nameof(IstasyonTipi))]
    [Index(nameof(OgrenciGTarih))]
    [Index(nameof(OgrenciCTarih))]
    public class OgrenciDetayModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OgrenciDetayId { get; set; }

        [Required]
        public int OgrenciId { get; set; }
        [ForeignKey(nameof(OgrenciId))]
        public virtual OgrenciModel Ogrenci { get; set; } = default!;

        [Required]
        public IstasyonTipi IstasyonTipi { get; set; }

        public DateTime? OgrenciGTarih { get; set; }
        public DateTime? OgrenciCTarih { get; set; }

        // İsteğe bağlı alanlar
        [StringLength(5)]
        [Column(TypeName = "nvarchar(5)")]
        public string? OgrenciGecisTipi { get; set; }

        public bool? OgrenciSmsGonderildi { get; set; } = false;

        [Column(TypeName = "nvarchar(255)")]
        public string? OgrenciResimYolu { get; set; }

        // Tek cihaz alanı (genelde girişteki cihaz; temsilci olarak kullan)
        public int? CihazId { get; set; }
        [ForeignKey(nameof(CihazId))]
        public virtual CihazModel? Cihaz { get; set; }
    }
}
