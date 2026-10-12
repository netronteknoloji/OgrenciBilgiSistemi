using OgrenciBilgiSistemi.Shared.Enums;
using OgrenciBilgiSistemi.Shared.Kurallar;

namespace OgrenciBilgiSistemi.Tests
{
    public class CihazSenkronKurallariTests
    {
        private static readonly DateTime Simdi = new(2026, 10, 12, 9, 0, 0);

        [Theory]
        [InlineData(IstasyonTipi.AnaKapi, true, false, true)]
        [InlineData(IstasyonTipi.AnaKapi, true, true, true)]
        [InlineData(IstasyonTipi.AnaKapi, false, true, false)]
        [InlineData(IstasyonTipi.Yemekhane, true, true, true)]
        [InlineData(IstasyonTipi.Yemekhane, true, false, false)]
        [InlineData(IstasyonTipi.Yemekhane, false, true, false)]
        public void Ogrenci_istasyona_gore_cihazda_tutulur(
            IstasyonTipi istasyon, bool aktif, bool yemekVar, bool beklenen)
            => Assert.Equal(beklenen, CihazSenkronKurallari.CihazdaOlmaliMi(istasyon, aktif, yemekVar));

        [Fact]
        public void Yemek_hakki_kalkinca_ana_kapidan_silinmez()
            => Assert.True(CihazSenkronKurallari.CihazdaOlmaliMi(IstasyonTipi.AnaKapi, ogrenciAktif: true, buAyYemekVar: false));

        [Fact]
        public void Basarili_senkron_tamamlanir()
        {
            var (durum, sonraki) = CihazSenkronKurallari.SonrakiAdim(1, CihazSenkronSonucTuru.Basarili, Simdi);
            Assert.Equal(CihazSenkronDurumu.Tamamlandi, durum);
            Assert.Null(sonraki);
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(5, 15)]
        [InlineData(50, 30)]
        public void Cihaz_kapaliyken_vazgecilmeden_artan_beklemeyle_denenir(int deneme, int dakika)
        {
            var (durum, sonraki) = CihazSenkronKurallari.SonrakiAdim(deneme, CihazSenkronSonucTuru.Gecici, Simdi);
            Assert.Equal(CihazSenkronDurumu.Bekliyor, durum);
            Assert.Equal(Simdi.AddMinutes(dakika), sonraki);
        }

        [Fact]
        public void Elle_eklenmis_kullaniciyla_cakisma_tekrar_denenmez()
        {
            var (durum, sonraki) = CihazSenkronKurallari.SonrakiAdim(1, CihazSenkronSonucTuru.Cakisma, Simdi);
            Assert.Equal(CihazSenkronDurumu.Basarisiz, durum);
            Assert.Null(sonraki);
        }

        [Fact]
        public void Cihazin_reddettigi_islem_birkac_denemeden_sonra_basarisiz_olur()
        {
            var (once, _) = CihazSenkronKurallari.SonrakiAdim(
                CihazSenkronKurallari.MaksKaliciDeneme - 1, CihazSenkronSonucTuru.Kalici, Simdi);
            var (son, sonraki) = CihazSenkronKurallari.SonrakiAdim(
                CihazSenkronKurallari.MaksKaliciDeneme, CihazSenkronSonucTuru.Kalici, Simdi);

            Assert.Equal(CihazSenkronDurumu.Bekliyor, once);
            Assert.Equal(CihazSenkronDurumu.Basarisiz, son);
            Assert.Null(sonraki);
        }
    }
}
