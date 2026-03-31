using System.ComponentModel.DataAnnotations;

namespace OgrenciBilgiSistemi.Shared.Enums
{
    public enum DonanimTipi : byte
    {
        UsbRfid = 1,

        ZKTeco = 2,

        QrOkuyucu = 3,

        [Display(Name = "Diğer")]
        Diger = 9
    }

    public enum IstasyonTipi : short
    {
        Bilinmiyor = 0,

        [Display(Name = "AnaKapı")]
        AnaKapi = 10,

        Yemekhane = 20,
    }
}
