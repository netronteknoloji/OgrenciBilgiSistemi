using OgrenciBilgiSistemi.Shared.Enums;
using OgrenciBilgiSistemi.Shared.Kurallar;

namespace OgrenciBilgiSistemi.Tests
{
    public class RandevuDurumKurallariTests
    {
        [Theory]
        [InlineData(RandevuDurumu.Beklemede, true)]
        [InlineData(RandevuDurumu.Onaylandi, false)]
        [InlineData(RandevuDurumu.Reddedildi, false)]
        [InlineData(RandevuDurumu.IptalEdildi, false)]
        [InlineData(RandevuDurumu.Tamamlandi, false)]
        public void Yalnizca_beklemedeki_randevu_onaylanir_veya_reddedilir(RandevuDurumu mevcut, bool beklenen)
            => Assert.Equal(beklenen, RandevuDurumKurallari.YanitlanabilirMi(mevcut));

        [Theory]
        [InlineData(RandevuDurumu.Beklemede, true)]
        [InlineData(RandevuDurumu.Onaylandi, true)]
        [InlineData(RandevuDurumu.Reddedildi, false)]
        [InlineData(RandevuDurumu.IptalEdildi, false)]
        [InlineData(RandevuDurumu.Tamamlandi, false)]
        public void Yalnizca_beklemede_veya_onayli_randevu_iptal_edilir(RandevuDurumu mevcut, bool beklenen)
            => Assert.Equal(beklenen, RandevuDurumKurallari.IptalEdilebilirMi(mevcut));

        [Fact]
        public void Sql_kosullari_enum_degerleriyle_uyumlu()
        {
            Assert.Equal(0, (int)RandevuDurumu.Beklemede);
            Assert.Equal(1, (int)RandevuDurumu.Onaylandi);
        }
    }
}
