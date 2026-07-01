using OgrenciBilgiSistemi.Models;
using System.ComponentModel.DataAnnotations;

namespace OgrenciBilgiSistemi.ViewModels
{
    public class VeliFormVm
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

        [StringLength(150)] [Display(Name = "Adres")]    public string? VeliAdres { get; set; }
        [StringLength(50)]  [Display(Name = "Meslek")]   public string? VeliMeslek { get; set; }
        [StringLength(100)] [Display(Name = "İş Yeri")]  public string? VeliIsYeri { get; set; }

        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz!")]
        [StringLength(100)] [Display(Name = "E-posta")]  public string? VeliEmail { get; set; }

        [Display(Name = "Yakınlık")]
        public YakinlikTipi? VeliYakinlik { get; set; }

        [Display(Name = "Durum")]
        public bool IsDeleted { get; set; } = false;

        public string FormAction { get; set; } = "Ekle";
        public string SubmitText { get; set; } = "Kaydet";

        public static VeliFormVm FromModel(VeliProfilModel m) => new()
        {
            KullaniciId = m.KullaniciId,
            KullaniciAdi = m.Kullanici?.KullaniciAdi ?? string.Empty,
            Telefon = m.Kullanici?.Telefon,
            VeliAdres = m.VeliAdres,
            VeliMeslek = m.VeliMeslek,
            VeliIsYeri = m.VeliIsYeri,
            VeliEmail = m.VeliEmail,
            VeliYakinlik = m.VeliYakinlik,
            IsDeleted = m.IsDeleted,
            FormAction = "Guncelle",
            SubmitText = "Güncelle",
        };

        public VeliEkleVm ToEkleVm() => new()
        {
            KullaniciAdi = KullaniciAdi,
            Sifre = Sifre ?? string.Empty,
            Telefon = Telefon,
            VeliAdres = VeliAdres,
            VeliMeslek = VeliMeslek,
            VeliIsYeri = VeliIsYeri,
            VeliEmail = VeliEmail,
            VeliYakinlik = VeliYakinlik,
        };

        public VeliProfilModel ToProfilModel() => new()
        {
            KullaniciId = KullaniciId,
            VeliAdres = VeliAdres,
            VeliMeslek = VeliMeslek,
            VeliIsYeri = VeliIsYeri,
            VeliEmail = VeliEmail,
            VeliYakinlik = VeliYakinlik,
            IsDeleted = this.IsDeleted,
        };
    }
}
