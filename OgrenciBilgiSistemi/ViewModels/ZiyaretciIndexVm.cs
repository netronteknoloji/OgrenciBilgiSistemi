using Microsoft.AspNetCore.Mvc.Rendering;
using OgrenciBilgiSistemi.Models;

namespace OgrenciBilgiSistemi.ViewModels
{
    public class ZiyaretciIndexVm
    {
        public SayfalanmisListeModel<ZiyaretciModel> Ziyaretciler { get; init; } = default!;
        public string? AramaMetni { get; init; }
        public int? KullaniciId { get; init; }
        public bool SadeceAktif { get; init; } = true;
        public List<SelectListItem> Kullanicilar { get; init; } = [];
    }
}
