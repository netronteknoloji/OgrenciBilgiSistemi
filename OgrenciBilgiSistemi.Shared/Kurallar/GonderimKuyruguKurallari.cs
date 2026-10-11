using OgrenciBilgiSistemi.Shared.Enums;

namespace OgrenciBilgiSistemi.Shared.Kurallar
{
    public enum GonderimSonucTuru
    {
        Basarili,
        Gecici,   // tekrar denenir (zaman aşımı, 5xx, ağ)
        Kalici    // tekrar denenmez (geçersiz numara, yetki)
    }

    /// <summary>
    /// SMS/push kuyruğunun (GonderimKuyrugu) ortak kuralları: tekil anahtarlar, geçerlilik ve tekrar deneme.
    /// MVC (kuyruğa alma + gönderici) ve API (kuyruğa alma) aynı kuralları kullanır.
    /// </summary>
    public static class GonderimKuyruguKurallari
    {
        public const int MaksDeneme = 10;
        public const int TopluAlimBoyutu = 50;
        public static readonly TimeSpan KilitSuresi = TimeSpan.FromMinutes(5);
        public static readonly TimeSpan SaklamaSuresi = TimeSpan.FromDays(30);

        // Aynı kaynaktan ikinci kez kuyruğa alınmayı engelleyen anahtarlar (DB'de unique)
        public static string GecisAnahtari(int ogrenciDetayId) => $"GECIS:{ogrenciDetayId}";
        public static string SinifYoklamaAnahtari(int sinifYoklamaId, int dersNo) => $"SINIF:{sinifYoklamaId}:{dersNo}";
        public static string ServisYoklamaAnahtari(int servisYoklamaId, int durumId) => $"SERVIS:{servisYoklamaId}:{durumId}";

        /// <summary>SMS o günün sonuna kadar geçerlidir (ertesi gün "okula giriş yaptı" SMS'i gönderilmez).</summary>
        public static DateTime SmsGecerlilikBitis(DateTime olusturma) => olusturma.Date.AddDays(1);

        public static DateTime PushGecerlilikBitis(DateTime olusturma) => olusturma.AddDays(1);

        /// <summary>Geçici hatadan sonra bekleme: 1, 2, 4, 8, 15, sonra 30 dakika.</summary>
        public static TimeSpan Bekleme(int denemeSayisi) => denemeSayisi switch
        {
            <= 1 => TimeSpan.FromMinutes(1),
            2 => TimeSpan.FromMinutes(2),
            3 => TimeSpan.FromMinutes(4),
            4 => TimeSpan.FromMinutes(8),
            5 => TimeSpan.FromMinutes(15),
            _ => TimeSpan.FromMinutes(30)
        };

        /// <summary>Bir gönderim denemesinden sonra satırın yeni durumu ve (bekliyorsa) sonraki deneme zamanı.</summary>
        public static (GonderimDurumu Durum, DateTime? SonrakiDeneme) SonrakiAdim(
            int denemeSayisi, GonderimSonucTuru sonuc, DateTime simdi, DateTime gecerlilikBitis)
        {
            switch (sonuc)
            {
                case GonderimSonucTuru.Basarili:
                    return (GonderimDurumu.Gonderildi, null);
                case GonderimSonucTuru.Kalici:
                    return (GonderimDurumu.Basarisiz, null);
            }

            if (denemeSayisi >= MaksDeneme)
                return (GonderimDurumu.Basarisiz, null);

            var sonraki = simdi + Bekleme(denemeSayisi);
            return sonraki >= gecerlilikBitis
                ? (GonderimDurumu.SuresiDoldu, null)
                : (GonderimDurumu.Bekliyor, sonraki);
        }
    }
}
