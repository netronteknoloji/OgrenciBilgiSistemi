using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using OgrenciBilgiSistemi.Data;
using OgrenciBilgiSistemi.Dtos;
using OgrenciBilgiSistemi.Helpers;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Services.Interfaces;
using OgrenciBilgiSistemi.Shared.Services;

namespace OgrenciBilgiSistemi.Services.Implementations
{
    public sealed class AidatService : IAidatService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<AidatService> _logger;
        private readonly IMemoryCache _cache;
        private readonly TenantBaglami _tenant;

        public AidatService(AppDbContext db, ILogger<AidatService> logger, IMemoryCache cache, TenantBaglami tenant)
        {
            _db = db;
            _logger = logger;
            _cache = cache;
            _tenant = tenant;
        }

        // ---------------------- Helpers ----------------------
        private static (DateTime? Start, DateTime? EndExcl) NormalizeDateRange(DateTime? bas, DateTime? bit)
        {
            var start = bas?.Date;
            var endExcl = bit?.Date.AddDays(1); // inclusive end: bit günündeki ödemeler dahil
            if (start.HasValue && endExcl.HasValue && endExcl <= start)
                endExcl = start.Value.AddDays(1);
            return (start, endExcl);
        }

        private string TarifeCacheKey(int baslangicYil) => $"aidat_tarife:{_tenant.OkulKodu}:{baslangicYil}";

        private async Task<decimal> GetTarifeTutarAsync(int baslangicYil, CancellationToken ct)
        {
            var key = TarifeCacheKey(baslangicYil);
            if (_cache.TryGetValue(key, out decimal cached))
                return cached;

            var tutar = await _db.OgrenciAidatTarifeler
                .AsNoTracking()
                .Where(t => t.BaslangicYil == baslangicYil)
                .Select(t => (decimal?)t.Tutar)
                .FirstOrDefaultAsync(ct);

            var result = tutar ?? 0m;
            _cache.Set(key, result, TimeSpan.FromMinutes(30));
            return result;
        }

        // Tarifeyi kullanan (özel tutarı olmayan), muaf olmayan ve ödemesi olan kayıtlar.
        // Tarife değişikliğinden borcu etkilenecek ödeme yapmış öğrencileri verir.
        private IQueryable<OgrenciAidatModel> TarifeyeBagliOdeyenler(int yil) =>
            _db.OgrenciAidatlar
                .IgnoreQueryFilters()
                .Where(a => a.BaslangicYil == yil
                            && a.Borc == null
                            && !a.Muaf
                            && a.Odemeler.Any(o => !o.IsDeleted));

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

            var tarifeTutar = tarife?.Tutar ?? 0m;

            if (aidat == null)
            {
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
                    TarifeTutari = tarifeTutar,
                    OzelBorc = null,
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
                ToplamBorc = aidat.Muaf ? 0m : (aidat.Borc ?? tarifeTutar),
                TarifeTutari = tarifeTutar,
                OzelBorc = aidat.Borc,
                ToplamOdenen = aidat.Odemeler.Sum(p => p.Tutar),
                Muaf = aidat.Muaf,
                SonOdemeTarihi = aidat.Odemeler.Max(p => (DateTime?)p.OdemeTarihi),
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
                        : (a.Muaf ? 0m : (a.Borc ?? tarifeTutar)),
                    // Muaf olsa da tahsil edilmiş ödemeler gösterilir (raporda para kaybolmasın)
                    OdenenRaw = a == null
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
                GosterilenOdenen = x.OdenenRaw,

                Kalan = x.Muaf ? 0m : (x.BorcRaw - x.OdenenRaw),
                Kapandi = x.Muaf || (x.BorcRaw - x.OdenenRaw) <= 0m
            });

            // Durum filtresi
            dtoQuery = durum switch
            {
                RaporDurumFiltresiDto.Borclu => dtoQuery.Where(r => !r.Muaf && r.Kalan > 0m),
                RaporDurumFiltresiDto.Borcsuz => dtoQuery.Where(r => !r.Muaf && r.Kalan <= 0m),
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

            var oncekiPasifFlag = _db.IncludeDeleted;
            try
            {
                _db.IncludeDeleted = includePasif;

                var dtoQuery = await BuildAidatRaporDtoQueryAsync(
                    yil, bas, bit, query, birimId, durum, tarifeYil, includePasif, ct);

                var aktifQuery = dtoQuery.Where(x => !x.Muaf);

                var toplamBorc = await aktifQuery.SumAsync(x => (decimal?)x.BorcGosterim, ct) ?? 0m;
                var toplamOdenen = await dtoQuery.SumAsync(x => (decimal?)x.GosterilenOdenen, ct) ?? 0m; // muaflar dahil
                var toplamKalan = await aktifQuery.SumAsync(x => (decimal?)x.Kalan, ct) ?? 0m;

                var orderedQuery = dtoQuery
                    .OrderBy(x => x.OgrenciSinif)
                    .ThenBy(x => x.OgrenciAdSoyad);

                var paged = await SayfalanmisListeModel<AidatRaporDto>.CreateAsync(
                    orderedQuery,
                    page,
                    pageSize,
                    ct);

                var kullanilabilirYillar = await _db.OgrenciAidatlar
                    .AsNoTracking()
                    .Select(a => a.BaslangicYil)
                    .Distinct()
                    .OrderByDescending(x => x)
                    .ToListAsync(ct);

                var seciliYil = yil ?? AkademikDonemHelper.Current();

                var tarife = await _db.OgrenciAidatTarifeler
                    .AsNoTracking()
                    .Where(t => t.BaslangicYil == seciliYil)
                    .Select(t => new TarifeDto { Yil = t.BaslangicYil, Tutar = t.Tutar, Aciklama = t.Aciklama })
                    .FirstOrDefaultAsync(ct);

                var odemesiOlanSayisi = await TarifeyeBagliOdeyenler(seciliYil).CountAsync(ct);

                return new AidatRaporSonucDto
                {
                    Satirlar = paged,
                    ToplamBorc = toplamBorc,
                    ToplamOdenenGosterilen = toplamOdenen,
                    ToplamKalan = toplamKalan,
                    KullanilabilirYillar = kullanilabilirYillar,
                    Tarife = tarife,
                    OdemesiOlanSayisi = odemesiOlanSayisi
                };
            }
            finally
            {
                _db.IncludeDeleted = oncekiPasifFlag;
            }
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
            var oncekiPasifFlag = _db.IncludeDeleted;
            List<AidatRaporDto> rows;
            try
            {
                _db.IncludeDeleted = includePasif;

                var dtoQuery = await BuildAidatRaporDtoQueryAsync(
                    yil, bas, bit, query, birimId, durum, tarifeYil, includePasif, ct);

                rows = await dtoQuery
                    .OrderBy(x => x.OgrenciSinif)
                    .ThenBy(x => x.OgrenciAdSoyad)
                    .ToListAsync(ct);
            }
            finally
            {
                _db.IncludeDeleted = oncekiPasifFlag;
            }

            // ✅ Toplamlar (filtreli tüm kayıtlar üzerinden)
            var aktif = rows.Where(x => !x.Muaf).ToList();
            var toplamBorc = aktif.Sum(x => x.BorcGosterim);
            var toplamOdenen = rows.Sum(x => x.GosterilenOdenen); // muaflar dahil
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
            // Aynı öğrenciye aynı anda ilk ödeme girilirse aidat kaydı unique index'e takılır;
            // bir kez daha denendiğinde diğer isteğin oluşturduğu kayıt bulunur.
            for (var deneme = 1; ; deneme++)
            {
                try
                {
                    return await OdemeEkleIcAsync(dto, ct);
                }
                catch (DbUpdateException) when (deneme == 1)
                {
                    _db.ChangeTracker.Clear();
                }
            }
        }

        private async Task<OdemeSatiriDto> OdemeEkleIcAsync(AidatOdemeEkleDto dto, CancellationToken ct)
        {
            var aidat = await _db.OgrenciAidatlar
                .FirstOrDefaultAsync(a => a.OgrenciId == dto.OgrenciId && a.BaslangicYil == dto.Yil, ct);

            if (aidat == null)
            {
                // Borc = null => yılın tarifesi uygulanır
                aidat = new OgrenciAidatModel
                {
                    OgrenciId = dto.OgrenciId,
                    BaslangicYil = dto.Yil,
                    Muaf = false
                };
                _db.OgrenciAidatlar.Add(aidat);
            }
            else if (aidat.Muaf)
            {
                throw new InvalidOperationException("Muaf öğrenciye ödeme eklenemez.");
            }

            var ent = new OgrenciAidatOdemeModel
            {
                OgrenciAidat = aidat,
                OdemeTarihi = dto.Tarih,
                Tutar = dto.Tutar,
                Aciklama = dto.Aciklama
            };
            _db.OgrenciAidatOdemeler.Add(ent);

            // Aidat kaydı ve ödeme tek SaveChanges ile (tek transaction) yazılır.
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
            var ent = await _db.OgrenciAidatOdemeler
                .FirstOrDefaultAsync(p => p.OgrenciAidatOdemeId == ogrenciAidatOdemeId, ct);

            if (ent is null)
                return false;

            ent.IsDeleted = true;
            await _db.SaveChangesAsync(ct);
            return true;
        }

        // ---------------------- Muafiyet İşlemleri ----------------------
        public async Task<bool> SetYillikMuafiyetAsync(int ogrenciId, int yil, bool muaf, CancellationToken ct = default)
        {
            var aidat = await _db.OgrenciAidatlar
                .FirstOrDefaultAsync(a => a.OgrenciId == ogrenciId && a.BaslangicYil == yil, ct);

            if (aidat == null)
            {
                aidat = new OgrenciAidatModel
                {
                    OgrenciId = ogrenciId,
                    BaslangicYil = yil,
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

        public async Task<bool> OgrenciBorcAyarlaAsync(int ogrenciId, int yil, decimal? borc, CancellationToken ct = default)
        {
            if (borc is < 0m)
                return false;

            var aidat = await _db.OgrenciAidatlar
                .FirstOrDefaultAsync(a => a.OgrenciId == ogrenciId && a.BaslangicYil == yil, ct);

            if (aidat == null)
            {
                if (borc is null)
                    return true; // kayıt yok => zaten tarife uygulanıyor

                aidat = new OgrenciAidatModel
                {
                    OgrenciId = ogrenciId,
                    BaslangicYil = yil,
                    Muaf = false
                };
                _db.OgrenciAidatlar.Add(aidat);
            }

            aidat.Borc = borc.HasValue
                ? decimal.Round(borc.Value, 2, MidpointRounding.AwayFromZero)
                : null;

            await _db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<TarifeDto> TarifeKaydetAsync(TarifeDto dto, CancellationToken ct = default)
        {
            var strategy = _db.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _db.Database.BeginTransactionAsync(ct);

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
                    // Ödeme yapmış öğrencileri eski tarifede sabitle (özel tutar olarak yaz)
                    if (dto.OdeyenleriSabitle && tarife.Tutar != dto.Tutar)
                    {
                        var eskiTutar = tarife.Tutar;
                        await TarifeyeBagliOdeyenler(dto.Yil)
                            .ExecuteUpdateAsync(s => s.SetProperty(a => a.Borc, (decimal?)eskiTutar), ct);
                    }

                    tarife.Tutar = dto.Tutar;
                    tarife.Aciklama = dto.Aciklama;
                }

                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            });

            _cache.Remove(TarifeCacheKey(dto.Yil));
            return dto;
        }
    }
}