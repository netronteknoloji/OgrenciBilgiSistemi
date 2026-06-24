using OgrenciBilgiSistemi.Models;

namespace OgrenciBilgiSistemi.ViewModels
{
    public class BirimIndexVm
    {
        public SayfalanmisListeModel<BirimModel> Birimler { get; init; } = default!;
        public string? AramaMetni { get; init; }
        public BirimFiltre Durum { get; init; }
    }
}
