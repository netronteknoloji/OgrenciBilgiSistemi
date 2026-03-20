using DocumentFormat.OpenXml.Vml.Office;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OgrenciBilgiSistemi.Abstractions;
using OgrenciBilgiSistemi.Data;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Models.Enums;
using OgrenciBilgiSistemi.Services.Interfaces;
using System.Globalization;


namespace OgrenciBilgiSistemi.Services.Implementations
{
    public class OgrenciService : IOgrenciService
    {
        private static readonly CultureInfo _tr = CultureInfo.GetCultureInfo("tr-TR");

        private readonly AppDbContext _db;
        private readonly ICihazService _cihaz;
        private readonly IYemekhaneService _yemekhane;
        private readonly IFileStorage _files;
        private readonly ILogger<OgrenciService> _log;

        public OgrenciService(
            AppDbContext db,
            ICihazService cihaz,
            IYemekhaneService yemekhane,
            IFileStorage files,
            ILogger<OgrenciService> log)
        {
            _db = db;
            _cihaz = cihaz;
            _yemekhane = yemekhane;
            _files = files;
            _log = log;
        }

        // ---- Helpers ---------------------------------------------------------

        private static string NormalizeKartNo(string? val)
        {
            if (string.IsNullOrWhiteSpace(val))
                return string.Empty;
            return val.Trim().TrimStart('0');
        }

        private static string? NormalizeText(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

        // ---- IOgrenciService -------------------------------------------------

        public async Task<int> EkleAsync(OgrenciModel model, IFormFile? gorsel, bool buAyYemekhaneAktif, CancellationToken ct = default)
        {
            model.OgrenciAdSoyad = (model.OgrenciAdSoyad ?? string.Empty).ToUpper(_tr);
            model.OgrenciKartNo = NormalizeKartNo(model.OgrenciKartNo);

            var strategy = _db.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _db.Database.BeginTransactionAsync(ct);

                if (gorsel is not null)
                {
                    model.OgrenciGorsel = await _files.SaveImageAsync(gorsel, existingPath: null, ct);
                }

                _db.Ogrenciler.Add(model);
                await _db.SaveChangesAsync(ct);

                // Yalnızca içinde bulunulan ay için yemekhane durumu
                await _yemekhane.SetBuAyAsync(model.OgrenciId, buAyYemekhaneAktif, ct: ct);

                await tx.CommitAsync(ct);
            });

            return model.OgrenciId;
        }

        public async Task GuncelleAsync(OgrenciModel model, IFormFile? gorsel, bool? buAyYemekhaneAktif, CancellationToken ct = default)
        {
            var strategy = _db.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _db.Database.BeginTransactionAsync(ct);

                var ent = await _db.Ogrenciler.FindAsync(new object[] { model.OgrenciId }, ct)
                          ?? throw new KeyNotFoundException("Öğrenci yok");

                ent.OgrenciAdSoyad = (model.OgrenciAdSoyad ?? string.Empty).ToUpper(_tr);
                ent.OgrenciNo = model.OgrenciNo;
                ent.OgrenciKartNo = NormalizeKartNo(model.OgrenciKartNo);
                ent.BirimId = model.BirimId;
                ent.PersonelId = model.PersonelId;
                ent.OgrenciDurum = model.OgrenciDurum;
                ent.OgrenciCikisDurumu = model.OgrenciCikisDurumu;
                ent.OgrenciVeliId = model.OgrenciVeliId;
                ent.ServisId = model.ServisId;

                if (gorsel is not null)
                {
                    ent.OgrenciGorsel = await _files.SaveImageAsync(gorsel, ent.OgrenciGorsel, ct);
                }

                await _db.SaveChangesAsync(ct);

                if (buAyYemekhaneAktif.HasValue)
                {
                    await _yemekhane.SetBuAyAsync(ent.OgrenciId, buAyYemekhaneAktif.Value, ct: ct);
                }

                await tx.CommitAsync(ct);
            });

            if (buAyYemekhaneAktif.HasValue)
            {
                var cihazlar = await _db.Cihazlar
                    .Where(c => c.Aktif && c.IstasyonTipi == IstasyonTipi.Yemekhane)
                    .ToListAsync(ct);

                foreach (var cihaz in cihazlar)
                {
                    try
                    {
                        if (buAyYemekhaneAktif.Value)
                        {
                            await _cihaz.CihazaOgrenciGuncelleAsync(model, ct);
                        }
                        else
                        {
                            await _cihaz.CihazaOgrenciSilAsync(model.OgrenciId, ct);
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.LogError(ex, "Cihaz senkron hatası. Cihaz: {ip}, ÖğrenciId: {id}", cihaz.IpAdresi, model.OgrenciId);
                    }
                }
            }
        }

        public async Task SilAsync(int ogrenciId, CancellationToken ct = default)
        {
            var strategy = _db.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _db.Database.BeginTransactionAsync(ct);

                var ent = await _db.Ogrenciler.FindAsync(new object[] { ogrenciId }, ct);
                if (ent == null)
                {
                    _log.LogWarning("Silinmek istenen öğrenci bulunamadı. Id={Id}", ogrenciId);
                    return;
                }

                ent.OgrenciDurum = false;
                await _db.SaveChangesAsync(ct);

                await tx.CommitAsync(ct);
            });
        }

        public async Task<bool> CihazaGonderAsync(int cihazId, CancellationToken ct = default)
        {
            int yil = DateTime.Now.Year;
            int ay = DateTime.Now.Month;

            var q = _db.Ogrenciler
                .AsNoTracking()
                .Where(o => o.OgrenciYemekler.Any(y => y.Yil == yil && y.Ay == ay && y.Aktif));

            var list = await q.ToListAsync(ct);

            if (list.Count == 0)
            {
                _log.LogInformation("Cihaza gönderilecek yemekhane aktif öğrenci yok. cihazId={Id}", cihazId);
                return true;
            }

            var ok = await _cihaz.CihazaOgrencileriGonderAsync(cihazId, list, ct);

            _log.LogInformation(
                "Cihaza gönderim tamamlandı. cihazId={Id}, sayi={Count}, sonuc={Sonuc}",
                cihazId, list.Count, ok
            );

            return ok;
        }

        public async Task<SayfalanmisListeModel<OgrenciModel>> SearchPagedAsync(
        string? sortOrder,
        string? searchString,
        int pageNumber,
        int? birimId,
        bool includePasif,
        int pageSize = 50,
        CancellationToken ct = default)
        {
            var q = _db.Ogrenciler
                .AsNoTracking()
                .Include(o => o.Birim)
                .Where(o => includePasif || o.OgrenciDurum)
                .AsQueryable();

            // Arama (AdSoyad + Numara + KartNo)
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var s = searchString.Trim();
                if (int.TryParse(s, out var no))
                {
                    q = q.Where(o =>
                        o.OgrenciNo == no ||
                        (o.OgrenciKartNo != null && (o.OgrenciKartNo == s || EF.Functions.Like(o.OgrenciKartNo, $"%{s}%"))) ||
                        (o.OgrenciAdSoyad != null && (
                            EF.Functions.Like(EF.Functions.Collate(o.OgrenciAdSoyad!, "Turkish_100_CI_AI"), $"%{s}%") ||
                            EF.Functions.Like(EF.Functions.Collate(o.OgrenciAdSoyad!, "Latin1_General_CI_AI"), $"%{s}%")
                        )));
                }
                else
                {
                    q = q.Where(o =>
                        (o.OgrenciAdSoyad != null && (
                            EF.Functions.Like(EF.Functions.Collate(o.OgrenciAdSoyad!, "Turkish_100_CI_AI"), $"%{s}%") ||
                            EF.Functions.Like(EF.Functions.Collate(o.OgrenciAdSoyad!, "Latin1_General_CI_AI"), $"%{s}%")
                        )) ||
                        (o.OgrenciKartNo != null && EF.Functions.Like(o.OgrenciKartNo, $"%{s}%")));
                }
            }

            // Birim filtresi
            if (birimId.HasValue)
                q = q.Where(o => o.BirimId == birimId.Value);

            // Sıralama
            q = sortOrder switch
            {
                "AdSoyad" => q.OrderBy(o => o.OgrenciAdSoyad).ThenBy(o => o.OgrenciNo),
                "AdSoyad_desc" => q.OrderByDescending(o => o.OgrenciAdSoyad).ThenBy(o => o.OgrenciNo),
                "No" => q.OrderBy(o => o.OgrenciNo).ThenBy(o => o.OgrenciAdSoyad),
                "No_desc" => q.OrderByDescending(o => o.OgrenciNo).ThenBy(o => o.OgrenciAdSoyad),
                _ => q.OrderBy(o => o.OgrenciAdSoyad).ThenBy(o => o.OgrenciNo)
            };

            // Güvenli sayfa
            var pageIndex = Math.Max(1, pageNumber);

            return await SayfalanmisListeModel<OgrenciModel>.CreateAsync(q, pageIndex, pageSize, ct);
        }

        public async Task<OgrenciModel?> GetByIdAsync(int id, bool includeVeli = true, CancellationToken ct = default)
        {
            var q = _db.Ogrenciler.AsQueryable();

            if (includeVeli)
                q = q.Include(o => o.OgrenciVeli);

            return await q.AsNoTracking()
                          .FirstOrDefaultAsync(o => o.OgrenciId == id, ct);
        }

    }
}