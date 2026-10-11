using System.Globalization;

namespace OgrenciBilgiSistemi.Models
{
    /// <summary>
    /// appsettings.json → "KartOkuma". Ana kapı öğle çıkış kuralları.
    /// Bölüm yoksa veya değer geçersizse varsayılanlar kullanılır.
    /// </summary>
    public class KartOkumaAyarlari
    {
        public string OgleCikisBaslangic { get; set; } = "11:30";
        public string OgleCikisBitis { get; set; } = "13:30";

        // Öğle çıkışından sonra bu süre dolmadan yapılan okutma dönüş sayılmaz (yanlışlıkla çift okutma)
        public int OgleDonusMinDakika { get; set; } = 10;

        public TimeSpan OgleCikisBaslangicSaati => SaatCoz(OgleCikisBaslangic, new TimeSpan(11, 30, 0));
        public TimeSpan OgleCikisBitisSaati => SaatCoz(OgleCikisBitis, new TimeSpan(13, 30, 0));
        public TimeSpan OgleDonusMinSure => TimeSpan.FromMinutes(OgleDonusMinDakika > 0 ? OgleDonusMinDakika : 10);

        public bool OgleCikisSaatiMi(DateTime zaman)
        {
            var saat = zaman.TimeOfDay;
            return saat >= OgleCikisBaslangicSaati && saat <= OgleCikisBitisSaati;
        }

        public string OgleCikisAraligiMetni => $"{OgleCikisBaslangicSaati:hh\\:mm}-{OgleCikisBitisSaati:hh\\:mm}";

        private static TimeSpan SaatCoz(string? deger, TimeSpan varsayilan) =>
            TimeSpan.TryParseExact(deger, @"hh\:mm", CultureInfo.InvariantCulture, out var ts) ? ts : varsayilan;
    }
}
