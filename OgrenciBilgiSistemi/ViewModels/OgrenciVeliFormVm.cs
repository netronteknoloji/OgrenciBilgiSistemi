using Microsoft.AspNetCore.Mvc.Rendering;
using OgrenciBilgiSistemi.Models;

namespace OgrenciBilgiSistemi.ViewModels
{
    public class OgrenciVeliFormVm
    {
        public OgrenciModel Ogrenci { get; set; } = new();

        // Yemekhane (bu ay)
        public bool? BuAyYemekhaneAktif { get; set; } = true;

        // DropDown listeler
        public List<SelectListItem> Ogretmenler { get; set; } = new();
        public List<SelectListItem> Birimler { get; set; } = new();
        public List<SelectListItem> Servisler { get; set; } = new();
        public List<SelectListItem> Veliler { get; set; } = new();

        // Form davranışı
        public string Action { get; set; } = "Ekle";
        public string SubmitText { get; set; } = "Kaydet";
        public bool IncludeId { get; set; } = false;
    }
}
