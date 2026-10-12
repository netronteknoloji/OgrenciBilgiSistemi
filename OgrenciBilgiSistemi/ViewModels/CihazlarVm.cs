using OgrenciBilgiSistemi.Dtos;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Services.Interfaces;

namespace OgrenciBilgiSistemi.ViewModels
{
    public class CihazIndexVm
    {
        public SayfalanmisListeModel<CihazModel> Cihazlar { get; init; } = default!;
        public string? CurrentFilter { get; init; }
        public Dictionary<int, CihazSenkronOzetDto> SenkronOzetleri { get; init; } = new();
    }

    public class CihazKullanicilariVm
    {
        public List<ZkUserDto> Kullanicilar { get; init; } = [];
        public int CihazId { get; init; }
    }
}
