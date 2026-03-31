using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace OgrenciBilgiSistemi.Models
{
    [Index(nameof(BaslangicYil), IsUnique = true)]
    public class OgrenciAidatTarifeModel
    {
        [Key] public int OgrenciAidatTarifeId { get; set; }

        [Required, Range(2000, 2100)]
        public int BaslangicYil { get; set; } // 2025 => 2025-2026

        [Precision(18, 2)] public decimal Tutar { get; set; }

        [Column(TypeName = "nvarchar(200)")]
        public string? Aciklama { get; set; }

        [NotMapped] public string Etiket => $"{BaslangicYil}-{BaslangicYil + 1}";
    }
}
