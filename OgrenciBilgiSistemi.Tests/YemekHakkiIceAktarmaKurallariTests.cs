using OgrenciBilgiSistemi.Shared.Kurallar;

namespace OgrenciBilgiSistemi.Tests
{
    public class YemekHakkiIceAktarmaKurallariTests
    {
        // no 415 → öğrenci 1, no 215 → öğrenci 2, no 13 → öğrenci 3 ve 4 (aynı no)
        private static readonly IReadOnlyDictionary<int, IReadOnlyList<int>> Ogrenciler =
            new Dictionary<int, IReadOnlyList<int>>
            {
                [415] = [1],
                [215] = [2],
                [13] = [3, 4],
            };

        private static YemekHakkiEslesmeSonucu Esle(params string?[] hucreler)
            => YemekHakkiIceAktarmaKurallari.Esle(hucreler, Ogrenciler);

        [Fact]
        public void Baslik_satiri_atlanir_numaralar_eslenir()
        {
            var s = Esle("Öğrenci No", "415", "215");
            Assert.Equal([1, 2], s.AcilacakIdler);
            Assert.Empty(s.GecersizSatirlar);
        }

        [Fact]
        public void Basliksiz_dosya_da_okunur()
            => Assert.Equal([1], Esle("415").AcilacakIdler);

        [Theory]
        [InlineData(" 415 ")]
        [InlineData("415.0")]
        [InlineData("415.00")]
        public void Excel_sayi_bicimi_tam_sayiya_cevrilir(string hucre)
            => Assert.Equal([1], Esle(hucre).AcilacakIdler);

        [Fact]
        public void Bos_satirlar_atlanir()
        {
            var s = Esle("415", "", null, "  ", "215");
            Assert.Equal([1, 2], s.AcilacakIdler);
            Assert.Empty(s.GecersizSatirlar);
        }

        [Fact]
        public void Bilinmeyen_no_bulunamayan_olarak_raporlanir()
        {
            var s = Esle("415", "999");
            Assert.Equal([1], s.AcilacakIdler);
            Assert.Equal([999], s.Bulunamayan);
        }

        [Fact]
        public void Birden_cok_ogrenciye_eslesen_no_acilmaz()
        {
            var s = Esle("13", "415");
            Assert.Equal([1], s.AcilacakIdler);
            Assert.Equal([13], s.BirdenFazla);
        }

        [Fact]
        public void Tekrar_eden_no_bir_kez_sayilir()
        {
            var s = Esle("415", "415", "999", "999");
            Assert.Equal([1], s.AcilacakIdler);
            Assert.Equal([999], s.Bulunamayan);
        }

        [Theory]
        [InlineData("abc")]
        [InlineData("415.5")]
        [InlineData("-3")]
        [InlineData("0")]
        public void Sayi_olmayan_hucre_satir_noyla_gecersiz_sayilir(string hucre)
        {
            var s = Esle("Öğrenci No", "415", hucre);
            Assert.Equal([1], s.AcilacakIdler);
            Assert.Equal([3], s.GecersizSatirlar);
        }

        [Fact]
        public void Bos_dosyada_acilacak_yok()
        {
            var s = Esle("Öğrenci No");
            Assert.Empty(s.AcilacakIdler);
            Assert.Empty(s.GecersizSatirlar);
        }
    }
}
