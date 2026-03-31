using Microsoft.AspNetCore.Mvc.Rendering;
using OgrenciBilgiSistemi.Dtos;

namespace OgrenciBilgiSistemi.ViewModels
{
    public class OgrenciVeliRaporVm
    {
        public string? query { get; set; }      
        public int? birimId { get; set; }
        public List<SelectListItem> Birimler { get; set; } = new();
        public SayfalanmisListeModel<OgrenciVeliRaporDto> Rapor { get; set; } = null!;
    }
}
