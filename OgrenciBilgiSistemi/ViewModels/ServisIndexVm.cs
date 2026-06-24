using OgrenciBilgiSistemi.Models;

namespace OgrenciBilgiSistemi.ViewModels
{
    public class ServisIndexVm
    {
        public SayfalanmisListeModel<ServisProfilModel> Servisler { get; init; } = default!;
        public string? AramaMetni { get; init; }
    }
}
