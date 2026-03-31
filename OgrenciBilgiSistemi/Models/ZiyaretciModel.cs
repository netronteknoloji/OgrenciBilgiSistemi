using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OgrenciBilgiSistemi.Models
{
    public class ZiyaretciModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ZiyaretciId { get; set; }

        // --- Kimlik Bilgileri ---
        [Required, StringLength(50)]
        [Display(Name = "Ad Soyad")]
        public string AdSoyad { get; set; } = string.Empty;

        [StringLength(11)]
        [Display(Name = "T.C. Kimlik No")]
        public string? TcKimlikNo { get; set; }

        // --- İletişim Bilgileri ---
        [StringLength(15)]
        [Display(Name = "Telefon")]
        public string? Telefon { get; set; }

        [StringLength(150)]
        [Display(Name = "Adres")]
        public string? Adres { get; set; }

        // --- Ziyaret Bilgileri ---

        [Display(Name = "Görüştüğü Kişi")]
        public int? KullaniciId { get; set; }

        [ForeignKey(nameof(KullaniciId))]
        [ValidateNever]
        public KullaniciModel? Kullanici { get; set; }

        [StringLength(100)]
        [Display(Name = "Ziyaret Sebebi")]
        public string? ZiyaretSebebi { get; set; }

        // --- Kart Bilgisi ---
        [StringLength(50)]
        [Display(Name = "Kart Numarası")]
        public string? KartNo { get; set; }

        [Display(Name = "Kart Verildi Mi?")]
        public bool KartVerildiMi { get; set; } = false;

        // --- Giriş / Çıkış ---
        [Display(Name = "Giriş Tarihi / Saati")]
        public DateTime GirisZamani { get; set; } = DateTime.Now;

        [Display(Name = "Çıkış Tarihi / Saati")]
        public DateTime? CikisZamani { get; set; }

        [Display(Name = "Aktif Ziyaret")]
        public bool AktifMi { get; set; } = true;


        [Display(Name = "Giriş Kapısı")]
        public int? CihazId { get; set; }

        [ForeignKey(nameof(CihazId))]
        [ValidateNever]
        public virtual CihazModel? Cihaz { get; set; }
    }
}