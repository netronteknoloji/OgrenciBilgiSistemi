using System.Globalization;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using OgrenciBilgiSistemi.Data;
using OgrenciBilgiSistemi.Dtos;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Services.Interfaces;
using OgrenciBilgiSistemi.Shared.Kurallar;
using OgrenciBilgiSistemi.ViewModels;

namespace OgrenciBilgiSistemi.Services.Implementations
{
    public class YemekhaneService : IYemekhaneService
    {
        private readonly AppDbContext _ctx;
        private readonly ILogger<YemekhaneService> _logger;
        private readonly ICihazSenkronService _cihazSenkron;

        public YemekhaneService(AppDbContext ctx, ILogger<YemekhaneService> logger, ICihazSenkronService cihazSenkron)
        {
            _ctx = ctx;
            _logger = logger;
            _cihazSenkron = cihazSenkron;
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

            // İçinde bulunulan ayın hakkı değiştiyse yemekhane cihazına eklenir/silinir
            if ((yil, ay) == GetCurrentYearMonth())
            {
                await _cihazSenkron.KuyrugaEkleAsync(ogrenciId, ct);
                _cihazSenkron.Tetikle();
            }
            return kayit;
        }

        public async Task<int> SetAyTopluAsync(IReadOnlyCollection<int> ogrenciIdler, int yil, int ay, bool aktif, CancellationToken ct = default)
        {
            var ids = ogrenciIdler.Distinct().ToList();
            if (ids.Count == 0) return 0;

            var buAyMi = (yil, ay) == GetCurrentYearMonth();

            var strategy = _ctx.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _ctx.Database.BeginTransactionAsync(ct);

                var mevcut = await _ctx.OgrenciYemekler
                    .Where(x => ids.Contains(x.OgrenciId) && x.Yil == yil && x.Ay == ay)
                    .ToListAsync(ct);

                AyHakkiYaz(mevcut, ids, yil, ay, aktif);
                await _ctx.SaveChangesAsync(ct);

                // İçinde bulunulan ayın hakkı değiştiyse yemekhane cihazına eklenir/silinir
                if (buAyMi)
                {
                    foreach (var id in ids)
                        await _cihazSenkron.KuyrugaEkleAsync(id, ct);
                }

                await tx.CommitAsync(ct);
            });

            if (buAyMi)
                _cihazSenkron.Tetikle();

            _logger.LogInformation("Yemek SetAyToplu: {Adet} öğrenci, {Yil}-{Ay}, Aktif={Aktif}", ids.Count, yil, ay, aktif);
            return ids.Count;
        }

        // Upsert: mevcut kaydın notu korunur; yalnızca hak değişir. Durumu gerçekten değişen öğrencileri döner.
        private List<int> AyHakkiYaz(List<OgrenciYemekModel> mevcut, IEnumerable<int> ids, int yil, int ay, bool aktif)
        {
            var degisen = new List<int>();
            foreach (var id in ids)
            {
                var kayit = mevcut.FirstOrDefault(x => x.OgrenciId == id);
                if (kayit == null)
                {
                    _ctx.OgrenciYemekler.Add(new OgrenciYemekModel { OgrenciId = id, Yil = yil, Ay = ay, Aktif = aktif });
                    if (aktif) degisen.Add(id);   // kayıt yoksa hak yok sayılır
                }
                else if (kayit.Aktif != aktif)
                {
                    kayit.Aktif = aktif;
                    degisen.Add(id);
                }
            }
            return degisen;
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // Excel'den ay listesi: A sütunu öğrenci no; dosya o ayın tam listesidir
        // ─────────────────────────────────────────────────────────────────────────────
        public async Task<YemekHakkiIceAktarmaSonucDto> ExceldenAyHakkiYukleAsync(Stream dosya, int yil, int ay, CancellationToken ct = default)
        {
            List<string?> hucreler;
            try
            {
                using var wb = new XLWorkbook(dosya);
                var ws = wb.Worksheets.First();
                var sonSatir = ws.LastRowUsed()?.RowNumber() ?? 0;
                hucreler = Enumerable.Range(1, sonSatir)
                    .Select(r => (string?)ws.Cell(r, 1).GetFormattedString())
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Yemek Excel okunamadı: {Yil}-{Ay}", yil, ay);
                return new YemekHakkiIceAktarmaSonucDto { Hata = "Excel dosyası okunamadı. .xlsx biçiminde ve ilk sayfanın A sütununda öğrenci no olmalı." };
            }

            // Aktif öğrenciler (global filtre pasifleri dışlar); no unique değil
            var ogrenciler = await _ctx.Ogrenciler.AsNoTracking()
                .Select(o => new { o.OgrenciNo, o.OgrenciId })
                .ToListAsync(ct);
            var noSozlugu = ogrenciler
                .GroupBy(o => o.OgrenciNo)
                .ToDictionary(g => g.Key, g => (IReadOnlyList<int>)g.Select(o => o.OgrenciId).ToList());

            var eslesme = YemekHakkiIceAktarmaKurallari.Esle(hucreler, noSozlugu);

            var sonuc = new YemekHakkiIceAktarmaSonucDto
            {
                Bulunamayan = eslesme.Bulunamayan,
                BirdenFazla = eslesme.BirdenFazla,
                GecersizSatirlar = eslesme.GecersizSatirlar
            };

            // Boş/yanlış dosya herkesin hakkını kapatmasın
            if (eslesme.AcilacakIdler.Count == 0)
            {
                sonuc.Hata = "Dosyada eşleşen öğrenci no bulunamadı; hiçbir değişiklik yapılmadı.";
                return sonuc;
            }

            var listedekiler = eslesme.AcilacakIdler.ToHashSet();
            var buAyMi = (yil, ay) == GetCurrentYearMonth();
            var degisen = new List<int>();

            var strategy = _ctx.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                degisen.Clear();
                sonuc.Acilan = sonuc.Kapatilan = 0;
                await using var tx = await _ctx.Database.BeginTransactionAsync(ct);

                var mevcut = await _ctx.OgrenciYemekler
                    .Where(x => x.Yil == yil && x.Ay == ay)
                    .ToListAsync(ct);

                var acilan = AyHakkiYaz(mevcut, listedekiler, yil, ay, aktif: true);
                var kapatilacak = mevcut.Where(x => x.Aktif && !listedekiler.Contains(x.OgrenciId))
                                        .Select(x => x.OgrenciId)
                                        .ToList();
                var kapatilan = AyHakkiYaz(mevcut, kapatilacak, yil, ay, aktif: false);
                await _ctx.SaveChangesAsync(ct);

                degisen.AddRange(acilan);
                degisen.AddRange(kapatilan);

                // İçinde bulunulan ayın hakkı değiştiyse yemekhane cihazına eklenir/silinir
                if (buAyMi)
                {
                    foreach (var id in degisen)
                        await _cihazSenkron.KuyrugaEkleAsync(id, ct);
                }

                await tx.CommitAsync(ct);
                sonuc.Acilan = acilan.Count;
                sonuc.Kapatilan = kapatilan.Count;
            });

            if (buAyMi && degisen.Count > 0)
                _cihazSenkron.Tetikle();

            sonuc.Degismeyen = listedekiler.Count - sonuc.Acilan;
            _logger.LogInformation("Yemek Excel yükleme: {Yil}-{Ay}, Açılan={Acilan}, Kapatılan={Kapatilan}, Bulunamayan={Bulunamayan}",
                yil, ay, sonuc.Acilan, sonuc.Kapatilan, sonuc.Bulunamayan.Count);
            return sonuc;
        }

        public byte[] ExcelSablonu()
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Yemek Listesi");
            ws.Cell(1, 1).Value = "Öğrenci No";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Column(1).Width = 16;

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }

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
                od.IsDeleted = true;
                await _ctx.SaveChangesAsync(ct);
                _logger.LogInformation("Yemek OdemeSil: OdemeId={Id}", odemeId);
            }
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // Özet (akademik yıl)
        // ─────────────────────────────────────────────────────────────────────────────
        public async Task<YemekhaneOzetVm> GetOzetAsync(int ogrenciId, int akademikYil, CancellationToken ct = default)
        {
            var y1 = akademikYil;       // Eyl..Ara
            var y2 = akademikYil + 1;   // Oca..Ağu

            var ayKayitlari = await _ctx.OgrenciYemekler
                .Where(x => x.OgrenciId == ogrenciId
                         && (x.Yil == y1 && x.Ay >= 9 && x.Ay <= 12 ||
                             x.Yil == y2 && x.Ay >= 1 && x.Ay <= 8))
                .AsNoTracking()
                .ToListAsync(ct);

            var vm = new YemekhaneOzetVm
            {
                OgrenciId = ogrenciId,
                Yil = akademikYil,
            };

            foreach (var slot in BuildAkademikAylar(akademikYil))
            {
                var kayit = ayKayitlari.FirstOrDefault(k => k.Yil == slot.YilGercek && k.Ay == slot.AyGercek);

                vm.Aylar.Add(new YemekhaneOzetAyVm
                {
                    AkAyIndex = slot.AkIndex,
                    AyAd = slot.Ad,
                    YilGercek = slot.YilGercek,
                    AyGercek = slot.AyGercek,
                    Aktif = kayit?.Aktif ?? false,
                    Not = kayit?.Not
                });
            }

            return vm;
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // Yemekhane Index — seçilen ayda öğrencilerin yemek hakkı (takvim yılı/ay)
        // ─────────────────────────────────────────────────────────────────────────────
        public async Task<YemekhaneAylikTakipSonucDto> GetAylikTakipAsync(
            int yil,
            int ay,
            string? query,
            int? birimId,
            YemekHakkiFiltresi durum = YemekHakkiFiltresi.Hepsi,
            int page = 1,
            int pageSize = 50,
            bool includePasif = false,
            CancellationToken ct = default)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 5, 200);

            var oncekiPasifFlag = _ctx.IncludeDeleted;
            if (includePasif)
                _ctx.IncludeDeleted = true;

            try
            {
                // 1) Öğrenci listesi (filtreli)
                var ogrenciQ = _ctx.Ogrenciler.AsNoTracking();

                if (birimId.HasValue)
                    ogrenciQ = ogrenciQ.Where(o => o.BirimId == birimId.Value);

                if (!string.IsNullOrWhiteSpace(query))
                {
                    var qLike = $"%{query.Trim()}%";
                    ogrenciQ = ogrenciQ.Where(o =>
                        EF.Functions.Like(o.OgrenciAdSoyad, qLike) ||
                        EF.Functions.Like(o.OgrenciNo.ToString(), qLike));
                }

                var ogrenciler = await ogrenciQ
                    .Select(o => new
                    {
                        o.OgrenciId,
                        o.OgrenciAdSoyad,
                        o.OgrenciNo,
                        SinifAd = o.Birim != null ? o.Birim.BirimAd : null
                    })
                    .ToListAsync(ct);

                var ogrenciIds = ogrenciler.Select(x => x.OgrenciId).ToList();

                // 2) Batch: seçilen ayın kayıtları
                var ayKayitlari = ogrenciIds.Count == 0
                    ? []
                    : await _ctx.OgrenciYemekler.AsNoTracking()
                        .Where(a => ogrenciIds.Contains(a.OgrenciId) && a.Yil == yil && a.Ay == ay)
                        .Select(a => new { a.OgrenciId, a.Aktif, a.Not })
                        .ToListAsync(ct);
                var ayDict = ayKayitlari.GroupBy(a => a.OgrenciId).ToDictionary(g => g.Key, g => g.First());

                // 3) Satırları derle (kayıt yoksa hak yok)
                var tumSatirlar = ogrenciler.Select(o =>
                {
                    ayDict.TryGetValue(o.OgrenciId, out var kayit);
                    return new YemekhaneIndexSatirDto
                    {
                        OgrenciId = o.OgrenciId,
                        OgrenciAdSoyad = o.OgrenciAdSoyad ?? "",
                        OgrenciNo = o.OgrenciNo.ToString(),
                        OgrenciSinif = o.SinifAd,
                        HakVar = kayit?.Aktif ?? false,
                        Not = kayit?.Not
                    };
                }).ToList();

                // 4) Sayılar (durum filtresinden önce)
                var hakVarSayisi = tumSatirlar.Count(s => s.HakVar);
                var hakYokSayisi = tumSatirlar.Count - hakVarSayisi;

                // 5) Durum filtresi
                tumSatirlar = durum switch
                {
                    YemekHakkiFiltresi.Var => tumSatirlar.Where(s => s.HakVar).ToList(),
                    YemekHakkiFiltresi.Yok => tumSatirlar.Where(s => !s.HakVar).ToList(),
                    _ => tumSatirlar
                };

                // 6) Sıralama + sayfalama (memory)
                var ordered = tumSatirlar
                    .OrderBy(s => s.OgrenciSinif)
                    .ThenBy(s => s.OgrenciAdSoyad)
                    .ToList();

                return new YemekhaneAylikTakipSonucDto
                {
                    Satirlar = SayfalanmisListeModel<YemekhaneIndexSatirDto>.FromList(ordered, page, pageSize),
                    HakVarSayisi = hakVarSayisi,
                    HakYokSayisi = hakYokSayisi
                };
            }
            finally
            {
                _ctx.IncludeDeleted = oncekiPasifFlag;
            }
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

            // 4) Batch sorgular — N+1 yerine 3 toplu sorgu
            var ogrenciIds = rows.Select(r => r.OgrenciId).Distinct().ToList();
            var akYillar = rows.Select(r => r.AkYil).Distinct().ToList();

            // Batch: Aylık tarifeler (OgrenciId, Yil) → AylikTutar
            var tarifeler = await _ctx.OgrenciYemekTarifeler.AsNoTracking()
                .Where(t => ogrenciIds.Contains(t.OgrenciId) && akYillar.Contains(t.Yil))
                .Select(t => new { t.OgrenciId, t.Yil, t.AylikTutar })
                .ToListAsync(ct);
            var tarifeDict = tarifeler.ToDictionary(t => (t.OgrenciId, t.Yil), t => t.AylikTutar);

            // Batch: Aktif ay sayıları (OgrenciId) → Count
            // Akademik yıl aralığını kapsayan tüm ay kayıtlarını çek, bellekte grupla
            // Sayfada satır yoksa (ödeme kaydı yok / filtre boş) Min/Max patlamasın; ogrenciIds boş olduğundan sorgu zaten boş döner
            var yilMin = akYillar.Count > 0 ? akYillar.Min() : 0;
            var yilMax = akYillar.Count > 0 ? akYillar.Max() + 1 : 0;
            var ayKayitlari = await _ctx.OgrenciYemekler.AsNoTracking()
                .Where(a => ogrenciIds.Contains(a.OgrenciId) && a.Aktif
                         && a.Yil >= yilMin && a.Yil <= yilMax
                         && (!sy.HasValue || a.Yil > sy.Value || a.Yil == sy.Value && a.Ay >= sm.Value)
                         && (!ey.HasValue || a.Yil < ey.Value || a.Yil == ey.Value && a.Ay <= em.Value))
                .Select(a => new { a.OgrenciId, a.Yil, a.Ay })
                .ToListAsync(ct);

            // Batch: Ödemeler (OgrenciId, AkYil) → Toplam
            var odemeler = await _ctx.OgrenciYemekOdemeler.AsNoTracking()
                .Where(o => ogrenciIds.Contains(o.OgrenciId)
                    && (!startDate.HasValue || o.Tarih >= startDate.Value)
                    && (!endDateExcl.HasValue || o.Tarih < endDateExcl.Value))
                .Select(o => new { o.OgrenciId, o.Tarih, o.Tutar })
                .ToListAsync(ct);
            var odemeDict = odemeler
                .GroupBy(o => (o.OgrenciId, AkYil: o.Tarih.Month >= 9 ? o.Tarih.Year : o.Tarih.Year - 1))
                .ToDictionary(g => g.Key, g => g.Sum(o => o.Tutar));

            // 5) Satırları oluştur — DB çağrısı yok, tamamen bellekte
            var satirlar = new List<YemekhaneRaporSatirVm>();
            foreach (var row in rows)
            {
                int y1 = row.AkYil;
                int y2 = row.AkYil + 1;

                var aylikTutar = tarifeDict.TryGetValue((row.OgrenciId, y1), out var t) ? t : 0m;

                var aktifAy = ayKayitlari.Count(a =>
                    a.OgrenciId == row.OgrenciId
                    && (a.Yil == y1 && a.Ay >= 9 && a.Ay <= 12 || a.Yil == y2 && a.Ay >= 1 && a.Ay <= 8));

                var borc = aylikTutar * aktifAy;

                var odenen = odemeDict.TryGetValue((row.OgrenciId, row.AkYil), out var od) ? od : 0m;

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
            // Tüm sonuçlar: makul üst sınır (N+1 batch sorgu ile artık güvenli)
            var vm = await GetTopluRaporAsync(bas, bit, q, page: 1, pageSize: 50_000, ct);

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
