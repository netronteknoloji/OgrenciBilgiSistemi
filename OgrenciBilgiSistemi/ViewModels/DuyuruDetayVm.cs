using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Shared.Enums;

namespace OgrenciBilgiSistemi.ViewModels
{
    public class DuyuruDetayVm
    {
        public int DuyuruId { get; init; }
        public string Baslik { get; init; } = string.Empty;
        public string Icerik { get; init; } = string.Empty;
        public DateTime OlusturulmaTarihi { get; init; }
        public string? OlusturanAdi { get; init; }
        public DuyuruHedefi Hedef { get; init; }

        public static DuyuruDetayVm FromModel(DuyuruModel m) => new()
        {
            DuyuruId = m.DuyuruId,
            Baslik = m.Baslik,
            Icerik = m.Icerik,
            OlusturulmaTarihi = m.OlusturulmaTarihi,
            OlusturanAdi = m.Olusturan?.KullaniciAdi,
            Hedef = m.Hedef,
        };
    }
}
