using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OgrenciBilgiSistemi.Models
{
    /// <summary>
    /// Öğrenci ↔ ZKTeco cihaz senkron kuyruğu (outbox). Satır "bu öğrenciyi bu cihazla eşitle" demektir;
    /// CihazSenkronIsleyici öğrencinin o anki durumuna göre cihaza yazar veya cihazdan siler.
    /// Aynı cihaz + öğrenci için en fazla bir bekleyen satır olur. Kurallar: Shared/Kurallar/CihazSenkronKurallari.
    /// </summary>
    [Table("CihazSenkronKuyrugu")]
    public class CihazSenkronKuyruguModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long CihazSenkronKuyruguId { get; set; }

        public int CihazId { get; set; }
        public int OgrenciId { get; set; }

        public CihazSenkronDurumu Durum { get; set; } = CihazSenkronDurumu.Bekliyor;
        public CihazSenkronIslemi SonIslem { get; set; } = CihazSenkronIslemi.Yok;
        public int DenemeSayisi { get; set; }
        public DateTime SonrakiDeneme { get; set; }
        public DateTime? KilitBitis { get; set; }
        public DateTime OlusturmaZamani { get; set; }
        public DateTime? TamamlanmaZamani { get; set; }

        [MaxLength(500)]
        public string? SonHata { get; set; }
    }
}
