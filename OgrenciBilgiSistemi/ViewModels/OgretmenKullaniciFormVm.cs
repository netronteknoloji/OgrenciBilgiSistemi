using Microsoft.AspNetCore.Mvc.Rendering;
using OgrenciBilgiSistemi.Models;
using System.ComponentModel.DataAnnotations;

namespace OgrenciBilgiSistemi.ViewModels
{
    public class OgretmenKullaniciFormVm : KullaniciBaseFormVm
    {
        [StringLength(120)]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz!")]
        public string? OgretmenEmail { get; set; }

        public int? OgretmenBirimId { get; set; }
        public IFormFile? OgretmenGorselFile { get; set; }
        public string? OgretmenGorselPath { get; set; }

        public List<SelectListItem> Birimler { get; set; } = [];

        public static OgretmenKullaniciFormVm FromModel(KullaniciModel m) => new()
        {
            KullaniciId = m.KullaniciId,
            KullaniciAdi = m.KullaniciAdi,
            Sifre = string.Empty,
            Rol = KullaniciRolu.Ogretmen,
            IsDeleted = m.IsDeleted,
            Telefon = m.Telefon,
            OgretmenEmail = m.OgretmenProfil?.Email,
            OgretmenBirimId = m.OgretmenProfil?.BirimId,
            OgretmenGorselPath = m.OgretmenProfil?.GorselPath,
            FormAction = "GuncelleOgretmen",
            SubmitText = "Güncelle",
            IncludeId = true,
        };

        public KullaniciModel ToModel() => new()
        {
            KullaniciId = KullaniciId,
            KullaniciAdi = KullaniciAdi,
            Sifre = Sifre ?? string.Empty,
            Rol = KullaniciRolu.Ogretmen,
            IsDeleted = this.IsDeleted,
            Telefon = Telefon,
            OgretmenProfil = new OgretmenProfilModel
            {
                KullaniciId = KullaniciId,
                Email = OgretmenEmail,
                BirimId = OgretmenBirimId,
                GorselFile = OgretmenGorselFile,
            },
        };
    }
}
