using OgrenciBilgiSistemi.Models;

namespace OgrenciBilgiSistemi.Services.Interfaces
{
    public interface IKimlikDogrulamaService
    {
        Task<KullaniciModel?> DogrulaAsync(string connectionString, string kullaniciAdi, string sifre, CancellationToken ct = default);
        Task GenelAdminOlusturAsync(string connectionString, CancellationToken ct = default);
    }
}
