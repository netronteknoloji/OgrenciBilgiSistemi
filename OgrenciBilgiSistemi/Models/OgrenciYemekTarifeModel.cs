using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OgrenciBilgiSistemi.Models
{
    // Aynı öğrenci için bir yılda tek tarife
    [Index(nameof(OgrenciId), nameof(Yil), IsUnique = true)]
    public class OgrenciYemekTarifeModel
    {
        [Key] public int Id { get; set; }

        [Required, ForeignKey(nameof(Ogrenci))]
        public int OgrenciId { get; set; }

        [Required, Range(2000, 2100)]
        public int Yil { get; set; }

        [Required, Precision(18, 2)]
        [Range(0, double.MaxValue, ErrorMessage = "Aylık tutar negatif olamaz.")]
        public decimal AylikTutar { get; set; }

        [StringLength(200)]
        public string? Aciklama { get; set; }

        public OgrenciModel Ogrenci { get; set; } = null!;
    }
}
