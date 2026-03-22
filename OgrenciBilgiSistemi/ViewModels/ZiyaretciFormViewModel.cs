using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace OgrenciBilgiSistemi.ViewModels
{
    public class ZiyaretciFormViewModel
    {
        // -----------------------------
        // ID (Düzenleme için)
        // -----------------------------
        public int? ZiyaretciId { get; set; }


        // -----------------------------
        // Kimlik Bilgileri
        // -----------------------------
        [Required(ErrorMessage = "Ad Soyad zorunludur.")]
        [StringLength(50, ErrorMessage = "En fazla 50 karakter yazabilirsiniz!")]
        [Display(Name = "Ad Soyad")]
        public string AdSoyad { get; set; } = string.Empty;

        [StringLength(11)]
        [Display(Name = "T.C. Kimlik No")]
        public string? TcKimlikNo { get; set; }


        // -----------------------------
        // İletişim
        // -----------------------------
        [StringLength(15)]
        [Display(Name = "Telefon")]
        public string? Telefon { get; set; }

        [StringLength(150)]
        [Display(Name = "Adres")]
        public string? Adres { get; set; }


        // -----------------------------
        // Görüştüğü Kullanıcı
        // -----------------------------
        [Display(Name = "Görüştüğü Personel")]
        public int? KullaniciId { get; set; }

        // Dropdown listesi
        public List<SelectListItem> Kullanicilar { get; set; } = new();


        // -----------------------------
        // Ziyaret Bilgileri
        // -----------------------------
        [StringLength(100)]
        [Display(Name = "Ziyaret Sebebi")]
        public string? ZiyaretSebebi { get; set; }


        // -----------------------------
        // Kart Bilgisi
        // -----------------------------
        [StringLength(50)]
        [Display(Name = "Kart Numarası")]
        public string? KartNo { get; set; }

        [Display(Name = "Kart Verildi Mi?")]
        public bool KartVerildiMi { get; set; }


        // -----------------------------
        // Giriş / Çıkış
        // -----------------------------
        [Display(Name = "Giriş Tarihi / Saati")]
        public DateTime? GirisZamani { get; set; } = DateTime.Now;

        [Display(Name = "Çıkış Tarihi / Saati")]
        public DateTime? CikisZamani { get; set; }

        [Display(Name = "Giriş Kapısı")]
        public int? CihazId { get; set; }
    }
}
