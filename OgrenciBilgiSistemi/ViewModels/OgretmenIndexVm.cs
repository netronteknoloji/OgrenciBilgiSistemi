using OgrenciBilgiSistemi.Models;

namespace OgrenciBilgiSistemi.ViewModels
{
    public class OgretmenIndexVm
    {
        public SayfalanmisListeModel<OgretmenProfilModel> Ogretmenler { get; init; } = default!;
        public string? AramaMetni { get; init; }
        public OgretmenFiltre Durum { get; init; }
    }
}
