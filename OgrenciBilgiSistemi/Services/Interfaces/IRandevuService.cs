using OgrenciBilgiSistemi.Models;

namespace OgrenciBilgiSistemi.Services.Interfaces
{
    public interface IRandevuService
    {
        Task<SayfalanmisListeModel<RandevuModel>> AraVeListele(
            string? arama, int? ogretmenId, RandevuDurumu? durum,
            DateTime? baslangic, DateTime? bitis,
            int sayfaNo, int sayfaBoyutu = 20, CancellationToken ct = default);

        Task<RandevuModel?> IdIleGetir(int randevuId, CancellationToken ct = default);

        Task IptalEt(int randevuId, CancellationToken ct = default);

        Task<List<RandevuModel>> ExcelListeleAsync(
            string? arama, int? ogretmenId, RandevuDurumu? durum,
            DateTime? baslangic, DateTime? bitis, CancellationToken ct = default);
    }
}
