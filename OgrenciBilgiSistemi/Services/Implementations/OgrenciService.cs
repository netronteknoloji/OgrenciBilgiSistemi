using ClosedXML.Excel;
using DocumentFormat.OpenXml.Vml.Office;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OgrenciBilgiSistemi.Abstractions;
using OgrenciBilgiSistemi.Data;
using OgrenciBilgiSistemi.Dtos;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Services.Interfaces;
using OgrenciBilgiSistemi.Shared.Enums;
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

        private static string? NormalizeKartNo(string? val)
        {
            if (string.IsNullOrWhiteSpace(val))
                return null;
            return val.Trim().TrimStart('0');
        }

        private static string? NormalizeText(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

        // Ortak arama filtresi — liste Excel'i ve veli raporu Excel'inde kullanılır
        // (OgrencilerController'dan taşındı; davranış birebir korunmuştur)
        private static IQueryable<OgrenciModel> AramaFiltreUygula(
            IQueryable<OgrenciModel> q, string? searchString, bool veliDahil = false)
        {
            if (string.IsNullOrWhiteSpace(searchString))
                return q;

            var s = searchString.Trim();

            if (long.TryParse(s, out var no))
            {
                q = q.Where(o =>
                    o.OgrenciNo == no ||
                    (o.OgrenciAdSoyad != null &&
                     (EF.Functions.Like(EF.Functions.Collate(o.OgrenciAdSoyad, "Turkish_100_CI_AI"), $"%{s}%")
                      || EF.Functions.Like(EF.Functions.Collate(o.OgrenciAdSoyad, "Latin1_General_CI_AI"), $"%{s}%")))
                    || (veliDahil && o.Veli != null && o.Veli.KullaniciAdi != null &&
                        EF.Functions.Like(o.Veli.KullaniciAdi, $"%{s}%")));
            }
            else
            {
                q = q.Where(o =>
                    (o.OgrenciAdSoyad != null &&
                     (EF.Functions.Like(EF.Functions.Collate(o.OgrenciAdSoyad, "Turkish_100_CI_AI"), $"%{s}%")
                      || EF.Functions.Like(EF.Functions.Collate(o.OgrenciAdSoyad, "Latin1_General_CI_AI"), $"%{s}%")))
                    || (veliDahil && o.Veli != null && o.Veli.KullaniciAdi != null &&
                        EF.Functions.Like(o.Veli.KullaniciAdi, $"%{s}%")));
            }

            return q;
        }

        private async Task<int?> BirimdenOgretmenBulAsync(int? birimId, CancellationToken ct)
        {
            if (birimId is null) return null;
            return await _db.OgretmenProfiller
                .AsNoTracking()
                .Where(op => op.BirimId == birimId && op.OgretmenDurum)
                .OrderBy(op => op.KullaniciId)
                .Select(op => (int?)op.KullaniciId)
                .FirstOrDefaultAsync(ct);
        }

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

                model.OgretmenId = await BirimdenOgretmenBulAsync(model.BirimId, ct);
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
            OgrenciModel? ent = null;
            var strategy = _db.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _db.Database.BeginTransactionAsync(ct);

                ent = await _db.Ogrenciler.FindAsync(new object[] { model.OgrenciId }, ct)
                      ?? throw new KeyNotFoundException("Öğrenci yok");

                ent.OgrenciAdSoyad = (model.OgrenciAdSoyad ?? string.Empty).ToUpper(_tr);
                ent.OgrenciNo = model.OgrenciNo;
                ent.OgrenciKartNo = NormalizeKartNo(model.OgrenciKartNo);
                ent.BirimId = model.BirimId;
                ent.OgretmenId = await BirimdenOgretmenBulAsync(model.BirimId, ct);
                ent.OgrenciDurum = model.OgrenciDurum;
                ent.OgrenciCikisDurumu = model.OgrenciCikisDurumu;
                ent.VeliId = model.VeliId;
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

            // Cihaz senkronizasyonu transaction dışında — uzak cihaz çağrısı transaction'ı uzun tutmamalı.
            if (buAyYemekhaneAktif.HasValue && ent is not null)
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
                            await _cihaz.CihazaOgrenciGuncelleAsync(ent, ct);
                        }
                        else
                        {
                            await _cihaz.CihazaOgrenciSilAsync(ent.OgrenciId, ct);
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.LogError(ex, "Cihaz senkron hatası. Cihaz: {ip}, ÖğrenciId: {id}", cihaz.IpAdresi, ent.OgrenciId);
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
        OgrenciFiltre filtre = OgrenciFiltre.Aktif,
        int pageSize = 50,
        CancellationToken ct = default)
        {
            var q = _db.Ogrenciler
                .AsNoTracking()
                .Include(o => o.Birim)
                .AsQueryable();

            q = filtre switch
            {
                OgrenciFiltre.Aktif => q.Where(o => o.OgrenciDurum),
                OgrenciFiltre.Pasif => q.Where(o => !o.OgrenciDurum),
                _ => q
            };

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
                q = q.Include(o => o.Veli);

            return await q.AsNoTracking()
                          .FirstOrDefaultAsync(o => o.OgrenciId == id, ct);
        }

        // ---- Excel / Rapor (OgrencilerController'dan taşındı) -----------------

        public async Task<(byte[] Content, string FileName, string ContentType)> ExportOgrenciListesiExcelAsync(
            string? sortOrder,
            string? searchString,
            int? birimId,
            CancellationToken ct = default)
        {
            var ogrenciler = _db.Ogrenciler
                .AsNoTracking()
                .Include(o => o.Birim)
                .Include(o => o.Veli)
                    .ThenInclude(k => k!.VeliProfil)
                .Where(o => o.OgrenciDurum);

            ogrenciler = AramaFiltreUygula(ogrenciler, searchString);

            if (birimId.HasValue)
                ogrenciler = ogrenciler.Where(o => o.BirimId == birimId.Value);

            ogrenciler = sortOrder == "No_desc"
                ? ogrenciler.OrderByDescending(o => o.OgrenciNo).ThenBy(o => o.OgrenciAdSoyad)
                : ogrenciler.OrderBy(o => o.OgrenciNo).ThenBy(o => o.OgrenciAdSoyad);

            var list = await ogrenciler.ToListAsync(ct);

            var ids = list.Select(o => o.OgrenciId).ToList();
            var yemekMap = await _yemekhane.GetBuAyDurumlariAsync(ids, ct);

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Öğrenci Listesi");

            ws.Cell(1, 1).Value = "ID";
            ws.Cell(1, 2).Value = "Ad Soyad";
            ws.Cell(1, 3).Value = "Nosu";
            ws.Cell(1, 4).Value = "Kart No";
            ws.Cell(1, 5).Value = "Birim";
            ws.Cell(1, 6).Value = "Veli Ad Soyad";
            ws.Cell(1, 7).Value = "Veli Telefon";
            ws.Cell(1, 8).Value = "Durum";
            ws.Cell(1, 9).Value = "Öğle Çıkışı";
            ws.Cell(1, 10).Value = "Yemekhane (Bu Ay)";

            ws.Range("A1:J1").Style.Font.Bold = true;

            var row = 2;
            foreach (var o in list)
            {
                ws.Cell(row, 1).Value = o.OgrenciId;
                ws.Cell(row, 2).Value = o.OgrenciAdSoyad;

                ws.Cell(row, 3).Value = o.OgrenciNo;
                ws.Cell(row, 3).Style.NumberFormat.Format = "0";

                ws.Cell(row, 4).Value = o.OgrenciKartNo;
                ws.Cell(row, 5).Value = o.Birim?.BirimAd;

                ws.Cell(row, 6).Value = o.Veli?.KullaniciAdi;
                ws.Cell(row, 7).Value = o.Veli?.Telefon;

                ws.Cell(row, 8).Value = o.OgrenciDurum ? "Aktif" : "Pasif";
                ws.Cell(row, 9).Value = o.OgrenciCikisDurumu switch
                {
                    OglenCikisDurumu.Hayir => "Hayır",
                    OglenCikisDurumu.Evet => "Evet",
                    _ => o.OgrenciCikisDurumu.ToString()
                };

                var aktifMi = yemekMap.TryGetValue(o.OgrenciId, out var a) && a;
                ws.Cell(row, 10).Value = aktifMi ? "Aktif" : "Pasif";

                row++;
            }

            if (row > 2)
                ws.Range(1, 1, row - 1, 10).SetAutoFilter();

            ws.SheetView.FreezeRows(1);
            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);

            var fileName = $"OgrenciListesi_{DateTime.Now:yyyyMMdd}.xlsx";
            const string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            return (stream.ToArray(), fileName, contentType);
        }

        public async Task<SayfalanmisListeModel<OgrenciVeliRaporDto>> GetVeliRaporAsync(
            string? query,
            int? birimId,
            int page,
            int pageSize,
            CancellationToken ct = default)
        {
            // Temel sorgu: aktif öğrenciler + sınıf + veli
            var q = _db.Ogrenciler
                .AsNoTracking()
                .Include(o => o.Birim)
                .Include(o => o.Veli)
                    .ThenInclude(k => k!.VeliProfil)
                .Where(o => o.OgrenciDurum); // sadece aktif öğrenciler

            // Sınıf filtresi
            if (birimId.HasValue)
                q = q.Where(o => o.BirimId == birimId.Value);

            // Arama filtresi (öğrenci / numara / veli)
            if (!string.IsNullOrWhiteSpace(query))
            {
                var s = query.Trim();

                if (int.TryParse(s, out var no))
                {
                    q = q.Where(o =>
                        o.OgrenciNo == no ||
                        (o.OgrenciAdSoyad != null && EF.Functions.Like(o.OgrenciAdSoyad, $"%{s}%")) ||
                        (o.Veli != null && o.Veli.KullaniciAdi != null &&
                         EF.Functions.Like(o.Veli.KullaniciAdi, $"%{s}%")));
                }
                else
                {
                    q = q.Where(o =>
                        (o.OgrenciAdSoyad != null && EF.Functions.Like(o.OgrenciAdSoyad, $"%{s}%")) ||
                        (o.Veli != null && o.Veli.KullaniciAdi != null &&
                         EF.Functions.Like(o.Veli.KullaniciAdi, $"%{s}%")));
                }
            }

            // Sıralama: önce sınıf, sonra öğrenci
            q = q
                .OrderBy(o => o.Birim!.BirimAd)
                .ThenBy(o => o.OgrenciAdSoyad);

            // DTO'ya projeksiyon + sayfalama
            var dtoQuery = q.Select(o => new OgrenciVeliRaporDto
            {
                OgrenciId = o.OgrenciId,
                OgrenciAdSoyad = o.OgrenciAdSoyad,
                OgrenciNo = o.OgrenciNo.ToString(),
                SinifAd = o.Birim != null ? o.Birim.BirimAd : null,
                VeliKullaniciAdi = o.Veli != null ? o.Veli.KullaniciAdi : null,
                Yakinlik = o.Veli != null && o.Veli.VeliProfil != null ? o.Veli.VeliProfil.VeliYakinlik.ToString() : null,
                VeliTelefon = o.Veli != null ? o.Veli.Telefon : null,
                VeliMeslek = o.Veli != null && o.Veli.VeliProfil != null ? o.Veli.VeliProfil.VeliMeslek : null,
                VeliIsYeri = o.Veli != null && o.Veli.VeliProfil != null ? o.Veli.VeliProfil.VeliIsYeri : null
            });

            return await SayfalanmisListeModel<OgrenciVeliRaporDto>.CreateAsync(dtoQuery, page, pageSize, ct);
        }

        public async Task<(byte[] Content, string FileName, string ContentType)> ExportVeliRaporExcelAsync(
            string? query,
            int? birimId,
            CancellationToken ct = default)
        {
            var q = _db.Ogrenciler
                .AsNoTracking()
                .Include(o => o.Birim)
                .Include(o => o.Veli)
                    .ThenInclude(k => k!.VeliProfil)
                .Where(o => o.OgrenciDurum);

            if (birimId.HasValue)
                q = q.Where(o => o.BirimId == birimId.Value);

            q = AramaFiltreUygula(q, query, veliDahil: true);

            q = q
                .OrderBy(o => o.Birim!.BirimAd)
                .ThenBy(o => o.OgrenciAdSoyad);

            var list = await q
                .Select(o => new OgrenciVeliRaporDto
                {
                    OgrenciId = o.OgrenciId,
                    OgrenciAdSoyad = o.OgrenciAdSoyad,
                    OgrenciNo = o.OgrenciNo.ToString(),
                    SinifAd = o.Birim != null ? o.Birim.BirimAd : null,
                    VeliKullaniciAdi = o.Veli != null ? o.Veli.KullaniciAdi : null,
                    Yakinlik = o.Veli != null && o.Veli.VeliProfil != null ? o.Veli.VeliProfil.VeliYakinlik.ToString() : null,
                    VeliTelefon = o.Veli != null ? o.Veli.Telefon : null,
                    VeliMeslek = o.Veli != null && o.Veli.VeliProfil != null ? o.Veli.VeliProfil.VeliMeslek : null,
                    VeliIsYeri = o.Veli != null && o.Veli.VeliProfil != null ? o.Veli.VeliProfil.VeliIsYeri : null
                })
                .ToListAsync(ct);

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("OgrenciVeliRaporu");

            // Başlıklar
            ws.Cell(1, 1).Value = "Öğrenci Adı";
            ws.Cell(1, 2).Value = "Öğrenci No";
            ws.Cell(1, 3).Value = "Sınıf";
            ws.Cell(1, 4).Value = "Veli Adı";
            ws.Cell(1, 5).Value = "Yakınlık";
            ws.Cell(1, 6).Value = "Telefon";
            ws.Cell(1, 7).Value = "Meslek";
            ws.Cell(1, 8).Value = "İşyeri";

            ws.Range("A1:H1").Style.Font.Bold = true;

            var row = 2;
            foreach (var s in list)
            {
                ws.Cell(row, 1).Value = s.OgrenciAdSoyad;
                ws.Cell(row, 2).Value = s.OgrenciNo;
                ws.Cell(row, 3).Value = s.SinifAd;
                ws.Cell(row, 4).Value = s.VeliKullaniciAdi;
                ws.Cell(row, 5).Value = s.Yakinlik;
                ws.Cell(row, 6).Value = s.VeliTelefon;
                ws.Cell(row, 7).Value = s.VeliMeslek;
                ws.Cell(row, 8).Value = s.VeliIsYeri;
                row++;
            }

            if (row > 2)
                ws.Range(1, 1, row - 1, 8).SetAutoFilter();

            ws.SheetView.FreezeRows(1);
            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);

            var fileName = $"OgrenciVeliRaporu_{DateTime.Now:yyyyMMdd}.xlsx";
            const string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            return (stream.ToArray(), fileName, contentType);
        }

    }
}