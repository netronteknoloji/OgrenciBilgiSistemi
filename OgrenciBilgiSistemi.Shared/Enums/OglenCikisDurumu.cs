using System.ComponentModel.DataAnnotations;

namespace OgrenciBilgiSistemi.Shared.Enums
{
    public enum OglenCikisDurumu
    {
        Evet = 0,

        [Display(Name = "Hayır")]
        Hayir = 1
    }
}
