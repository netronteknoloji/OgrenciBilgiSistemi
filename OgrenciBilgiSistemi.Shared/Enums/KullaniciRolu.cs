using System.ComponentModel.DataAnnotations;

namespace OgrenciBilgiSistemi.Shared.Enums
{
    public enum KullaniciRolu
    {
        [Display(Name = "Yönetici")]
        Admin = 1,

        [Display(Name = "Öğretmen")]
        Ogretmen = 2,

        Servis = 3,

        Veli = 4
    }
}
