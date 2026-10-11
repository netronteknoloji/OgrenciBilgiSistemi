using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace OgrenciBilgiSistemi.Models
{
    [Index(nameof(OgrenciId), nameof(BaslangicYil), IsUnique = true)]
    public class OgrenciAidatModel
    {
        [Key] public int OgrenciAidatId { get; set; }

        [Required] public int OgrenciId { get; set; }
        public OgrenciModel Ogrenci { get; set; } = default!;

        // 🔁 Yil yerine → Akademik yıl başlangıcı (örn. 2025 => 2025-2026)
        [Required, Range(2000, 2100)]
        public int BaslangicYil { get; set; }

        // NULL => yılın tarifesi uygulanır; değer => öğrenciye özel tutar (indirim/burs)
        [Precision(18, 2)] public decimal? Borc { get; set; }

        public bool Muaf { get; set; } = false;

        // Ödenen tutar ve son ödeme tarihi kolonda tutulmaz; Odemeler'den hesaplanır.
        public ICollection<OgrenciAidatOdemeModel> Odemeler { get; set; } = new List<OgrenciAidatOdemeModel>();

        [NotMapped] public string Etiket => $"{BaslangicYil}-{BaslangicYil + 1}";
    }
}
