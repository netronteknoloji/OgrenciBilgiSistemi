using Microsoft.AspNetCore.Mvc.Rendering;
using OgrenciBilgiSistemi.Models;
using System.ComponentModel.DataAnnotations;

namespace OgrenciBilgiSistemi.ViewModels
{
    public class KullaniciFormVm : KullaniciBaseFormVm
    {
        // Öğretmen profil
        [StringLength(120)]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz!")]
        public string? OgretmenEmail { get; set; }
        public int? OgretmenBirimId { get; set; }
        public IFormFile? OgretmenGorselFile { get; set; }
        public string? OgretmenGorselPath { get; set; }

        // Servis profil
        [StringLength(20)]
        public string? ServisPlaka { get; set; }

        // Veli profil
        [StringLength(150)] public string? VeliAdres { get; set; }
        [StringLength(50)]  public string? VeliMeslek { get; set; }
        [StringLength(100)] public string? VeliIsYeri { get; set; }
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz!")]
        [StringLength(100)] public string? VeliEmail { get; set; }
        public YakinlikTipi? VeliYakinlik { get; set; }

        // Dropdown listeler (Ekle + Admin Guncelle)
        public List<SelectListItem> Servisler { get; set; } = [];
        public List<SelectListItem> Birimler { get; set; } = [];

        public static KullaniciFormVm FromModel(KullaniciModel m) => new()
        {
            KullaniciId = m.KullaniciId,
            KullaniciAdi = m.KullaniciAdi,
            Sifre = string.Empty,
            Rol = m.Rol,
            IsDeleted = m.IsDeleted,
            Telefon = m.Telefon,
            OgretmenEmail = m.OgretmenProfil?.Email,
            OgretmenBirimId = m.OgretmenProfil?.BirimId,
            OgretmenGorselPath = m.OgretmenProfil?.GorselPath,
            ServisPlaka = m.ServisProfil?.Plaka,
            VeliAdres = m.VeliProfil?.VeliAdres,
            VeliMeslek = m.VeliProfil?.VeliMeslek,
            VeliIsYeri = m.VeliProfil?.VeliIsYeri,
            VeliEmail = m.VeliProfil?.VeliEmail,
            VeliYakinlik = m.VeliProfil?.VeliYakinlik,
            FormAction = "Guncelle",
            SubmitText = "Güncelle",
            IncludeId = true,
        };

        public KullaniciModel ToModel() => new()
        {
            KullaniciId = KullaniciId,
            KullaniciAdi = KullaniciAdi,
            Sifre = Sifre ?? string.Empty,
            Rol = Rol,
            IsDeleted = this.IsDeleted,
            Telefon = Telefon,
            OgretmenProfil = Rol == KullaniciRolu.Ogretmen ? new OgretmenProfilModel
            {
                KullaniciId = KullaniciId,
                Email = OgretmenEmail,
                BirimId = OgretmenBirimId,
                GorselFile = OgretmenGorselFile,
            } : null,
            ServisProfil = Rol == KullaniciRolu.Servis ? new ServisProfilModel
            {
                KullaniciId = KullaniciId,
                Plaka = ServisPlaka ?? string.Empty,
            } : null,
            VeliProfil = Rol == KullaniciRolu.Veli ? new VeliProfilModel
            {
                KullaniciId = KullaniciId,
                VeliAdres = VeliAdres,
                VeliMeslek = VeliMeslek,
                VeliIsYeri = VeliIsYeri,
                VeliEmail = VeliEmail,
                VeliYakinlik = VeliYakinlik,
            } : null,
        };
    }
}
