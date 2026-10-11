using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OgrenciBilgiSistemi.Models
{
    /// <summary>
    /// Mobil API refresh token'ları (yalnızca API yazar/okur; tablo şeması burada yönetilir).
    /// Token'ın kendisi değil SHA-256 hash'i saklanır. Her girişte yeni oturum (OturumId) açılır;
    /// yenilemede eski satır iptal edilip aynı OturumId ile yeni satır eklenir (çoklu cihaz desteklenir).
    /// </summary>
    [Table("RefreshTokenlar")]
    public class RefreshTokenModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long RefreshTokenId { get; set; }

        // Genel Admin için 0 (DB'de kullanıcı satırı yok); bu yüzden Kullanicilar'a FK yok
        public int KullaniciId { get; set; }

        public Guid OturumId { get; set; }

        [Required, MaxLength(64), Column(TypeName = "char(64)")]
        public string TokenHash { get; set; } = string.Empty;

        public DateTime OlusturmaZamani { get; set; }
        public DateTime SonKullanim { get; set; }
        public DateTime? IptalZamani { get; set; }
    }
}
