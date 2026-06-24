using OgrenciBilgiSistemi.Models;

namespace OgrenciBilgiSistemi.ViewModels
{
    public class DuyuruIndexVm
    {
        public SayfalanmisListeModel<DuyuruModel> Duyurular { get; init; } = default!;
    }
}
