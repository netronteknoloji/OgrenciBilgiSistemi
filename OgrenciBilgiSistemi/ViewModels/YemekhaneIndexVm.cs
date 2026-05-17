using Microsoft.AspNetCore.Mvc.Rendering;
using OgrenciBilgiSistemi.Dtos;

namespace OgrenciBilgiSistemi.ViewModels
{
    public sealed class YemekhaneIndexVm
    {
        // Filtreler
        public string? query { get; set; }
        public int? yil { get; set; }
        public int? birimId { get; set; }
        public RaporDurumFiltresiDto durum { get; set; } = RaporDurumFiltresiDto.Hepsi;
        public bool includePasif { get; set; }

        // Dropdown kaynakları
        public IEnumerable<SelectListItem> Yillar { get; set; } = Enumerable.Empty<SelectListItem>();
        public IEnumerable<SelectListItem> Birimler { get; set; } = Enumerable.Empty<SelectListItem>();
        public IEnumerable<SelectListItem> Durumlar { get; set; } = Enumerable.Empty<SelectListItem>();

        // Liste + sayfalama
        public SayfalanmisListeModel<YemekhaneIndexSatirDto> Satirlar { get; set; } = null!;

        // Üst özetler (filtreli tüm data üzerinden)
        public decimal ToplamBorc { get; set; }
        public decimal ToplamOdenen { get; set; }
        public decimal ToplamKalan { get; set; }

        public List<int> KullanilabilirYillar { get; set; } = new();
    }
}
