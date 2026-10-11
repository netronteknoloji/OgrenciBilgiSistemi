using OgrenciBilgiSistemi.Shared.Enums;
using OgrenciBilgiSistemi.Shared.Kurallar;

namespace OgrenciBilgiSistemi.Tests
{
    public class GonderimKuyruguKurallariTests
    {
        private static readonly DateTime Simdi = new(2026, 10, 12, 9, 0, 0);
        private static readonly DateTime GunSonu = GonderimKuyruguKurallari.SmsGecerlilikBitis(Simdi);

        [Fact]
        public void Basarili_gonderim_tamamlanir()
            => Assert.Equal((GonderimDurumu.Gonderildi, (DateTime?)null),
                GonderimKuyruguKurallari.SonrakiAdim(1, GonderimSonucTuru.Basarili, Simdi, GunSonu));

        [Fact]
        public void Kalici_hata_tekrar_denenmez()
            => Assert.Equal(GonderimDurumu.Basarisiz,
                GonderimKuyruguKurallari.SonrakiAdim(1, GonderimSonucTuru.Kalici, Simdi, GunSonu).Durum);

        [Theory]
        [InlineData(1, 1)]
        [InlineData(2, 2)]
        [InlineData(3, 4)]
        [InlineData(4, 8)]
        [InlineData(5, 15)]
        [InlineData(9, 30)]
        public void Gecici_hata_artan_beklemeyle_tekrar_denenir(int deneme, int dakika)
        {
            var (durum, sonraki) = GonderimKuyruguKurallari.SonrakiAdim(deneme, GonderimSonucTuru.Gecici, Simdi, GunSonu);
            Assert.Equal(GonderimDurumu.Bekliyor, durum);
            Assert.Equal(Simdi.AddMinutes(dakika), sonraki);
        }

        [Fact]
        public void Maks_denemeden_sonra_vazgecilir()
            => Assert.Equal(GonderimDurumu.Basarisiz,
                GonderimKuyruguKurallari.SonrakiAdim(GonderimKuyruguKurallari.MaksDeneme, GonderimSonucTuru.Gecici, Simdi, GunSonu).Durum);

        [Fact]
        public void Sms_gun_sonundan_sonra_denenmez()
        {
            var gece = new DateTime(2026, 10, 12, 23, 50, 0);
            var (durum, _) = GonderimKuyruguKurallari.SonrakiAdim(6, GonderimSonucTuru.Gecici, gece,
                GonderimKuyruguKurallari.SmsGecerlilikBitis(gece));
            Assert.Equal(GonderimDurumu.SuresiDoldu, durum);
        }

        [Fact]
        public void Tekil_anahtarlar_kaynaga_ozeldir()
        {
            Assert.Equal("GECIS:15", GonderimKuyruguKurallari.GecisAnahtari(15));
            Assert.NotEqual(GonderimKuyruguKurallari.SinifYoklamaAnahtari(7, 1), GonderimKuyruguKurallari.SinifYoklamaAnahtari(7, 2));
            Assert.NotEqual(GonderimKuyruguKurallari.ServisYoklamaAnahtari(7, 1), GonderimKuyruguKurallari.ServisYoklamaAnahtari(7, 2));
        }
    }
}
