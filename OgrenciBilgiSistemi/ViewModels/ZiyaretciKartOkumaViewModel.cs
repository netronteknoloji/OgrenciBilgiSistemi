using System.ComponentModel.DataAnnotations;

namespace OgrenciBilgiSistemi.ViewModels
{
    public class ZiyaretciKartOkumaViewModel
    {
        [Display(Name = "Kart No")]
        public string? KartNo { get; set; }

        [Display(Name = "Kart Okuma Zamanı")]
        public DateTime? KartOkumaZamani { get; set; }

        public int? ZiyaretciId { get; set; }

        [Display(Name = "Ad Soyad")]
        public string? AdSoyad { get; set; }

        [Display(Name = "T.C. Kimlik No")]
        public string? TcKimlikNo { get; set; }

        [Display(Name = "Telefon")]
        public string? Telefon { get; set; }

        [Display(Name = "Adres")]
        public string? Adres { get; set; }

        [Display(Name = "Görüştüğü Personel")]
        public int? KullaniciId { get; set; }

        [Display(Name = "Görüştüğü Personel")]
        public string? KullaniciAdi { get; set; }

        [Display(Name = "Ziyaret Sebebi")]
        public string? ZiyaretSebebi { get; set; }

        [Display(Name = "Kart Verildi Mi?")]
        public bool KartVerildiMi { get; set; }

        [Display(Name = "Giriş Tarihi / Saati")]
        public DateTime? GirisZamani { get; set; }

        [Display(Name = "Çıkış Tarihi / Saati")]
        public DateTime? CikisZamani { get; set; }

        [Display(Name = "Aktif Ziyaret")]
        public bool AktifMi { get; set; }

        // Ekranda göstereceğin bilgi / uyarı
        public string? Mesaj { get; set; }
    }
}

