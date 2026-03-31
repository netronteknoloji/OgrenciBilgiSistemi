using OgrenciBilgiSistemi.Models;

namespace OgrenciBilgiSistemi.ViewModels
{
    public class ZiyaretciDetayViewModel
    {
        public ZiyaretciModel Ziyaretci { get; set; } = null!;
        public List<ZiyaretciModel> Ziyaretler { get; set; } = new();
    }

}

