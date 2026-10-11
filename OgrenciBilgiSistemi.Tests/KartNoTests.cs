using OgrenciBilgiSistemi.Shared.Helpers;

namespace OgrenciBilgiSistemi.Tests
{
    public class KartNoTests
    {
        [Theory]
        [InlineData(null, null)]
        [InlineData("", null)]
        [InlineData("   ", null)]
        [InlineData("123", "123")]
        [InlineData(" 00123 ", "123")]
        [InlineData("000", "0")]
        [InlineData("0", "0")]
        [InlineData("1020", "1020")]
        public void Normalize_kaydetme_ve_okutmada_ayni_bicimi_uretir(string? girdi, string? beklenen)
            => Assert.Equal(beklenen, KartNo.Normalize(girdi));
    }
}
