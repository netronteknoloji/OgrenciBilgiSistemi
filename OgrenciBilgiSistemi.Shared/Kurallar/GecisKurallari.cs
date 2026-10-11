using OgrenciBilgiSistemi.Shared.Enums;
using OgrenciBilgiSistemi.Shared.Models;

namespace OgrenciBilgiSistemi.Shared.Kurallar
{
    /// <summary>
    /// Kart okutulduğunda öğrencinin o günkü durumu (DB'den toplanır).
    /// Yalnızca istasyonla ilgili alanlar anlamlıdır.
    /// </summary>
    public sealed record GecisDurumu(
        IstasyonTipi Istasyon,
        OglenCikisDurumu OgleIzni,
        DateTime Simdi,
        bool YemekIzniVar = false,
        bool BugunYemekVar = false,
        bool AnaKapiCikisVar = false,
        bool AnaKapiGirisVar = false,
        DateTime? AnaKapiCikisZamani = null);

    /// <summary>
    /// Okutma kararı. Kayıt yazılacaksa <see cref="ZorunluGecisTipi"/> null olabilir (yön kaydederken belirlenir);
    /// red ise kayıt ve SMS yoktur, ekranda <see cref="Reason"/>/<see cref="Mesaj"/> gösterilir.
    /// </summary>
    public sealed record GecisKarari(
        bool KayitYaz,
        string? ZorunluGecisTipi,
        string? Reason,
        string? Mesaj,
        DateTime? OncekiCikisZamani = null)
    {
        public static GecisKarari Kaydet(string? gecisTipi) => new(true, gecisTipi, null, null);
        public static GecisKarari Reddet(string reason, string mesaj, DateTime? oncekiCikis = null) =>
            new(false, null, reason, mesaj, oncekiCikis);
    }

    /// <summary>
    /// Ana kapı (öğle çıkışı/dönüşü) ve yemekhane geçiş kuralları. DB ve saat bağımlılığı yoktur;
    /// USB okuyucu ve ZKTeco yolu aynı kararı buradan alır.
    /// </summary>
    public static class GecisKurallari
    {
        public const string Giris = "Giriş";
        public const string Cikis = "Çıkış";

        public static class Mesajlar
        {
            public const string OglenIzniYok = "ÖĞRENCİNİN ÖĞLE ÇIKIŞ İZNİ YOK!";
            public const string YemekIzniYok = "ÖĞRENCİNİN YEMEKHANE GEÇİŞ İZNİ YOK!";
            public const string YemekLimit = "Bugün için yemekhane geçiş hakkı kullanıldı!";
            public const string OgleDonusLimit = "Bugün öğle çıkışı ve dönüşü zaten yapılmış!";
            public const string KartTanimsiz = "KART TANIMSIZ!";
            public const string OgleCikisOk = "ÖĞLE ÇIKIŞI ONAYLANDI.";
            public const string OgleDonusOk = "ÖĞLE DÖNÜŞÜ ONAYLANDI.";
            public const string YemekOk = "YEMEKHANE GEÇİŞİ ONAYLANDI.";
            public const string GenelOk = "Geçiş başarılı.";
        }

        public static GecisKarari KararVer(GecisDurumu d, KartOkumaAyarlari ayar)
        {
            switch (d.Istasyon)
            {
                case IstasyonTipi.Yemekhane:
                    if (!d.YemekIzniVar)
                        return GecisKarari.Reddet("YEMEKHANE_RED", Mesajlar.YemekIzniYok);
                    if (d.BugunYemekVar)
                        return GecisKarari.Reddet("YEMEKHANE_LIMIT", Mesajlar.YemekLimit);
                    return GecisKarari.Kaydet(Giris);

                case IstasyonTipi.AnaKapi:
                    if (d.OgleIzni == OglenCikisDurumu.Hayir)
                        return GecisKarari.Reddet("ANA_KAPI_OGLE_RED", Mesajlar.OglenIzniYok);
                    return AnaKapiOgle(d, ayar);

                default:
                    return GecisKarari.Kaydet(null);
            }
        }

        // İzinli öğrenci: günde bir öğle çıkışı (saat penceresinde) ve bir dönüş
        private static GecisKarari AnaKapiOgle(GecisDurumu d, KartOkumaAyarlari ayar)
        {
            if (!d.AnaKapiCikisVar)
            {
                return ayar.OgleCikisSaatiMi(d.Simdi)
                    ? GecisKarari.Kaydet(Cikis)
                    : GecisKarari.Reddet("ANA_KAPI_OGLE_SAAT_DISI",
                        $"ÖĞLE ÇIKIŞ SAATİ DEĞİL ({ayar.OgleCikisAraligiMetni})!");
            }

            if (!d.AnaKapiGirisVar)
            {
                // Çıkıştan hemen sonra tekrar okutma (çift okutma) dönüş sayılmaz
                if (d.AnaKapiCikisZamani is { } cikis && d.Simdi - cikis < ayar.OgleDonusMinSure)
                    return GecisKarari.Reddet("ANA_KAPI_OGLE_CIKIS_YAPILDI",
                        $"Öğle çıkışı zaten yapıldı ({cikis:HH:mm}).", cikis);

                return GecisKarari.Kaydet(Giris);
            }

            return GecisKarari.Reddet("ANA_KAPI_OGLE_DONUS_LIMIT", Mesajlar.OgleDonusLimit);
        }

        /// <summary>Kayıt yazıldıktan sonra ekranda gösterilecek gerekçe kodu ve mesaj.</summary>
        public static (string Reason, string Mesaj) BasariMesaji(IstasyonTipi istasyon, string? gecisTipi)
        {
            if (istasyon == IstasyonTipi.Yemekhane)
                return ("YEMEKHANE_OK", Mesajlar.YemekOk);
            if (istasyon != IstasyonTipi.AnaKapi)
                return ("GENEL_OK", Mesajlar.GenelOk);
            if (string.Equals(gecisTipi, Cikis, StringComparison.OrdinalIgnoreCase))
                return ("ANA_KAPI_OGLE_OK", Mesajlar.OgleCikisOk);
            if (string.Equals(gecisTipi, Giris, StringComparison.OrdinalIgnoreCase))
                return ("ANA_KAPI_OGLE_DONUS_OK", Mesajlar.OgleDonusOk);
            return ("GENEL_OK", Mesajlar.GenelOk);
        }
    }
}
