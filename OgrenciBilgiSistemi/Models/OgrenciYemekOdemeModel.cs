// Models/OgrenciYemekOdemeModel.cs
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OgrenciBilgiSistemi.Models
{
    public class OgrenciYemekOdemeModel
    {
        [Key] public int OgrenciYemekOdemeId { get; set; }

        [Required, ForeignKey(nameof(Ogrenci))]
        public int OgrenciId { get; set; }

        [Required] public int Yil { get; set; }
        [Required] public int Ay { get; set; } // 1..12 (ödemeyi bu aya say)

        [Precision(18, 2)]
        [Required] public decimal Tutar { get; set; }
        [Required] public DateTime Tarih { get; set; } = DateTime.Now;

        [StringLength(200)]
        public string? Aciklama { get; set; }

        public bool AktifMi { get; set; } = true;

        public OgrenciModel Ogrenci { get; set; } = null!;
    }
}
