using System.ComponentModel.DataAnnotations;

namespace OgrenciBilgiSistemi.Shared.Enums
{
    public enum DuyuruHedefi
    {
        [Display(Name = "Öğretmenin Kendi Öğrencileri")]
        OgretmenKendiOgrencileri = 1,

        [Display(Name = "Tüm Veliler")]
        TumVeliler = 2
    }
}
