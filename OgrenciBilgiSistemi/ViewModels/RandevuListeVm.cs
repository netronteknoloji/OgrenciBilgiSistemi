using Microsoft.AspNetCore.Mvc.Rendering;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Shared.Enums;

namespace OgrenciBilgiSistemi.ViewModels
{
    public class RandevuListeVm
    {
        public SayfalanmisListeModel<RandevuModel> Randevular { get; init; } = default!;
        public string? Arama { get; init; }
        public int? OgretmenId { get; init; }
        public RandevuDurumu? Durum { get; init; }
        public string? Baslangic { get; init; }
        public string? Bitis { get; init; }
        public List<SelectListItem> Ogretmenler { get; init; } = [];
    }
}
