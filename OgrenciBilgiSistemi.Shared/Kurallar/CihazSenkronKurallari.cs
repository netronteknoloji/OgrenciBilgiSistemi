using OgrenciBilgiSistemi.Shared.Enums;

namespace OgrenciBilgiSistemi.Shared.Kurallar
{
    public enum CihazSenkronSonucTuru
    {
        Basarili,
        Gecici,   // cihaza ulaşılamadı: cihaz açılana kadar tekrar denenir
        Kalici,   // cihaz işlemi reddetti: birkaç denemeden sonra vazgeçilir
        Cakisma   // cihazda bu numarada öğrenciyle eşleşmeyen (elle eklenmiş) kullanıcı var: dokunulmaz, yönetici karar verir
    }

    /// <summary>
    /// Öğrenci ↔ ZKTeco cihaz senkronu (CihazSenkronKuyrugu) kuralları. Kuyruk "ne yapılacağını" değil
    /// "bu öğrenciyi bu cihazla eşitle"yi tutar; işleyici öğrencinin o anki durumuna göre yazar veya siler.
    /// </summary>
    public static class CihazSenkronKurallari
    {
        public const int MaksKaliciDeneme = 5;
        public const int TopluAlimBoyutu = 200;
        public static readonly TimeSpan KilitSuresi = TimeSpan.FromMinutes(10);
        public static readonly TimeSpan SaklamaSuresi = TimeSpan.FromDays(7);

        /// <summary>
        /// Öğrenci bu istasyondaki cihazda kayıtlı olmalı mı: ana kapıda her aktif öğrenci,
        /// yemekhanede yalnızca bu ay yemek hakkı olan aktif öğrenci.
        /// </summary>
        public static bool CihazdaOlmaliMi(IstasyonTipi istasyon, bool ogrenciAktif, bool buAyYemekVar)
            => ogrenciAktif && istasyon switch
            {
                IstasyonTipi.AnaKapi => true,
                IstasyonTipi.Yemekhane => buAyYemekVar,
                _ => false
            };

        /// <summary>
        /// Bir denemeden sonra satırın yeni durumu ve (bekliyorsa) sonraki deneme zamanı.
        /// Cihaza ulaşılamaması kalıcı hata sayılmaz: cihaz kapalı kaldıkça (en fazla 30 dk arayla) denenir.
        /// </summary>
        public static (CihazSenkronDurumu Durum, DateTime? SonrakiDeneme) SonrakiAdim(
            int denemeSayisi, CihazSenkronSonucTuru sonuc, DateTime simdi)
        {
            if (sonuc == CihazSenkronSonucTuru.Basarili)
                return (CihazSenkronDurumu.Tamamlandi, null);

            if (sonuc == CihazSenkronSonucTuru.Cakisma)
                return (CihazSenkronDurumu.Basarisiz, null);

            if (sonuc == CihazSenkronSonucTuru.Kalici && denemeSayisi >= MaksKaliciDeneme)
                return (CihazSenkronDurumu.Basarisiz, null);

            return (CihazSenkronDurumu.Bekliyor, simdi + GonderimKuyruguKurallari.Bekleme(denemeSayisi));
        }
    }
}
