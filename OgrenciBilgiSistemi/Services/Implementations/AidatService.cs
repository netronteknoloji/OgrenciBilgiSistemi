using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OgrenciBilgiSistemi.Data;
using OgrenciBilgiSistemi.Dtos;
using OgrenciBilgiSistemi.Helpers;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Services.Interfaces;
using ClosedXML.Excel;

namespace OgrenciBilgiSistemi.Services.Implementations
{
    public sealed class AidatService : IAidatService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<AidatService> _logger;

        public AidatService(AppDbContext db, ILogger<AidatService> logger)
        {
            _db = db;
            _logger = logger;
        }

        // ---------------------- Helpers ----------------------
        private static (DateTime? Start, DateTime? EndExcl) NormalizeDateRange(DateTime? bas, DateTime? bit)
        {
            var start = bas?.Date;
            var endExcl = bit?.Date;
            if (start.HasValue && endExcl.HasValue && endExcl <= start)
                endExcl = start.Value.AddDays(1);
            return (start, endExcl);
        }

        private async Task<decimal> GetTarifeTutarAsync(int baslangicYil, CancellationToken ct)
        {
            var tutar = await _db.OgrenciAidatTarifeler
                .AsNoTracking()
                .Where(t => t.BaslangicYil == baslangicYil)
                .Select(t => (decimal?)t.Tutar)
                .FirstOrDefaultAsync(ct);

            return tutar ?? 0m;
        }

        // ---------------------- Öğrenci Yıllık Özet ----------------------
        public async Task<OgrenciAidatDto> GetOgrenciAidatAsync(int ogrenciId, int yil, CancellationToken ct = default)
        {
            var aidat = await _db.OgrenciAidatlar
                .AsNoTracking()
                .Include(a => a.Ogrenci)
                .Include(a => a.Odemeler)
                .FirstOrDefaultAsync(a => a.OgrenciId == ogrenciId && a.BaslangicYil == yil, ct);

            var tarife = await _db.OgrenciAidatTarifeler
                .AsNoTracking()
                .Where(t => t.BaslangicYil == yil)
                .Select(t => new TarifeDto { Yil = t.BaslangicYil, Tutar = t.Tutar, Aciklama = t.Aciklama })
                .FirstOrDefaultAsync(ct);

            if (aidat == null)
            {
                var tarifeTutar = tarife?.Tutar ?? await GetTarifeTutarAsync(yil, ct);

                var ogr = await _db.Ogrenciler
                    .AsNoTracking()
                    .Where(x => x.OgrenciId == ogrenciId)
                    .Select(x => new { x.OgrenciId, x.OgrenciAdSoyad })
                    .FirstOrDefaultAsync(ct);

                return new OgrenciAidatDto
                {
                    OgrenciId = ogrenciId,
                    OgrenciAdSoyad = ogr?.OgrenciAdSoyad ?? "Öğrenci",
                    Yil = yil,
                    ToplamBorc = tarifeTutar,
                    ToplamOdenen = 0m,
                    Muaf = false,
                    SonOdemeTarihi = null,
                    Odemeler = new(),
                    Tarife = tarife
                };
            }

            return new OgrenciAidatDto
            {
                OgrenciId = aidat.OgrenciId,
                OgrenciAdSoyad = aidat.Ogrenci?.OgrenciAdSoyad ?? string.Empty,
                Yil = aidat.BaslangicYil,
                ToplamBorc = aidat.Borc,
                ToplamOdenen = aidat.Odenen,
                Muaf = aidat.Muaf,
                SonOdemeTarihi = aidat.SonOdemeTarihi,
                Odemeler = aidat.Odemeler
                    .OrderByDescending(p => p.OdemeTarihi)
                    .Select(p => new OdemeSatiriDto
                    {
                        OgrenciAidatOdemeId = p.OgrenciAidatOdemeId,
                        Tarih = p.OdemeTarihi,
                        Tutar = p.Tutar,
                        Aciklama = p.Aciklama
                    }).ToList(),
                Tarife = tarife
            };
        }

        private async Task<IQueryable<AidatRaporDto>> BuildAidatRaporDtoQueryAsync(
    int? yil,
    DateTime? bas,
    DateTime? bit,
    string? query,
    int? birimId,
    RaporDurumFiltresiDto durum,
    int? tarifeYil,
    bool includePasif,
    CancellationToken ct)
        {
            var (start, endExcl) = NormalizeDateRange(bas, bit);

            var y = yil ?? AkademikDonemHelper.Current();
            var ty = tarifeYil ?? y;

            var students = _db.Ogrenciler.AsNoTracking().AsQueryable();

            if (!includePasif)
                students = students.Where(s => s.OgrenciDurum == true);

            if (birimId is not null)
                students = students.Where(s => s.BirimId == birimId);

            if (!string.IsNullOrWhiteSpace(query))
            {
                var q = query.Trim();

                if (int.TryParse(q, out var no))
                {
                    students = students.Where(s =>
                        s.OgrenciNo == no ||
                        (s.OgrenciAdSoyad != null &&
                            (EF.Functions.Like(EF.Functions.Collate(s.OgrenciAdSoyad, "Turkish_100_CI_AI"), $"%{q}%") ||
                             EF.Functions.Like(EF.Functions.Collate(s.OgrenciAdSoyad, "Latin1_General_CI_AI"), $"%{q}%")))
                    );
                }
                else
                {
                    students = students.Where(s =>
                        s.OgrenciAdSoyad != null &&
                        (EF.Functions.Like(EF.Functions.Collate(s.OgrenciAdSoyad, "Turkish_100_CI_AI"), $"%{q}%") ||
                         EF.Functions.Like(EF.Functions.Collate(s.OgrenciAdSoyad, "Latin1_General_CI_AI"), $"%{q}%"))
                    );
                }
            }

            var tarifeTutar = await GetTarifeTutarAsync(ty, ct);

            var baseQuery =
                from s in students
                join b in _db.Birimler.AsNoTracking()
                    on s.BirimId equals b.BirimId into bjoin
                from b in bjoin.DefaultIfEmpty()
                join a in _db.OgrenciAidatlar.AsNoTracking().Where(x => x.BaslangicYil == y)
                    on s.OgrenciId equals a.OgrenciId into aidJoin
                from a in aidJoin.DefaultIfEmpty()
                select new
                {
                    s.OgrenciId,
                    s.OgrenciAdSoyad,
                    s.OgrenciNo,
                    OgrenciSinif = (string?)b.BirimAd,
                    Muaf = a != null && a.Muaf,
                    BorcRaw = a == null
                        ? tarifeTutar
                        : (a.Muaf ? 0m : ((a.Borc > 0m) ? a.Borc : tarifeTutar)),
                    OdenenRaw = a == null || a.Muaf
                        ? 0m
                        : (_db.OgrenciAidatOdemeler
                            .Where(p => p.OgrenciAidatId == a.OgrenciAidatId
                                        && (!start.HasValue || p.OdemeTarihi >= start.Value)
                                        && (!endExcl.HasValue || p.OdemeTarihi < endExcl.Value))
                            .Sum(p => (decimal?)p.Tutar) ?? 0m),
                    Yil = y
                };

            var dtoQuery = baseQuery.Select(x => new AidatRaporDto
            {
                OgrenciId = x.OgrenciId,
                OgrenciAdSoyad = x.OgrenciAdSoyad,
                OgrenciNo = x.OgrenciNo.ToString(),
                OgrenciSinif = x.OgrenciSinif,
                Yil = x.Yil,
                Muaf = x.Muaf,

                BorcGosterim = x.Muaf ? 0m : x.BorcRaw,
                GosterilenOdenen = x.Muaf ? 0m : x.OdenenRaw,

                Kalan = x.Muaf ? 0m : (x.BorcRaw - x.OdenenRaw),
                Kapandi = x.Muaf || (x.BorcRaw - x.OdenenRaw) <= 0m
            });

            // Durum filtresi
            dtoQuery = durum switch
            {
                RaporDurumFiltresiDto.Borclu => dtoQuery.Where(r => !r.Muaf && r.Kalan > 0m),
                RaporDurumFiltresiDto.Borcsuz => dtoQuery.Where(r => !r.Muaf && r.Kalan == 0m),
                RaporDurumFiltresiDto.Muaf => dtoQuery.Where(r => r.Muaf),
                _ => dtoQuery
            };

            return dtoQuery;
        }


        // ---------------------- Rapor (IAidatService ile UYUMLU) ----------------------
        public async Task<AidatRaporSonucDto> GetAidatRaporAsync(
            int? yil,
            DateTime? bas,
            DateTime? bit,
            string? query,
            int? birimId,
            RaporDurumFiltresiDto durum = RaporDurumFiltresiDto.Hepsi,
            int? tarifeYil = null,
            int page = 1,
            int pageSize = 50,
            bool includePasif = false,
            CancellationToken ct = default)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 5, 200);

            // ✅ Ortak sorgu (filtreler + durum filtresi dahil)
            var dtoQuery = await BuildAidatRaporDtoQueryAsync(
                yil, bas, bit, query, birimId, durum, tarifeYil, includePasif, ct);

            // ✅ Toplamlar: sayfalama öncesi, filtreli tüm veri üzerinden
            var aktifQuery = dtoQuery.Where(x => !x.Muaf);

            var toplamBorc = await aktifQuery.SumAsync(x => (decimal?)x.BorcGosterim, ct) ?? 0m;
            var toplamOdenen = await aktifQuery.SumAsync(x => (decimal?)x.GosterilenOdenen, ct) ?? 0m;
            var toplamKalan = await aktifQuery.SumAsync(x => (decimal?)x.Kalan, ct) ?? 0m;

            // ✅ UI için sayfalama
            var orderedQuery = dtoQuery
                .OrderBy(x => x.OgrenciSinif)
                .ThenBy(x => x.OgrenciAdSoyad);

            var paged = await SayfalanmisListeModel<AidatRaporDto>.CreateAsync(
                orderedQuery,
                page,
                pageSize,
                ct);

            // ✅ Kullanılabilir yıllar
            var kullanilabilirYillar = await _db.OgrenciAidatlar
                .AsNoTracking()
                .Select(a => a.BaslangicYil)
                .Distinct()
                .OrderByDescending(x => x)
                .ToListAsync(ct);

            return new AidatRaporSonucDto
            {
                Satirlar = paged,
                ToplamBorc = toplamBorc,
                ToplamOdenenGosterilen = toplamOdenen,
                ToplamKalan = toplamKalan,
                KullanilabilirYillar = kullanilabilirYillar
            };
        }


        // ---------------------- Excel Export (xlsx) ----------------------
        public async Task<(byte[] Content, string FileName, string ContentType)> ExportAidatRaporExcelAsync(
            int? yil,
            DateTime? bas,
            DateTime? bit,
            string? query,
            int? birimId,
            RaporDurumFiltresiDto durum = RaporDurumFiltresiDto.Hepsi,
            int? tarifeYil = null,
            bool includePasif = false,
            CancellationToken ct = default)
        {
            // ✅ Aynı filtrelerle tüm kayıtları çek (pagination YOK)
            var dtoQuery = await BuildAidatRaporDtoQueryAsync(
                yil, bas, bit, query, birimId, durum, tarifeYil, includePasif, ct);

            var rows = await dtoQuery
                .OrderBy(x => x.OgrenciSinif)
                .ThenBy(x => x.OgrenciAdSoyad)
                .ToListAsync(ct);

            // ✅ Toplamlar (filtreli tüm kayıtlar üzerinden)
            var aktif = rows.Where(x => !x.Muaf).ToList();
            var toplamBorc = aktif.Sum(x => x.BorcGosterim);
            var toplamOdenen = aktif.Sum(x => x.GosterilenOdenen);
            var toplamKalan = aktif.Sum(x => x.Kalan);

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Aidat Raporu");

            // Başlık satırı
            ws.Cell(1, 1).Value = "Öğrenci Id";
            ws.Cell(1, 2).Value = "Ad Soyad";
            ws.Cell(1, 3).Value = "Numara";
            ws.Cell(1, 4).Value = "Sınıf";
            ws.Cell(1, 5).Value = "Yıl";
            ws.Cell(1, 6).Value = "Borç";
            ws.Cell(1, 7).Value = "Ödenen";
            ws.Cell(1, 8).Value = "Kalan";
            ws.Cell(1, 9).Value = "Muaf";
            ws.Cell(1, 10).Value = "Durum";

            var rowIndex = 2;
            foreach (var r in rows)
            {
                ws.Cell(rowIndex, 1).Value = r.OgrenciId;
                ws.Cell(rowIndex, 2).Value = r.OgrenciAdSoyad;
                ws.Cell(rowIndex, 3).Value = r.OgrenciNo;
                ws.Cell(rowIndex, 4).Value = r.OgrenciSinif;
                ws.Cell(rowIndex, 5).Value = r.Yil;
                ws.Cell(rowIndex, 6).Value = r.BorcGosterim;
                ws.Cell(rowIndex, 7).Value = r.GosterilenOdenen;
                ws.Cell(rowIndex, 8).Value = r.Kalan;
                ws.Cell(rowIndex, 9).Value = r.Muaf ? "Evet" : "Hayır";
                ws.Cell(rowIndex, 10).Value = r.Muaf ? "Muaf" : (r.Kapandi ? "Kapalı" : "Açık");
                rowIndex++;
            }

            // Otomatik genişlik + başlık kalın
            ws.Columns().AdjustToContents();
            ws.Row(1).Style.Font.Bold = true;

            // Özet satırı
            var totalRow = rowIndex + 1;
            ws.Cell(totalRow, 5).Value = "TOPLAM:";
            ws.Cell(totalRow, 6).Value = toplamBorc;
            ws.Cell(totalRow, 7).Value = toplamOdenen;
            ws.Cell(totalRow, 8).Value = toplamKalan;
            ws.Range(totalRow, 5, totalRow, 8).Style.Font.Bold = true;

            using var ms = new MemoryStream();
            wb.SaveAs(ms);

            var fileName = $"AidatRapor_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
            const string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            return (ms.ToArray(), fileName, contentType);
        }


        // ---------------------- Ödeme İşlemleri ----------------------
        public async Task<OdemeSatiriDto> OdemeEkleAsync(AidatOdemeEkleDto dto, CancellationToken ct = default)
        {
            var aidat = await _db.OgrenciAidatlar
                .FirstOrDefaultAsync(a => a.OgrenciId == dto.OgrenciId && a.BaslangicYil == dto.Yil, ct);

            var tarife = await GetTarifeTutarAsync(dto.Yil, ct);

            if (aidat == null)
            {
                // kayıt yoksa oluştur
                aidat = new OgrenciAidatModel
                {
                    OgrenciId = dto.OgrenciId,
                    BaslangicYil = dto.Yil,
                    Borc = tarife,          // borcu tarifeden başlat
                    Odenen = 0m,
                    Muaf = false
                };
                _db.OgrenciAidatlar.Add(aidat);
            }
            else if (!aidat.Muaf && aidat.Borc <= 0m)
            {
                // kayıt var ama Borc<=0 ise tarifeye yükselt
                aidat.Borc = tarife;
            }

            var ent = new OgrenciAidatOdemeModel
            {
                OgrenciAidat = aidat,
                OdemeTarihi = dto.Tarih,
                Tutar = dto.Tutar,
                Aciklama = dto.Aciklama
            };
            _db.OgrenciAidatOdemeler.Add(ent);

            aidat.Odenen += dto.Tutar;
            aidat.SonOdemeTarihi = dto.Tarih;

            await _db.SaveChangesAsync(ct);

            return new OdemeSatiriDto
            {
                OgrenciAidatOdemeId = ent.OgrenciAidatOdemeId,
                Tarih = ent.OdemeTarihi,
                Tutar = ent.Tutar,
                Aciklama = ent.Aciklama
            };
        }

        public async Task<bool> OdemeSilAsync(int ogrenciAidatOdemeId, CancellationToken ct = default)
        {
            var strategy = _db.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _db.Database.BeginTransactionAsync(ct);

                var ent = await _db.OgrenciAidatOdemeler
                    .Include(p => p.OgrenciAidat)
                    .FirstOrDefaultAsync(p => p.OgrenciAidatOdemeId == ogrenciAidatOdemeId, ct);

                if (ent is null)
                    return false;

                var aidat = ent.OgrenciAidat;
                aidat.Odenen = decimal.Round(Math.Max(0m, aidat.Odenen - ent.Tutar), 2, MidpointRounding.AwayFromZero);
                ent.AktifMi = false;
                await _db.SaveChangesAsync(ct);

                aidat.SonOdemeTarihi = await _db.OgrenciAidatOdemeler
                    .Where(x => x.OgrenciAidatId == aidat.OgrenciAidatId)
                    .OrderByDescending(x => x.OdemeTarihi)
                    .Select(x => (DateTime?)x.OdemeTarihi)
                    .FirstOrDefaultAsync(ct);

                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                return true;
            });
        }

        // ---------------------- Muafiyet İşlemleri ----------------------
        public async Task<bool> SetYillikMuafiyetAsync(int ogrenciId, int yil, bool muaf, CancellationToken ct = default)
        {
            var aidat = await _db.OgrenciAidatlar
                .FirstOrDefaultAsync(a => a.OgrenciId == ogrenciId && a.BaslangicYil == yil, ct);

            if (aidat == null)
            {
                var borc = await GetTarifeTutarAsync(yil, ct);
                aidat = new OgrenciAidatModel
                {
                    OgrenciId = ogrenciId,
                    BaslangicYil = yil,
                    Borc = borc,
                    Odenen = 0m,
                    Muaf = muaf
                };
                _db.OgrenciAidatlar.Add(aidat);
            }
            else
            {
                aidat.Muaf = muaf;
            }

            await _db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> GetYillikMuafiyetAsync(int ogrenciId, int yil, CancellationToken ct = default)
        {
            var muaf = await _db.OgrenciAidatlar
                .Where(a => a.OgrenciId == ogrenciId && a.BaslangicYil == yil)
                .Select(a => (bool?)a.Muaf)
                .FirstOrDefaultAsync(ct);

            return muaf ?? false;
        }

        public async Task<List<int>> GetKullanilabilirYillarAsync(int ogrenciId, CancellationToken ct = default)
        {
            return await _db.OgrenciAidatlar
                .AsNoTracking()
                .Where(a => a.OgrenciId == ogrenciId)
                .Select(a => a.BaslangicYil)
                .Distinct()
                .OrderBy(y => y)
                .ToListAsync(ct);
        }

        public async Task<TarifeDto> TarifeKaydetAsync(TarifeDto dto, CancellationToken ct = default)
        {
            var tarife = await _db.OgrenciAidatTarifeler
                .FirstOrDefaultAsync(t => t.BaslangicYil == dto.Yil, ct);

            if (tarife == null)
            {
                tarife = new OgrenciAidatTarifeModel
                {
                    BaslangicYil = dto.Yil,
                    Tutar = dto.Tutar,
                    Aciklama = dto.Aciklama
                };
                _db.OgrenciAidatTarifeler.Add(tarife);
            }
            else
            {
                tarife.Tutar = dto.Tutar;
                tarife.Aciklama = dto.Aciklama;
            }

            await _db.SaveChangesAsync(ct);
            return dto;
        }
    }
}