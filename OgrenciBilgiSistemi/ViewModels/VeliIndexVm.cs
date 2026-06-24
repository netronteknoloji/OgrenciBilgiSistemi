using OgrenciBilgiSistemi.Models;

namespace OgrenciBilgiSistemi.ViewModels
{
    public class VeliIndexVm
    {
        public SayfalanmisListeModel<VeliProfilModel> Veliler { get; init; } = default!;
        public string? AramaMetni { get; init; }
    }
}
