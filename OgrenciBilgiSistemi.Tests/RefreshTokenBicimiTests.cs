using OgrenciBilgiSistemi.Shared.Helpers;

namespace OgrenciBilgiSistemi.Tests
{
    public class RefreshTokenBicimiTests
    {
        [Fact]
        public void Olusturulan_token_okul_kodunu_tasir_ve_her_seferinde_farklidir()
        {
            var a = RefreshTokenBicimi.Olustur("okul1");
            var b = RefreshTokenBicimi.Olustur("okul1");

            Assert.NotEqual(a, b);
            Assert.Equal("okul1", RefreshTokenBicimi.OkulKoduCoz(a));
            Assert.DoesNotContain('+', a);
            Assert.DoesNotContain('/', a);
            Assert.DoesNotContain('=', a);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("noktasiz")]
        [InlineData(".basinda")]
        [InlineData("sonunda.")]
        public void Gecersiz_bicimde_okul_kodu_cozulmez(string? token)
            => Assert.Null(RefreshTokenBicimi.OkulKoduCoz(token));

        [Fact]
        public void Hash_sabit_uzunlukta_ve_belirleyicidir()
        {
            var token = RefreshTokenBicimi.Olustur("okul1");
            var h = RefreshTokenBicimi.Hash(token);

            Assert.Equal(64, h.Length);
            Assert.Equal(h, RefreshTokenBicimi.Hash(token));
            Assert.NotEqual(h, RefreshTokenBicimi.Hash(token + "x"));
        }

        [Theory]
        [InlineData("")]
        [InlineData("a.b")]
        public void Gecersiz_okul_koduyla_olusturulamaz(string okulKodu)
            => Assert.Throws<ArgumentException>(() => RefreshTokenBicimi.Olustur(okulKodu));
    }
}
