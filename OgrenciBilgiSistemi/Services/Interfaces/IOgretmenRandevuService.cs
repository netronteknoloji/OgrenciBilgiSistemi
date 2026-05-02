using OgrenciBilgiSistemi.Models;

namespace OgrenciBilgiSistemi.Services.Interfaces
{
    public interface IOgretmenRandevuService
    {
        Task<List<OgretmenRandevuModel>> OgretmeneGoreListele(int ogretmenKullaniciId, CancellationToken ct = default);
        Task<OgretmenRandevuModel?> Getir(int ogretmenRandevuId, CancellationToken ct = default);
        Task Ekle(OgretmenRandevuModel model, CancellationToken ct = default);
        Task Guncelle(OgretmenRandevuModel model, CancellationToken ct = default);
        Task Sil(int ogretmenRandevuId, CancellationToken ct = default);
    }
}
