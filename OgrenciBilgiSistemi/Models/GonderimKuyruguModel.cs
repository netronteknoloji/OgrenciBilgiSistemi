using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OgrenciBilgiSistemi.Models
{
    /// <summary>
    /// Giden SMS ve push bildirimleri kuyruğu (outbox). Üretici yalnızca satır ekler;
    /// gönderimi GonderimKuyruguIsleyici atomik olarak üstlenip (Gonderiliyor) yapar ve tekrar dener.
    /// MVC ve API aynı tabloya yazar; kurallar Shared/Kurallar/GonderimKuyruguKurallari'ndadır.
    /// </summary>
    [Table("GonderimKuyrugu")]
    public class GonderimKuyruguModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long GonderimKuyruguId { get; set; }

        public GonderimKanali Kanal { get; set; }

        // "AnaKapi", "Yemekhane", "SinifYoklamasi", "ServisYoklamasi", "Bildirim"
        [Required, MaxLength(50)]
        public string Tip { get; set; } = string.Empty;

        // Aynı kaynağın ikinci kez kuyruğa girmesini engeller (SMS'te dolu, unique)
        [MaxLength(100)]
        public string? TekilAnahtar { get; set; }

        public int? OgrenciId { get; set; }

        // SMS
        [MaxLength(20)]
        public string? Telefon { get; set; }

        [MaxLength(500)]
        public string? Mesaj { get; set; }

        // Push: alıcı ve yükün kaynağı olan bildirim
        public int? AliciKullaniciId { get; set; }
        public int? BildirimId { get; set; }

        [ForeignKey(nameof(BildirimId))]
        public virtual BildirimModel? Bildirim { get; set; }

        public GonderimDurumu Durum { get; set; } = GonderimDurumu.Bekliyor;
        public int DenemeSayisi { get; set; }
        public DateTime SonrakiDeneme { get; set; }
        public DateTime? KilitBitis { get; set; }
        public DateTime GecerlilikBitis { get; set; }
        public DateTime OlusturmaZamani { get; set; }
        public DateTime? GonderimZamani { get; set; }

        [MaxLength(500)]
        public string? SonHata { get; set; }
    }
}
