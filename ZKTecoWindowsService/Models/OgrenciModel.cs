using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ZKTecoWindowsService.Models.Enums;

namespace ZKTecoWindowsService.Models
{
    public class OgrenciModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OgrenciId { get; set; }

        [Required, StringLength(50)]
        public string OgrenciAdSoyad { get; set; } = string.Empty;

        // DB kolon tipin ne ise onu kullan: int ise böyle kalsın
        public int OgrenciNo { get; set; }

        public string? OgrenciGorsel { get; set; }

        [Required, StringLength(30)]
        public string OgrenciKartNo { get; set; } = string.Empty;

        [StringLength(50)]
        public string? OgrenciVeliAdSoyad { get; set; }

        // 10–15 haneyi rahat karşılasın diye string bırakıyoruz
        [StringLength(15)]
        public string? OgrenciVeliTelefon { get; set; }

        public OglenCikisDurumu OgrenciCikisDurumu { get; set; } = OglenCikisDurumu.Hayir;
        public bool OgrenciDurum { get; set; } = true;

        public ICollection<OgrenciDetayModel> OgrenciDetaylar { get; set; } = new List<OgrenciDetayModel>();
    }
}
