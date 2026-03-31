using OgrenciBilgiSistemi.Models;

namespace OgrenciBilgiSistemi.ViewModels
{
    public class VeliDetayVm
    {
        public VeliProfilModel Veli { get; set; } = null!;
        public List<OgrenciModel> Ogrenciler { get; set; } = new();
    }
}
