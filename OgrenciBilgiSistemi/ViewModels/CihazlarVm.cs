using OgrenciBilgiSistemi.Dtos;
using OgrenciBilgiSistemi.Models;

namespace OgrenciBilgiSistemi.ViewModels
{
    public class CihazIndexVm
    {
        public SayfalanmisListeModel<CihazModel> Cihazlar { get; init; } = default!;
        public string? CurrentFilter { get; init; }
    }

    public class CihazKullanicilariVm
    {
        public List<ZkUserDto> Kullanicilar { get; init; } = [];
        public int CihazId { get; init; }
    }
}
