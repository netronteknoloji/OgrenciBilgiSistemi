using OgrenciBilgiSistemi.Shared.Dtos;

namespace OgrenciBilgiSistemi.Api.Services.Interfaces
{
    public interface IGecisKayitService
    {
        Task<List<GecisKayitDto>> GetListAsync(DateTime? baslangic, DateTime? bitis, string? arama, int? sinifId, int? veliId = null, int? servisId = null, int pageNumber = 1, int pageSize = 100);
        Task<List<GecisKayitDto>> GetByOgrenciIdAsync(int ogrenciId, DateTime? baslangic = null, DateTime? bitis = null);
    }
}
