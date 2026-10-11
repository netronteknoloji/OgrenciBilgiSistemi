namespace OgrenciBilgiSistemi.Shared.Enums
{
    // GonderimKuyrugu.Kanal (DB'de tinyint)
    public enum GonderimKanali : byte
    {
        Sms = 1,
        Push = 2
    }

    // GonderimKuyrugu.Durum (DB'de tinyint)
    public enum GonderimDurumu : byte
    {
        Bekliyor = 0,
        Gonderiliyor = 1,
        Gonderildi = 2,
        Basarisiz = 3,
        SuresiDoldu = 4
    }
}
