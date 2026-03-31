using System.ComponentModel.DataAnnotations;

namespace OgrenciBilgiSistemi.Shared.Enums
{
    public enum BirimFiltre
    {
        [Display(Name = "Tüm")]
        Tum = 0,

        Aktif = 1,

        Pasif = 2
    }
}
