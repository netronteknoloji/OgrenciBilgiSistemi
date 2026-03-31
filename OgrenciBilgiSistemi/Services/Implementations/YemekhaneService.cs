using System.Globalization;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using OgrenciBilgiSistemi.Data;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Services.Interfaces;
using OgrenciBilgiSistemi.ViewModels;

namespace OgrenciBilgiSistemi.Services.Implementations
{
    public class YemekhaneService : IYemekhaneService
    {
        private readonly AppDbContext _ctx;
        private readonly ILogger<YemekhaneService> _logger;

        public YemekhaneService(AppDbContext ctx, ILogger<YemekhaneService> logger)
        {
            _ctx = ctx;
            _logger = logger;
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // Yardımcılar
        // ─────────────────────────────────────────────────────────────────────────────
        private static (int yil, int ay) GetCurrentYearMonth()
        {
            var t = DateTime.Today;
            return (t.Year, t.Month);
        }

        private static IEnumerable<(int AkIndex, string Ad, int YilGercek, int AyGercek)>
            BuildAkademikAylar(int akademikYil)
        {
            var tr = new CultureInfo("tr-TR");
            for (int m = 9, i = 1; m <= 12; m++, i++)
                yield return (i, new DateTime(akademikYil, m, 1).ToString("MMMM", tr), akademikYil, m);
            for (int m = 1, i = 5; m <= 8; m++, i++)
                yield return (i, new DateTime(akademikYil + 1, m, 1).ToString("MMMM", tr), akademikYil + 1, m);
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // Tarife (akademik yıl başlangıcı: Eyl–Ağu)
        // ─────────────────────────────────────────────────────────────────────────────
        public Task<OgrenciYemekTarifeModel?> GetTarifeAsync(int ogrenciId, int akademikYil, CancellationToken ct = default)
            => _ctx.OgrenciYemekTarifeler
                   .AsNoTracking()
                   .FirstOrDefaultAsync(x => x.OgrenciId == ogrenciId && x.Yil == akademikYil, ct);

        public async Task SetTarifeAsync(int ogrenciId, int akademikYil, decimal aylikTutar, string? aciklama = null, CancellationToken ct = default)
        {
            var t = await _ctx.OgrenciYemekTarifeler
                              .FirstOrDefaultAsync(x => x.OgrenciId == ogrenciId && x.Yil == akademikYil, ct);

            if (t == null)
            {
                t = new OgrenciYemekTarifeModel
                {
                    OgrenciId = ogrenciId,
                    Yil = akademikYil,
                    AylikTutar = aylikTutar,
                    Aciklama = aciklama
                };
                _ctx.OgrenciYemekTarifeler.Add(t);
            }
            else
            {
                t.AylikTutar = aylikTutar;
                t.Aciklama = aciklama;
            }

            await _ctx.SaveChangesAsync(ct);
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // Ay durumu (takvim yılı/ay)
        // ─────────────────────────────────────────────────────────────────────────────
        public async Task<OgrenciYemekModel> SetAyAsync(int ogrenciId, int yil, int ay, bool aktif, string? not = null, CancellationToken ct = default)
        {
            var kayit = await _ctx.OgrenciYemekler
                .FirstOrDefaultAsync(x => x.OgrenciId == ogrenciId && x.Yil == yil && x.Ay == ay, ct);

            if (kayit == null)
            {
                kayit = new OgrenciYemekModel { OgrenciId = ogrenciId, Yil = yil, Ay = ay, Aktif = aktif, Not = not };
                _ctx.OgrenciYemekler.Add(kayit);
            }
            else
            {
                kayit.Aktif = aktif;
                kayit.Not = not;
                _ctx.OgrenciYemekler.Update(kayit);
            }

            await _ctx.SaveChangesAsync(ct);
            _logger.LogInformation("Yemek SetAy: OgrenciId={Id}, {Yil}-{Ay}, Aktif={Aktif}", ogrenciId, yil, ay, aktif);
            return kayit;
        }

        public Task<OgrenciYemekModel?> GetAyAsync(int ogrenciId, int yil, int ay, CancellationToken ct = default)
            => _ctx.OgrenciYemekler
                   .AsNoTracking()
                   .FirstOrDefaultAsync(x => x.OgrenciId == ogrenciId && x.Yil == yil && x.Ay == ay, ct);

        public async Task<OgrenciYemekModel> SetBuAyAsync(int ogrenciId, bool aktif, string? not = null, CancellationToken ct = default)
        {
            var (yil, ay) = GetCurrentYearMonth();
            var kayit = await _ctx.OgrenciYemekler
                .FirstOrDefaultAsync(x => x.OgrenciId == ogrenciId && x.Yil == yil && x.Ay == ay, ct);

            if (kayit == null)
            {
                kayit = new OgrenciYemekModel { OgrenciId = ogrenciId, Yil = yil, Ay = ay, Aktif = aktif, Not = not };
                _ctx.OgrenciYemekler.Add(kayit);
            }
            else
            {
                kayit.Aktif = aktif;
                kayit.Not = not;
                _ctx.OgrenciYemekler.Update(kayit);
            }

            await _ctx.SaveChangesAsync(ct);
            _logger.LogInformation("Yemek SetBuAy: OgrenciId={Id}, {Yil}-{Ay}, Aktif={Aktif}", ogrenciId, yil, ay, aktif);
            return kayit;
        }

        public async Task<bool?> GetBuAyDurumAsync(int ogrenciId, CancellationToken ct = default)
        {
            var (yil, ay) = GetCurrentYearMonth();
            return await _ctx.OgrenciYemekler
                .AsNoTracking()
                .Where(x => x.OgrenciId == ogrenciId && x.Yil == yil && x.Ay == ay)
                .Select(x => (bool?)x.Aktif)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<OgrenciYemekModel> ToggleBuAyAsync(int ogrenciId, CancellationToken ct = default)
        {
            var (yil, ay) = GetCurrentYearMonth();
            var kayit = await _ctx.OgrenciYemekler
                .FirstOrDefaultAsync(x => x.OgrenciId == ogrenciId && x.Yil == yil && x.Ay == ay, ct);

            if (kayit == null)
            {
                kayit = new OgrenciYemekModel
                {
                    OgrenciId = ogrenciId,
                    Yil = yil,
                    Ay = ay,
                    Aktif = true
                };
                _ctx.OgrenciYemekler.Add(kayit);
            }
            else
            {
                kayit.Aktif = !kayit.Aktif;
                _ctx.OgrenciYemekler.Update(kayit);
            }

            await _ctx.SaveChangesAsync(ct);
            _logger.LogInformation("Yemek ToggleBuAy: OgrenciId={Id}, {Yil}-{Ay}, YeniAktif={Aktif}", ogrenciId, yil, ay, kayit.Aktif);
            return kayit;
        }

        public async Task<Dictionary<int, bool>> GetBuAyDurumlariAsync(IEnumerable<int> ogrenciIdleri, CancellationToken ct = default)
        {
            var ids = ogrenciIdleri?.Distinct().ToList() ?? new List<int>();
            if (ids.Count == 0) return new Dictionary<int, bool>();

            var (yil, ay) = GetCurrentYearMonth();

            var list = await _ctx.OgrenciYemekler
                .AsNoTracking()
                .Where(x => ids.Contains(x.OgrenciId) && x.Yil == yil && x.Ay == ay)
                .Select(x => new { x.OgrenciId, x.Aktif })
                .ToListAsync(ct);

            return list.GroupBy(x => x.OgrenciId)
                       .ToDictionary(g => g.Key, g => g.First().Aktif);
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // Ödemeler (akademik yılın tamamı: Eyl..Ara + Oca..Ağu)
        // ─────────────────────────────────────────────────────────────────────────────
        public Task<List<OgrenciYemekOdemeModel>> GetAkademikYilOdemeleriAsync(int ogrenciId, int akademikYil, CancellationToken ct = default)
        {
            var y1 = akademikYil;       // Eyl..Ara
            var y2 = akademikYil + 1;   // Oca..Ağu

            return _ctx.OgrenciYemekOdemeler
                .Where(x => x.OgrenciId == ogrenciId &&
                            (x.Yil == y1 && x.Ay >= 9 && x.Ay <= 12 ||
                             x.Yil == y2 && x.Ay >= 1 && x.Ay <= 8))
                .OrderByDescending(x => x.Tarih)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task OdemeEkleAsync(int ogrenciId, int yil, int ay, decimal tutar, DateTime? tarih, string? aciklama, CancellationToken ct = default)
        {
            var od = new OgrenciYemekOdemeModel
            {
                OgrenciId = ogrenciId,
                Yil = yil,
                Ay = ay,
                Tutar = tutar,
                Tarih = tarih ?? DateTime.Now,
                Aciklama = aciklama
            };

            _ctx.OgrenciYemekOdemeler.Add(od);
            await _ctx.SaveChangesAsync(ct);
            _logger.LogInformation("Yemek OdemeEkle: OgrenciId={Id}, {Yil}-{Ay}, Tutar={Tutar}", ogrenciId, yil, ay, tutar);
        }

        public async Task OdemeSilAsync(int odemeId, CancellationToken ct = default)
        {
            var od = await _ctx.OgrenciYemekOdemeler
                .FirstOrDefaultAsync(x => x.OgrenciYemekOdemeId == odemeId, ct);

            if (od != null)
            {
                od.AktifMi = false;
                await _ctx.SaveChangesAsync(ct);
                _logger.LogInformation("Yemek OdemeSil: OdemeId={Id}", odemeId);
            }
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // Özet (akademik yıl)
        // ─────────────────────────────────────────────────────────────────────────────
        public async Task<YemekhaneOzetVm> GetOzetAsync(int ogrenciId, int akademikYil, CancellationToken ct = default)
        {
            var tarife = await GetTarifeAsync(ogrenciId, akademikYil, ct);
            var aylikTutar = tarife?.AylikTutar ?? 0m;

            var y1 = akademikYil;       // Eyl..Ara
            var y2 = akademikYil + 1;   // Oca..Ağu

            var ayKayitlari = await _ctx.OgrenciYemekler
                .Where(x => x.OgrenciId == ogrenciId
                         && (x.Yil == y1 && x.Ay >= 9 && x.Ay <= 12 ||
                             x.Yil == y2 && x.Ay >= 1 && x.Ay <= 8))
                .AsNoTracking()
                .ToListAsync(ct);

            var odemeler = await _ctx.OgrenciYemekOdemeler
                .Where(x => x.OgrenciId == ogrenciId
                         && (x.Yil == y1 && x.Ay >= 9 && x.Ay <= 12 ||
                             x.Yil == y2 && x.Ay >= 1 && x.Ay <= 8))
                .AsNoTracking()
                .ToListAsync(ct);

            var vm = new YemekhaneOzetVm
            {
                OgrenciId = ogrenciId,
                Yil = akademikYil,
                AylikTutar = aylikTutar,
            };

            decimal toplamBorc = 0m;

            foreach (var slot in BuildAkademikAylar(akademikYil))
            {
                var kayit = ayKayitlari.FirstOrDefault(k => k.Yil == slot.YilGercek && k.Ay == slot.AyGercek);
                var aktif = kayit?.Aktif ?? false;

                var odenenAy = odemeler
                    .Where(o => o.Yil == slot.YilGercek && o.Ay == slot.AyGercek)
                    .Sum(o => o.Tutar);

                var borcAy = aktif ? aylikTutar : 0m;

                vm.Aylar.Add(new YemekhaneOzetAyVm
                {
                    AkAyIndex = slot.AkIndex,
                    AyAd = slot.Ad,
                    YilGercek = slot.YilGercek,
                    AyGercek = slot.AyGercek,
                    Aktif = aktif,
                    AylikTutar = aylikTutar,
                    Borc = borcAy,
                    Odenen = odenenAy,
                    Not = kayit?.Not
                });

                toplamBorc += borcAy;
            }

            vm.ToplamBorc = toplamBorc;
            vm.ToplamOdenen = odemeler.Sum(x => x.Tutar);
            return vm;
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // Toplu Rapor (TARİH BAZLI) – View/Controller ile uyumlu
        // ─────────────────────────────────────────────────────────────────────────────
        public async Task<YemekhaneRaporVm> GetTopluRaporAsync(
            DateTime? bas,
            DateTime? bit,
            string? q,
            int page,
            int pageSize,
            CancellationToken ct = default)
        {
            // 1) Tarih aralığı (ödemeler için DateTime)
            var startDate = bas?.Date;                // dahil
            var endDateExcl = bit?.Date.AddDays(1);   // hariç  -> [startDate, endDateExcl)

            // Ay kayıtlarını yıl/ay ile daraltmak için
            int? sy = bas?.Year, sm = bas?.Month;
            int? ey = bit?.Year, em = bit?.Month;

            // 2) Baz sorgu: ÖDEMELER üzerinden distinct (OgrenciId, AkYil) seti
            var baseQuery =
                from o in _ctx.OgrenciYemekOdemeler.AsNoTracking()
                where (!startDate.HasValue || o.Tarih >= startDate.Value)
                   && (!endDateExcl.HasValue || o.Tarih < endDateExcl.Value)
                join s in _ctx.Ogrenciler.AsNoTracking() on o.OgrenciId equals s.OgrenciId
                where string.IsNullOrWhiteSpace(q) || EF.Functions.Like(s.OgrenciAdSoyad, $"%{q}%")
                let akYil = o.Tarih.Month >= 9 ? o.Tarih.Year : o.Tarih.Year - 1
                select new { o.OgrenciId, AkYil = akYil, s.OgrenciAdSoyad };

            var distinct = baseQuery.Distinct();

            // 3) Sayfalama
            var totalRows = await distinct.CountAsync(ct);
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalRows / (double)pageSize));
            page = Math.Min(Math.Max(page, 1), totalPages);

            var rows = await distinct
                .OrderByDescending(x => x.AkYil)
                .ThenBy(x => x.OgrenciAdSoyad)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            // 4) Satırlar
            var satirlar = new List<YemekhaneRaporSatirVm>();
            foreach (var row in rows)
            {
                int y1 = row.AkYil;       // Eyl..Ara
                int y2 = row.AkYil + 1;   // Oca..Ağu

                // Aylık tarife
                var aylikTutar = await _ctx.OgrenciYemekTarifeler.AsNoTracking()
                    .Where(t => t.OgrenciId == row.OgrenciId && t.Yil == y1)
                    .Select(t => (decimal?)t.AylikTutar)
                    .FirstOrDefaultAsync(ct) ?? 0m;

                // Aktif ay sayısı — seçilen tarih aralığına düşen aylar
                var aktifAy = await _ctx.OgrenciYemekler.AsNoTracking()
                    .Where(a => a.OgrenciId == row.OgrenciId && a.Aktif
                             && (a.Yil == y1 && a.Ay >= 9 && a.Ay <= 12 || a.Yil == y2 && a.Ay >= 1 && a.Ay <= 8)
                             && (!sy.HasValue || a.Yil > sy.Value || a.Yil == sy.Value && a.Ay >= sm.Value)
                             && (!ey.HasValue || a.Yil < ey.Value || a.Yil == ey.Value && a.Ay <= em.Value))
                    .CountAsync(ct);

                var borc = aylikTutar * aktifAy;

                // Ödenen — yalnızca ödeme tarihine göre + aynı akademik yıl
                var odenen = await _ctx.OgrenciYemekOdemeler.AsNoTracking()
                    .Where(o => o.OgrenciId == row.OgrenciId
                        && (!startDate.HasValue || o.Tarih >= startDate.Value)
                        && (!endDateExcl.HasValue || o.Tarih < endDateExcl.Value)
                        && (o.Tarih.Month >= 9 ? o.Tarih.Year : o.Tarih.Year - 1) == row.AkYil)
                    .SumAsync(o => (decimal?)o.Tutar, ct) ?? 0m;

                satirlar.Add(new YemekhaneRaporSatirVm
                {
                    OgrenciId = row.OgrenciId,
                    OgrenciAdSoyad = row.OgrenciAdSoyad,
                    Yil = row.AkYil,
                    AylikTutar = aylikTutar,
                    AktifAySayisi = aktifAy,
                    Borc = borc,
                    Odenen = odenen
                });
            }

            // Bilgi amaçlı yıl seti
            var yilSet = await _ctx.OgrenciYemekOdemeler.AsNoTracking()
                .Where(o => (!startDate.HasValue || o.Tarih >= startDate.Value)
                         && (!endDateExcl.HasValue || o.Tarih < endDateExcl.Value))
                .Select(o => o.Tarih.Month >= 9 ? o.Tarih.Year : o.Tarih.Year - 1)
                .Distinct()
                .OrderByDescending(x => x)
                .ToListAsync(ct);

            var listeVm = new YemekhaneRaporListeVm
            {
                KullanilabilirYillar = yilSet,
                SayfaIndeks = page,
                ToplamSayfa = totalPages,
                SayfaBoyutu = pageSize,
                Satirlar = satirlar,
                ToplamBorc = satirlar.Sum(s => s.Borc),
                ToplamOdenen = satirlar.Sum(s => s.Odenen)
            };

            return new YemekhaneRaporVm
            {
                Yil = null,
                Query = q,
                Rapor = listeVm
            };
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // Excel Export (tarih bazlı)
        // ─────────────────────────────────────────────────────────────────────────────
        public async Task<byte[]> ExportTopluRaporExcelAsync(DateTime? bas, DateTime? bit, string? q, CancellationToken ct = default)
        {
            // Tüm sonuçlar: dev pageSize
            var vm = await GetTopluRaporAsync(bas, bit, q, page: 1, pageSize: int.MaxValue, ct);

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Yemekhane Raporu");

            // Başlıklar
            ws.Cell(1, 1).Value = "Akad. Yıl";
            ws.Cell(1, 2).Value = "Öğrenci";
            ws.Cell(1, 3).Value = "Aylık Tarife";
            ws.Cell(1, 4).Value = "Aktif Ay";
            ws.Cell(1, 5).Value = "Borç";
            ws.Cell(1, 6).Value = "Ödenen";
            ws.Cell(1, 7).Value = "Kalan";

            var header = ws.Range(1, 1, 1, 7);
            header.Style.Font.Bold = true;
            header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            int row = 2;
            var nf = "#,##0.00 ₺";

            foreach (var s in vm.Rapor.Satirlar
                                   .OrderByDescending(x => x.Yil)
                                   .ThenBy(x => x.OgrenciAdSoyad))
            {
                ws.Cell(row, 1).Value = s.Yil;
                ws.Cell(row, 2).Value = s.OgrenciAdSoyad;

                ws.Cell(row, 3).Value = s.AylikTutar;
                ws.Cell(row, 3).Style.NumberFormat.Format = nf;

                ws.Cell(row, 4).Value = s.AktifAySayisi;

                ws.Cell(row, 5).Value = s.Borc;
                ws.Cell(row, 5).Style.NumberFormat.Format = nf;

                ws.Cell(row, 6).Value = s.Odenen;
                ws.Cell(row, 6).Style.NumberFormat.Format = nf;

                var kalan = s.Borc - s.Odenen;
                ws.Cell(row, 7).Value = kalan;
                ws.Cell(row, 7).Style.NumberFormat.Format = nf;

                row++;
            }

            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }
    }
}
