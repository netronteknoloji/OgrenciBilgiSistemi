using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using OgrenciBilgiSistemi.Dtos;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace OgrenciBilgiSistemi.ViewModels
{

    public sealed class AidatRaporVm
    {
        // Filtreler
        public string? query { get; set; }
        public int? yil { get; set; }
        public int? birimId { get; set; }
        public RaporDurumFiltresiDto durum { get; set; } = RaporDurumFiltresiDto.Hepsi;
        public DateTime? bas { get; set; }
        public DateTime? bit { get; set; }
        public bool includePasif { get; set; }

        // Dropdown kaynakları
        public IEnumerable<SelectListItem> Yillar { get; set; } = Enumerable.Empty<SelectListItem>();
        public IEnumerable<SelectListItem> Birimler { get; set; } = Enumerable.Empty<SelectListItem>();
        public IEnumerable<SelectListItem> Durumlar { get; set; } = Enumerable.Empty<SelectListItem>();

        // Liste + sayfalama (TEK KAYNAK)
        public SayfalanmisListeModel<AidatRaporDto> Satirlar { get; set; } = null!;

        // Üst özetler (tüm data üzerinden)
        public decimal ToplamBorc { get; set; }
        public decimal ToplamOdenenGosterilen { get; set; }
        public decimal ToplamKalan { get; set; }

        public List<int> KullanilabilirYillar { get; set; } = new();
    }
}