using System.ComponentModel.DataAnnotations;

namespace OgrenciBilgiSistemi.Shared.Enums
{
    public enum BildirimTuru
    {
        [Display(Name = "Randevu Oluşturuldu")]
        RandevuOlusturuldu = 1,

        [Display(Name = "Randevu Onaylandı")]
        RandevuOnaylandi = 2,

        [Display(Name = "Randevu Reddedildi")]
        RandevuReddedildi = 3,

        [Display(Name = "Randevu İptal Edildi")]
        RandevuIptalEdildi = 4,

        [Display(Name = "Randevu Hatırlatma")]
        RandevuHatirlatma = 5,

        [Display(Name = "Duyuru Yayınlandı")]
        DuyuruYayinlandi = 6
    }
}
