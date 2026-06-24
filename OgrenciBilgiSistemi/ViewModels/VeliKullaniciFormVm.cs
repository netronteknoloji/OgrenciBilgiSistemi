using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Shared.Enums;
using System.ComponentModel.DataAnnotations;

namespace OgrenciBilgiSistemi.ViewModels
{
    public class VeliKullaniciFormVm : KullaniciBaseFormVm
    {
        [StringLength(150)] public string? VeliAdres { get; set; }
        [StringLength(50)]  public string? VeliMeslek { get; set; }
        [StringLength(100)] public string? VeliIsYeri { get; set; }

        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz!")]
        [StringLength(100)] public string? VeliEmail { get; set; }

        public YakinlikTipi? VeliYakinlik { get; set; }

        public static VeliKullaniciFormVm FromModel(KullaniciModel m) => new()
        {
            KullaniciId = m.KullaniciId,
            KullaniciAdi = m.KullaniciAdi,
            Sifre = string.Empty,
            Rol = KullaniciRolu.Veli,
            KullaniciDurum = m.KullaniciDurum,
            Telefon = m.Telefon,
            VeliAdres = m.VeliProfil?.VeliAdres,
            VeliMeslek = m.VeliProfil?.VeliMeslek,
            VeliIsYeri = m.VeliProfil?.VeliIsYeri,
            VeliEmail = m.VeliProfil?.VeliEmail,
            VeliYakinlik = m.VeliProfil?.VeliYakinlik,
            FormAction = "GuncelleVeli",
            SubmitText = "Güncelle",
            IncludeId = true,
        };

        public KullaniciModel ToModel() => new()
        {
            KullaniciId = KullaniciId,
            KullaniciAdi = KullaniciAdi,
            Sifre = Sifre ?? string.Empty,
            Rol = KullaniciRolu.Veli,
            KullaniciDurum = KullaniciDurum,
            Telefon = Telefon,
            VeliProfil = new VeliProfilModel
            {
                KullaniciId = KullaniciId,
                VeliAdres = VeliAdres,
                VeliMeslek = VeliMeslek,
                VeliIsYeri = VeliIsYeri,
                VeliEmail = VeliEmail,
                VeliYakinlik = VeliYakinlik,
            },
        };
    }
}
