using Microsoft.AspNetCore.Mvc.Rendering;
using OgrenciBilgiSistemi.Models;
using System.ComponentModel.DataAnnotations;

namespace OgrenciBilgiSistemi.ViewModels
{
    public class OgretmenFormVm
    {
        public int KullaniciId { get; set; }

        [Required(ErrorMessage = "Kullanıcı adı gereklidir.")]
        [Display(Name = "Kullanıcı Adı")]
        public string KullaniciAdi { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Şifre")]
        public string? Sifre { get; set; }

        [StringLength(15)]
        [RegularExpression(@"^\d{10,15}$", ErrorMessage = "Telefon numarası yalnızca rakamlardan oluşmalıdır!")]
        [Display(Name = "Telefon")]
        public string? Telefon { get; set; }

        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz!")]
        [StringLength(120)]
        [Display(Name = "E-posta")]
        public string? Email { get; set; }

        [Display(Name = "Birim")]
        public int? BirimId { get; set; }

        [Display(Name = "Fotoğraf")]
        public IFormFile? GorselFile { get; set; }

        public string? GorselPath { get; set; }

        [Display(Name = "Durum")]
        public bool IsDeleted { get; set; } = false;

        public List<SelectListItem> Birimler { get; set; } = [];

        public string FormAction { get; set; } = "Ekle";
        public string SubmitText { get; set; } = "Kaydet";

        public static OgretmenFormVm FromModel(OgretmenProfilModel m) => new()
        {
            KullaniciId = m.KullaniciId,
            KullaniciAdi = m.Kullanici?.KullaniciAdi ?? string.Empty,
            Telefon = m.Kullanici?.Telefon,
            Email = m.Email,
            BirimId = m.BirimId,
            GorselPath = m.GorselPath,
            IsDeleted = m.IsDeleted,
            FormAction = "Guncelle",
            SubmitText = "Güncelle",
        };

        public OgretmenEkleVm ToEkleVm() => new()
        {
            KullaniciAdi = KullaniciAdi,
            Sifre = Sifre ?? string.Empty,
            Telefon = Telefon,
            Email = Email,
            BirimId = BirimId,
            GorselFile = GorselFile,
        };

        public OgretmenProfilModel ToProfilModel() => new()
        {
            KullaniciId = KullaniciId,
            Email = Email,
            BirimId = BirimId,
            GorselFile = GorselFile,
            IsDeleted = this.IsDeleted,
        };
    }
}
