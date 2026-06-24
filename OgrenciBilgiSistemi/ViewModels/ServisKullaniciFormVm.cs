using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Shared.Enums;
using System.ComponentModel.DataAnnotations;

namespace OgrenciBilgiSistemi.ViewModels
{
    public class ServisKullaniciFormVm : KullaniciBaseFormVm
    {
        [StringLength(20)]
        public string? ServisPlaka { get; set; }

        public static ServisKullaniciFormVm FromModel(KullaniciModel m) => new()
        {
            KullaniciId = m.KullaniciId,
            KullaniciAdi = m.KullaniciAdi,
            Sifre = string.Empty,
            Rol = KullaniciRolu.Servis,
            KullaniciDurum = m.KullaniciDurum,
            Telefon = m.Telefon,
            ServisPlaka = m.ServisProfil?.Plaka,
            FormAction = "GuncelleServis",
            SubmitText = "Güncelle",
            IncludeId = true,
        };

        public KullaniciModel ToModel() => new()
        {
            KullaniciId = KullaniciId,
            KullaniciAdi = KullaniciAdi,
            Sifre = Sifre ?? string.Empty,
            Rol = KullaniciRolu.Servis,
            KullaniciDurum = KullaniciDurum,
            Telefon = Telefon,
            ServisProfil = new ServisProfilModel
            {
                KullaniciId = KullaniciId,
                Plaka = ServisPlaka ?? string.Empty,
            },
        };
    }
}
