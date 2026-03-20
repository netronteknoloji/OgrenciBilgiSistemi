using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OgrenciBilgiSistemi.Models
{
    public class SinifYoklamaDurumModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int DurumId { get; set; }

        [Required]
        [StringLength(30)]
        public string DurumAd { get; set; } = string.Empty;
    }
}
