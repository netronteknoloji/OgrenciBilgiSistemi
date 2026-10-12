using OgrenciBilgiSistemi.Shared.Enums;
using OgrenciBilgiSistemi.Shared.Kurallar;

namespace OgrenciBilgiSistemi.Tests
{
    public class PanelOturumKurallariTests
    {
        [Theory]
        [InlineData(KullaniciRolu.Admin, false, false, true)]
        [InlineData(KullaniciRolu.Ogretmen, false, false, true)]
        [InlineData(KullaniciRolu.Ogretmen, false, true, false)]
        [InlineData(KullaniciRolu.Admin, true, false, false)]
        [InlineData(KullaniciRolu.Ogretmen, true, false, false)]
        [InlineData(KullaniciRolu.GenelAdmin, false, false, false)]
        public void Hesap_durumu_rol_ve_profile_gore_belirlenir(
            KullaniciRolu rol, bool kullaniciSilinmis, bool ogretmenProfilSilinmis, bool beklenen)
            => Assert.Equal(beklenen, PanelOturumKurallari.HesapAktifMi(rol, kullaniciSilinmis, ogretmenProfilSilinmis));

        [Theory]
        [InlineData(KullaniciRolu.Admin)]
        [InlineData(KullaniciRolu.Servis)]
        [InlineData(KullaniciRolu.Veli)]
        public void Pasif_ogretmen_profili_ogretmen_olmayan_hesabi_engellemez(KullaniciRolu rol)
            => Assert.True(PanelOturumKurallari.HesapAktifMi(rol, kullaniciSilinmis: false, ogretmenProfilSilinmis: true));

        [Theory]
        [InlineData(KullaniciRolu.Admin, true)]
        [InlineData(KullaniciRolu.Ogretmen, true)]
        [InlineData(KullaniciRolu.Servis, false)]
        [InlineData(KullaniciRolu.Veli, false)]
        [InlineData(KullaniciRolu.GenelAdmin, false)]
        public void Panele_yalnizca_yonetici_ve_ogretmen_girer(KullaniciRolu rol, bool beklenen)
            => Assert.Equal(beklenen, PanelOturumKurallari.PaneleGirebilirMi(rol));
    }
}
