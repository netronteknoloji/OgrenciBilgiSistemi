using OgrenciBilgiSistemi.Shared.Enums;
using OgrenciBilgiSistemi.Shared.Kurallar;
using OgrenciBilgiSistemi.Shared.Models;

namespace OgrenciBilgiSistemi.Tests
{
    public class GecisKurallariTests
    {
        private static readonly KartOkumaAyarlari Varsayilan = new();
        private static DateTime Saat(int saat, int dakika) => new(2026, 10, 12, saat, dakika, 0);

        private static GecisKarari AnaKapi(DateTime simdi, OglenCikisDurumu izin = OglenCikisDurumu.Evet,
            bool cikisVar = false, bool girisVar = false, DateTime? cikisZamani = null, KartOkumaAyarlari? ayar = null)
            => GecisKurallari.KararVer(
                new GecisDurumu(IstasyonTipi.AnaKapi, izin, simdi,
                    AnaKapiCikisVar: cikisVar, AnaKapiGirisVar: girisVar, AnaKapiCikisZamani: cikisZamani),
                ayar ?? Varsayilan);

        // ---------------- Yemekhane ----------------

        [Fact]
        public void Yemekhane_izni_yoksa_reddedilir()
        {
            var k = GecisKurallari.KararVer(new GecisDurumu(IstasyonTipi.Yemekhane, OglenCikisDurumu.Hayir, Saat(12, 0)), Varsayilan);
            Assert.False(k.KayitYaz);
            Assert.Equal("YEMEKHANE_RED", k.Reason);
        }

        [Fact]
        public void Yemekhane_bugun_girdiyse_limit()
        {
            var k = GecisKurallari.KararVer(new GecisDurumu(IstasyonTipi.Yemekhane, OglenCikisDurumu.Hayir, Saat(12, 0),
                YemekIzniVar: true, BugunYemekVar: true), Varsayilan);
            Assert.False(k.KayitYaz);
            Assert.Equal("YEMEKHANE_LIMIT", k.Reason);
        }

        [Fact]
        public void Yemekhane_izinli_ilk_giris_kaydedilir()
        {
            var k = GecisKurallari.KararVer(new GecisDurumu(IstasyonTipi.Yemekhane, OglenCikisDurumu.Hayir, Saat(12, 0),
                YemekIzniVar: true), Varsayilan);
            Assert.True(k.KayitYaz);
            Assert.Equal(GecisKurallari.Giris, k.ZorunluGecisTipi);
        }

        // ---------------- Ana kapı ----------------

        [Fact]
        public void Ogle_izni_olmayan_ogrenci_reddedilir()
        {
            var k = AnaKapi(Saat(12, 0), OglenCikisDurumu.Hayir);
            Assert.False(k.KayitYaz);
            Assert.Equal("ANA_KAPI_OGLE_RED", k.Reason);
        }

        [Theory]
        [InlineData(11, 30)]
        [InlineData(12, 15)]
        [InlineData(13, 30)]
        public void Izinli_ogrenci_pencere_icinde_cikis_yapar(int saat, int dakika)
        {
            var k = AnaKapi(Saat(saat, dakika));
            Assert.True(k.KayitYaz);
            Assert.Equal(GecisKurallari.Cikis, k.ZorunluGecisTipi);
        }

        [Theory]
        [InlineData(8, 0)]
        [InlineData(11, 29)]
        [InlineData(13, 31)]
        [InlineData(16, 0)]
        public void Pencere_disinda_cikis_reddedilir(int saat, int dakika)
        {
            var k = AnaKapi(Saat(saat, dakika));
            Assert.False(k.KayitYaz);
            Assert.Equal("ANA_KAPI_OGLE_SAAT_DISI", k.Reason);
            Assert.Contains("11:30-13:30", k.Mesaj);
        }

        [Fact]
        public void Cikistan_hemen_sonra_okutma_donus_sayilmaz()
        {
            var cikis = Saat(12, 0);
            var k = AnaKapi(Saat(12, 5), cikisVar: true, cikisZamani: cikis);
            Assert.False(k.KayitYaz);
            Assert.Equal("ANA_KAPI_OGLE_CIKIS_YAPILDI", k.Reason);
            Assert.Equal(cikis, k.OncekiCikisZamani);
        }

        [Fact]
        public void Yeterli_sure_sonra_okutma_donus_kaydedilir()
        {
            var k = AnaKapi(Saat(12, 40), cikisVar: true, cikisZamani: Saat(12, 0));
            Assert.True(k.KayitYaz);
            Assert.Equal(GecisKurallari.Giris, k.ZorunluGecisTipi);
        }

        [Fact]
        public void Donus_icin_saat_siniri_yoktur()
        {
            var k = AnaKapi(Saat(15, 0), cikisVar: true, cikisZamani: Saat(12, 0));
            Assert.True(k.KayitYaz);
            Assert.Equal(GecisKurallari.Giris, k.ZorunluGecisTipi);
        }

        [Fact]
        public void Cikis_ve_donus_yapildiysa_reddedilir()
        {
            var k = AnaKapi(Saat(13, 0), cikisVar: true, girisVar: true, cikisZamani: Saat(12, 0));
            Assert.False(k.KayitYaz);
            Assert.Equal("ANA_KAPI_OGLE_DONUS_LIMIT", k.Reason);
        }

        [Fact]
        public void Ayarlar_degistirilebilir_gecersiz_degerde_varsayilan_kullanilir()
        {
            var ayar = new KartOkumaAyarlari { OgleCikisBaslangic = "12:00", OgleCikisBitis = "gecersiz", OgleDonusMinDakika = 0 };

            Assert.Equal("ANA_KAPI_OGLE_SAAT_DISI", AnaKapi(Saat(11, 45), ayar: ayar).Reason);
            Assert.True(AnaKapi(Saat(13, 15), ayar: ayar).KayitYaz);              // bitiş geçersiz → 13:30
            Assert.Equal("ANA_KAPI_OGLE_CIKIS_YAPILDI",                            // 0 dk geçersiz → 10 dk
                AnaKapi(Saat(12, 9), cikisVar: true, cikisZamani: Saat(12, 0), ayar: ayar).Reason);
        }

        // ---------------- Başarı mesajları ----------------

        [Theory]
        [InlineData(IstasyonTipi.Yemekhane, "Giriş", "YEMEKHANE_OK")]
        [InlineData(IstasyonTipi.AnaKapi, "Çıkış", "ANA_KAPI_OGLE_OK")]
        [InlineData(IstasyonTipi.AnaKapi, "Giriş", "ANA_KAPI_OGLE_DONUS_OK")]
        [InlineData(IstasyonTipi.AnaKapi, null, "GENEL_OK")]
        public void Basari_mesaji_istasyon_ve_yone_gore(IstasyonTipi istasyon, string? gecisTipi, string reason)
            => Assert.Equal(reason, GecisKurallari.BasariMesaji(istasyon, gecisTipi).Reason);
    }
}
