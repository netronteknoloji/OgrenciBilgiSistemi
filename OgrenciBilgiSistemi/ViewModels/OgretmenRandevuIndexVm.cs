using Microsoft.AspNetCore.Mvc.Rendering;
using OgrenciBilgiSistemi.Models;

namespace OgrenciBilgiSistemi.ViewModels
{
    public class OgretmenRandevuIndexVm
    {
        public List<OgretmenRandevuModel> Liste { get; init; } = [];
        public int? OgretmenId { get; init; }
        public List<SelectListItem> Ogretmenler { get; init; } = [];
    }
}
