using System.ComponentModel.DataAnnotations;

namespace OgrenciBilgiSistemi.Shared.Enums
{
    public enum YakinlikTipi
    {
        Anne = 1,

        Baba = 2,

        [Display(Name = "Kardeş")]
        Kardes = 3,

        Dede = 4,

        [Display(Name = "Diğer")]
        Diger = 5
    }
}
