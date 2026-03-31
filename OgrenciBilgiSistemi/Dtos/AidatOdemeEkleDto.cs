using System.ComponentModel.DataAnnotations;

namespace OgrenciBilgiSistemi.Dtos
{
    public class AidatOdemeEkleDto
    {
        [Required]
        public int OgrenciId { get; set; }

        [Required, Range(2000, 2100)]
        public int Yil { get; set; }

        [Required, Range(0.01, 1_000_000)]
        public decimal Tutar { get; set; }

        [Required]
        public DateTime Tarih { get; set; } = DateTime.Now;

        [StringLength(200)]
        public string? Aciklama { get; set; }
    }
}
