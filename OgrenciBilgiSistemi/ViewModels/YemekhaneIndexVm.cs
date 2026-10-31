using Microsoft.AspNetCore.Mvc.Rendering;
using OgrenciBilgiSistemi.Dtos;

namespace OgrenciBilgiSistemi.ViewModels
{
    public sealed class YemekhaneIndexVm
    {
        // Filtreler (yil/ay = takvim yılı/ayı)
        public string? query { get; set; }
        public int yil { get; set; }
        public int ay { get; set; }
        public int? birimId { get; set; }
        public YemekHakkiFiltresi durum { get; set; } = YemekHakkiFiltresi.Hepsi;
        public bool includePasif { get; set; }

        // <input type="month"> değeri ve başlık (ör. "Kasım 2026")
        public string AyDegeri => $"{yil:D4}-{ay:D2}";
        public string AyAdi { get; set; } = "";

        // Dropdown kaynakları
        public IEnumerable<SelectListItem> Birimler { get; set; } = Enumerable.Empty<SelectListItem>();
        public IEnumerable<SelectListItem> Durumlar { get; set; } = Enumerable.Empty<SelectListItem>();

        // Liste + sayfalama
        public SayfalanmisListeModel<YemekhaneIndexSatirDto> Satirlar { get; set; } = null!;

        // Üst özet (durum filtresinden önce)
        public int HakVarSayisi { get; set; }
        public int HakYokSayisi { get; set; }
    }
}
