using Microsoft.AspNetCore.Mvc.Rendering;
using OgrenciBilgiSistemi.Dtos;

namespace OgrenciBilgiSistemi.ViewModels
{
    public sealed class ZiyaretciRaporVm
    {
        // --- Filtre Alanları ---
        public string? query { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? birimId { get; set; }
        public int? kullaniciId { get; set; }
        public string? kartNo { get; set; }

        // --- Dropdownlar ---
        public List<SelectListItem> Birimler { get; set; } = new();
        public List<SelectListItem> Kullanicilar { get; set; } = new();

        // --- Rapor Sonucu ---
        public List<ZiyaretciRaporDto> Rapor { get; set; } = new();
    }
}
