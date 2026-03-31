using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OgrenciBilgiSistemi.Models
{
    [Index(nameof(OgrenciAidatId), nameof(OdemeTarihi))]
    public class OgrenciAidatOdemeModel
    {
        [Key] public int OgrenciAidatOdemeId { get; set; }

        [Required] public int OgrenciAidatId { get; set; }
        public OgrenciAidatModel OgrenciAidat { get; set; } = default!;

        [Required] public DateTime OdemeTarihi { get; set; } = DateTime.Now;

        [Precision(18, 2)]
        public decimal Tutar { get; set; }

        [Column(TypeName = "nvarchar(50)")]
        public string? OdemeTipi { get; set; }

        [Column(TypeName = "nvarchar(255)")]
        public string? Aciklama { get; set; }

        public bool AktifMi { get; set; } = true;
    }
}
