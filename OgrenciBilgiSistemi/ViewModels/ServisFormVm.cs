using OgrenciBilgiSistemi.Models;
using System.ComponentModel.DataAnnotations;

namespace OgrenciBilgiSistemi.ViewModels
{
    public class ServisFormVm
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

        [Required(ErrorMessage = "Plaka gereklidir.")]
        [StringLength(20)]
        [Display(Name = "Plaka")]
        public string Plaka { get; set; } = string.Empty;

        [Display(Name = "Durum")]
        public bool ServisDurum { get; set; } = true;

        public string FormAction { get; set; } = "Ekle";
        public string SubmitText { get; set; } = "Kaydet";

        public static ServisFormVm FromModel(ServisProfilModel m) => new()
        {
            KullaniciId = m.KullaniciId,
            KullaniciAdi = m.Kullanici?.KullaniciAdi ?? string.Empty,
            Telefon = m.Kullanici?.Telefon,
            Plaka = m.Plaka,
            ServisDurum = m.ServisDurum,
            FormAction = "Guncelle",
            SubmitText = "Güncelle",
        };

        public ServisEkleVm ToEkleVm() => new()
        {
            KullaniciAdi = KullaniciAdi,
            Sifre = Sifre ?? string.Empty,
            Telefon = Telefon,
            Plaka = Plaka,
        };

        public ServisProfilModel ToProfilModel() => new()
        {
            KullaniciId = KullaniciId,
            Plaka = Plaka,
            ServisDurum = ServisDurum,
        };
    }
}
