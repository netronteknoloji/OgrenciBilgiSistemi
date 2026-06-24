using OgrenciBilgiSistemi.Api.Models;

namespace OgrenciBilgiSistemi.Api.Services.Interfaces
{
    public interface IGirisService
    {
        Task<KullaniciModel?> KimlikDogrulaAsync(string kullaniciAdi, string sifre, string connectionString);
        Task<KullaniciModel?> KimlikDogrulaAsync_IdIle(int kullaniciId, string connectionString);
        Task<bool> SifreDegistirAsync(int kullaniciId, string yeniSifre);
        Task<List<string>> KullaniciAdiAraAsync(string aranan, string connectionString);
    }
}
