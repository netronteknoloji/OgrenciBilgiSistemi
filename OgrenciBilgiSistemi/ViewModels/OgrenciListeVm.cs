using Microsoft.AspNetCore.Mvc.Rendering;
using OgrenciBilgiSistemi.Models;

namespace OgrenciBilgiSistemi.ViewModels
{
    public class OgrenciListeVm
    {
        public SayfalanmisListeModel<OgrenciModel> Page { get; set; } = default!;

        public List<SelectListItem> Birimler { get; set; } = new();

        public string? CurrentFilter { get; set; }

        public string? CurrentSort { get; set; }

        public int? BirimId { get; set; }
    }
}
