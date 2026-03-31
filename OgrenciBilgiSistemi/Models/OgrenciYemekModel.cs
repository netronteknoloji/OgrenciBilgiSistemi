using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OgrenciBilgiSistemi.Models
{
    public class OgrenciYemekModel
    {
        [Key] public int Id { get; set; }

        [Required, ForeignKey(nameof(Ogrenci))]
        public int OgrenciId { get; set; }

        [Required] public int Yil { get; set; }
        [Required] public int Ay { get; set; }

        [Required] public bool Aktif { get; set; } = true;

        [StringLength(200)]
        public string? Not { get; set; }

        public virtual OgrenciModel Ogrenci { get; set; } = null!;
    }
}
