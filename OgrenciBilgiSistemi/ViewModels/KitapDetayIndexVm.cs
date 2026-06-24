using OgrenciBilgiSistemi.Models;

namespace OgrenciBilgiSistemi.ViewModels
{
    public class KitapDetayIndexVm
    {
        public SayfalanmisListeModel<KitapDetayModel> Detaylar { get; init; } = default!;
        public string? AramaMetni { get; init; }
        public string? Siralama { get; init; }
        public string? DurumFiltre { get; init; }
    }
}
