using OgrenciBilgiSistemi.Models;

namespace OgrenciBilgiSistemi.ViewModels
{
    public class KitapIndexVm
    {
        public SayfalanmisListeModel<KitapModel> Kitaplar { get; init; } = default!;
        public string? AramaMetni { get; init; }
        public string? Siralama { get; init; }
    }
}
