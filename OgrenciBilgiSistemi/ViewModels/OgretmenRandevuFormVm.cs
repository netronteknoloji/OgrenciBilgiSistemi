using Microsoft.AspNetCore.Mvc.Rendering;
using OgrenciBilgiSistemi.Models;

namespace OgrenciBilgiSistemi.ViewModels
{
    public class OgretmenRandevuFormVm
    {
        public int OgretmenRandevuId { get; set; }
        public int OgretmenKullaniciId { get; set; }
        public DateTime Tarih { get; set; } = DateTime.Today;
        public TimeSpan BaslangicSaati { get; set; }
        public TimeSpan BitisSaati { get; set; }
        public List<SelectListItem> Ogretmenler { get; set; } = [];

        public static OgretmenRandevuFormVm FromModel(OgretmenRandevuModel m) => new()
        {
            OgretmenRandevuId = m.OgretmenRandevuId,
            OgretmenKullaniciId = m.OgretmenKullaniciId,
            Tarih = m.Tarih,
            BaslangicSaati = m.BaslangicSaati,
            BitisSaati = m.BitisSaati,
        };

        public OgretmenRandevuModel ToModel() => new()
        {
            OgretmenRandevuId = OgretmenRandevuId,
            OgretmenKullaniciId = OgretmenKullaniciId,
            Tarih = Tarih,
            BaslangicSaati = BaslangicSaati,
            BitisSaati = BitisSaati,
        };
    }
}
