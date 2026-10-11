using OgrenciBilgiSistemi.Shared.Services;

namespace OgrenciBilgiSistemi.Tests
{
    public class GirisDenemeSayaciTests
    {
        private sealed class ElleSaat(DateTimeOffset baslangic) : TimeProvider
        {
            public DateTimeOffset Simdi { get; set; } = baslangic;
            public override DateTimeOffset GetUtcNow() => Simdi;
        }

        private static readonly string Hesap = GirisDenemeSayaci.Anahtar("okul1", "veli");

        [Fact]
        public void Bes_hatali_denemeden_sonra_kilitlenir_ve_sure_dolunca_acilir()
        {
            var saat = new ElleSaat(new DateTimeOffset(2026, 10, 12, 8, 0, 0, TimeSpan.Zero));
            var sayac = new GirisDenemeSayaci(saat);

            for (int i = 0; i < GirisDenemeSayaci.MaksHataliDeneme - 1; i++)
                sayac.HataliKaydet(Hesap);
            Assert.False(sayac.KilitliMi(Hesap, out _));

            sayac.HataliKaydet(Hesap);
            Assert.True(sayac.KilitliMi(Hesap, out var kalan));
            Assert.Equal(GirisDenemeSayaci.KilitSuresi, kalan);

            saat.Simdi += GirisDenemeSayaci.KilitSuresi;
            Assert.False(sayac.KilitliMi(Hesap, out _));
        }

        [Fact]
        public void Basarili_giris_sayaci_sifirlar()
        {
            var sayac = new GirisDenemeSayaci(new ElleSaat(DateTimeOffset.UnixEpoch));
            for (int i = 0; i < GirisDenemeSayaci.MaksHataliDeneme - 1; i++)
                sayac.HataliKaydet(Hesap);

            sayac.Sifirla(Hesap);
            sayac.HataliKaydet(Hesap);

            Assert.False(sayac.KilitliMi(Hesap, out _));
        }

        [Fact]
        public void Pencere_disindaki_eski_hatalar_sayilmaz()
        {
            var saat = new ElleSaat(DateTimeOffset.UnixEpoch);
            var sayac = new GirisDenemeSayaci(saat);
            for (int i = 0; i < GirisDenemeSayaci.MaksHataliDeneme - 1; i++)
                sayac.HataliKaydet(Hesap);

            saat.Simdi += GirisDenemeSayaci.Pencere + TimeSpan.FromSeconds(1);
            sayac.HataliKaydet(Hesap);

            Assert.False(sayac.KilitliMi(Hesap, out _));
        }

        [Fact]
        public void Hesaplar_birbirini_etkilemez_ve_anahtar_buyuk_kucuk_harf_duyarsizdir()
        {
            var sayac = new GirisDenemeSayaci(new ElleSaat(DateTimeOffset.UnixEpoch));
            for (int i = 0; i < GirisDenemeSayaci.MaksHataliDeneme; i++)
                sayac.HataliKaydet(GirisDenemeSayaci.Anahtar("OKUL1", " Veli "));

            Assert.True(sayac.KilitliMi(Hesap, out _));
            Assert.False(sayac.KilitliMi(GirisDenemeSayaci.Anahtar("okul2", "veli"), out _));
            Assert.False(sayac.KilitliMi(GirisDenemeSayaci.Anahtar("okul1", "ogretmen"), out _));
        }
    }
}
