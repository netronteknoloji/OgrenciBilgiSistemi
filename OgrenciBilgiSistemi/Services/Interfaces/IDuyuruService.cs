using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Shared.Enums;

namespace OgrenciBilgiSistemi.Services.Interfaces
{
    public interface IDuyuruService
    {
        Task<int> Olustur(int olusturanId, DuyuruHedefi hedef, string baslik, string icerik, CancellationToken ct = default);

        Task<SayfalanmisListeModel<DuyuruModel>> Listele(int sayfaNo, int sayfaBoyutu = 20, CancellationToken ct = default);

        Task<DuyuruModel?> IdIleGetir(int duyuruId, CancellationToken ct = default);

        Task Sil(int duyuruId, CancellationToken ct = default);
    }
}
