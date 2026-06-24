using OgrenciBilgiSistemi.Shared.Models;

namespace OgrenciBilgiSistemi.ViewModels
{
    public class OkulSecimVm
    {
        public List<OkulBilgiAyari> Okullar { get; init; } = [];
        public string? SeciliOkulKodu { get; init; }
    }
}
