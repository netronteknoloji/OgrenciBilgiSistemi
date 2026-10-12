namespace OgrenciBilgiSistemi.Shared.Enums
{
    // CihazSenkronKuyrugu.Durum (DB'de tinyint)
    public enum CihazSenkronDurumu : byte
    {
        Bekliyor = 0,
        Isleniyor = 1,
        Tamamlandi = 2,
        Basarisiz = 3
    }

    // İşleyicinin cihazda yaptığı son işlem (panelde gösterilir)
    public enum CihazSenkronIslemi : byte
    {
        Yok = 0,        // cihazda olmaması gerekiyordu ve zaten yoktu
        Yazildi = 1,    // eklendi / güncellendi
        Silindi = 2
    }
}
