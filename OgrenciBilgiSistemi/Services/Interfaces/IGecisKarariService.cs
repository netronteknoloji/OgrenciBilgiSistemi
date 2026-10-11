using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Shared.Kurallar;

namespace OgrenciBilgiSistemi.Services.Interfaces
{
    public interface IGecisKarariService
    {
        /// <summary>Öğrencinin bugünkü durumunu DB'den toplayıp ortak geçiş kuralına göre karar verir.</summary>
        Task<GecisKarari> KararVerAsync(IstasyonTipi istasyon, OgrenciModel ogrenci, DateTime simdi, CancellationToken ct = default);
    }
}
