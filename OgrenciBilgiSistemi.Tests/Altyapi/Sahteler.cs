using Microsoft.AspNetCore.Http;
using OgrenciBilgiSistemi.Abstractions;
using OgrenciBilgiSistemi.Dtos;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Services.Interfaces;
using OgrenciBilgiSistemi.ViewModels;

namespace OgrenciBilgiSistemi.Tests.Altyapi
{
    /// <summary>ICihazService sahtesi — çağrıları kaydeder, hep başarılı döner.</summary>
    public sealed class SahteCihazService : ICihazService
    {
        public List<OgrenciModel> GuncellenenOgrenciler { get; } = new();
        public List<int> SilinenOgrenciIdleri { get; } = new();

        public Task YenileCihazListesiAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task<List<CihazModel>> GetCihazlarAsync(CancellationToken ct = default) => Task.FromResult(new List<CihazModel>());
        public List<CihazModel> GetCihazlar() => new();

        public Task<bool> CihazaOgrenciEkleAsync(OgrenciModel ogrenci, CancellationToken ct = default) => Task.FromResult(true);

        public Task<bool> CihazaOgrenciGuncelleAsync(OgrenciModel ogrenci, CancellationToken ct = default)
        {
            GuncellenenOgrenciler.Add(ogrenci);
            return Task.FromResult(true);
        }

        public Task<bool> CihazaOgrenciSilAsync(int ogrenciId, CancellationToken ct = default)
        {
            SilinenOgrenciIdleri.Add(ogrenciId);
            return Task.FromResult(true);
        }

        public Task<bool> CihazaOgrencileriGonderAsync(int cihazId, List<OgrenciModel> ogrenciListesi, CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> CihazdakiTumKullanicilariSilAsync(int cihazId, CancellationToken ct = default) => Task.FromResult(true);
        public Task<List<ZkUserDto>> CihazdanKullanicilariListeleAsync(int cihazId, CancellationToken ct = default) => Task.FromResult(new List<ZkUserDto>());
        public Task<CihazModel?> CihazGetByIdAsync(int id, CancellationToken ct = default) => Task.FromResult<CihazModel?>(null);
        public Task<bool> CihazEkleAsync(CihazModel model, CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> CihazGuncelleAsync(CihazModel model, CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> CihazSilAsync(int id, CancellationToken ct = default) => Task.FromResult(true);
    }

    /// <summary>IYemekhaneService sahtesi — yalnızca OgrenciService'in kullandığı SetBuAyAsync'i izler.</summary>
    public sealed class SahteYemekhaneService : IYemekhaneService
    {
        public List<(int OgrenciId, bool Aktif)> SetBuAyCagrilari { get; } = new();

        public Task<OgrenciYemekModel> SetBuAyAsync(int ogrenciId, bool aktif, string? not = null, CancellationToken ct = default)
        {
            SetBuAyCagrilari.Add((ogrenciId, aktif));
            return Task.FromResult(new OgrenciYemekModel { OgrenciId = ogrenciId, Aktif = aktif });
        }

        public Task<YemekhaneOzetVm> GetOzetAsync(int ogrenciId, int akademikYil, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<YemekhaneIndexRaporSonucDto> GetIndexRaporAsync(int? yil, string? query, int? birimId, RaporDurumFiltresiDto durum = RaporDurumFiltresiDto.Hepsi, int page = 1, int pageSize = 50, bool includePasif = false, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<YemekhaneRaporVm> GetTopluRaporAsync(DateTime? bas, DateTime? bit, string? q, int page, int pageSize, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<byte[]> ExportTopluRaporExcelAsync(DateTime? bas, DateTime? bit, string? q, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<OgrenciYemekTarifeModel?> GetTarifeAsync(int ogrenciId, int akademikYil, CancellationToken ct = default) => throw new NotImplementedException();
        public Task SetTarifeAsync(int ogrenciId, int akademikYil, decimal aylikTutar, string? aciklama = null, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<OgrenciYemekModel> SetAyAsync(int ogrenciId, int yil, int ay, bool aktif, string? not = null, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<OgrenciYemekModel?> GetAyAsync(int ogrenciId, int yil, int ay, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<bool?> GetBuAyDurumAsync(int ogrenciId, CancellationToken ct = default) => Task.FromResult<bool?>(null);
        public Task<OgrenciYemekModel> ToggleBuAyAsync(int ogrenciId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Dictionary<int, bool>> GetBuAyDurumlariAsync(IEnumerable<int> ogrenciIdleri, CancellationToken ct = default) => Task.FromResult(new Dictionary<int, bool>());
        public Task<List<OgrenciYemekOdemeModel>> GetAkademikYilOdemeleriAsync(int ogrenciId, int akademikYil, CancellationToken ct = default) => throw new NotImplementedException();
        public Task OdemeEkleAsync(int ogrenciId, int yil, int ay, decimal tutar, DateTime? tarih, string? aciklama, CancellationToken ct = default) => throw new NotImplementedException();
        public Task OdemeSilAsync(int odemeId, CancellationToken ct = default) => throw new NotImplementedException();
    }

    /// <summary>IFileStorage sahtesi — diske yazmadan sabit yol döner.</summary>
    public sealed class SahteFileStorage : IFileStorage
    {
        public List<string?> KaydedilenDosyalar { get; } = new();

        public Task<string?> SaveImageAsync(IFormFile file, string? existingPath = null, CancellationToken ct = default)
        {
            var yol = $"uploads/test_{KaydedilenDosyalar.Count + 1}.png";
            KaydedilenDosyalar.Add(yol);
            return Task.FromResult<string?>(yol);
        }
    }
}
