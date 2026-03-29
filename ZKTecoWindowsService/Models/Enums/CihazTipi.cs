namespace ZKTecoWindowsService.Models.Enums
{
    // Donaným türü (deðiþebilir)
    public enum DonanimTipi : byte
    {
        UsbRfid = 1,
        ZKTeco = 2,
        QrOkuyucu = 3,
        Diger = 9
    }

    // Ýstasyon/kapý türü (raporlama bununla yapýlýr)
    public enum IstasyonTipi : short
    {
        Bilinmiyor = 0,

        AnaKapi = 10,
        Yemekhane = 20,

    }

    public enum OglenCikisDurumu
    {
        Evet = 0,
        Hayir = 1
    }
}