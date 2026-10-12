using OgrenciBilgiSistemi.Shared.Enums;

namespace OgrenciBilgiSistemi.Shared.Kurallar
{
    /// <summary>
    /// Web paneli hesap kuralları. Giriş (<c>KimlikDogrulamaService</c>) ve her istekteki oturum denetimi
    /// (<c>OturumDogrulayici</c>) aynı kararı buradan alır; ikisi ayrışırsa kullanıcı girer ama hemen dışarı atılır.
    /// </summary>
    public static class PanelOturumKurallari
    {
        /// <summary>
        /// Hesap oturum açabilir / sürdürebilir mi. Pasif öğretmen profili yalnızca Öğretmen rolünü engeller:
        /// rolü öğretmenlikten değişen kullanıcının eski profili pasife çekilir ama hesap aktif kalır.
        /// DB'deki GenelAdmin satırı oturum açamaz; Genel Yönetici yalnızca yapılandırmadan doğrulanır.
        /// </summary>
        public static bool HesapAktifMi(KullaniciRolu rol, bool kullaniciSilinmis, bool ogretmenProfilSilinmis)
            => !kullaniciSilinmis
               && rol != KullaniciRolu.GenelAdmin
               && !(rol == KullaniciRolu.Ogretmen && ogretmenProfilSilinmis);

        /// <summary>Veli ve Servis hesapları yalnızca mobil uygulama içindir.</summary>
        public static bool PaneleGirebilirMi(KullaniciRolu rol)
            => rol is KullaniciRolu.Admin or KullaniciRolu.Ogretmen;
    }
}
