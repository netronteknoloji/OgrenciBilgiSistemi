using OgrenciBilgiSistemi.Models;

namespace OgrenciBilgiSistemi.ViewModels
{
    public class KullaniciIndexVm
    {
        public SayfalanmisListeModel<KullaniciModel> Kullanicilar { get; init; } = default!;
        public string? AramaMetni { get; init; }
    }
}
