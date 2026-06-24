using Microsoft.AspNetCore.Mvc.Rendering;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Shared.Enums;

namespace OgrenciBilgiSistemi.ViewModels
{
    public class OgrenciListeVm
    {
        public SayfalanmisListeModel<OgrenciModel> Page { get; set; } = default!;

        public List<SelectListItem> Birimler { get; set; } = new();

        public string? CurrentFilter { get; set; }

        public string? CurrentSort { get; set; }

        public int? BirimId { get; set; }

        public OgrenciFiltre Durum { get; set; } = OgrenciFiltre.Aktif;

        public Dictionary<int, bool> YemekDurumMap { get; set; } = new();
    }
}
