using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using OgrenciBilgiSistemi.Data;
using OgrenciBilgiSistemi.Dtos;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Services.Interfaces;
using OgrenciBilgiSistemi.ViewModels;
using System.IO;

namespace OgrenciBilgiSistemi.Services.Implementations
{
    public class ZiyaretciService : IZiyaretciService
    {
        private readonly AppDbContext _db;

        public ZiyaretciService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<int> EkleAsync(ZiyaretciModel model, CancellationToken ct = default)
        {
            model.GirisZamani = DateTime.Now;
            model.AktifMi = true;

            _db.Ziyaretciler.Add(model);
            await _db.SaveChangesAsync(ct);

            return model.ZiyaretciId;
        }

        public async Task GuncelleAsync(ZiyaretciModel model, CancellationToken ct = default)
        {
            _db.Ziyaretciler.Update(model);
            await _db.SaveChangesAsync(ct);
        }

        public async Task CikisYapAsync(int ziyaretciId, CancellationToken ct = default)
        {
            var ziyaretci = await _db.Ziyaretciler
                .FirstOrDefaultAsync(z => z.ZiyaretciId == ziyaretciId, ct);

            if (ziyaretci == null)
                return;

            ziyaretci.CikisZamani = DateTime.Now;
            ziyaretci.AktifMi = false;

            await _db.SaveChangesAsync(ct);
        }

        public async Task SilAsync(int ziyaretciId, CancellationToken ct = default)
        {
            var ziyaretci = await _db.Ziyaretciler
                .FirstOrDefaultAsync(z => z.ZiyaretciId == ziyaretciId, ct);

            if (ziyaretci == null)
                return;

            // Soft-delete: fiziksel silme yerine pasife al
            ziyaretci.AktifMi = false;
            if (!ziyaretci.CikisZamani.HasValue)
                ziyaretci.CikisZamani = DateTime.Now;

            await _db.SaveChangesAsync(ct);
        }

        public async Task<ZiyaretciModel?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            return await _db.Ziyaretciler
                .Include(z => z.Kullanici)
                .FirstOrDefaultAsync(z => z.ZiyaretciId == id, ct);
        }

        public async Task<ZiyaretciModel?> GetAktifByKartAsync(string kartNo, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(kartNo))
                return null;

            var k = kartNo.Trim();

            return await _db.Ziyaretciler
                .Include(z => z.Kullanici)
                .AsNoTracking()
                .FirstOrDefaultAsync(z =>
                    z.KartNo == k &&
                    z.AktifMi, ct);
        }

        public async Task<SayfalanmisListeModel<ZiyaretciModel>> SearchPagedAsync(
            string? searchString,
            int page,
            int pageSize,
            bool sadeceAktif = true,
            int? kullaniciId = null,
            CancellationToken ct = default)
        {
            var q = _db.Ziyaretciler
                .Include(z => z.Kullanici)
                .AsNoTracking()
                .AsQueryable();

            if (sadeceAktif)
                q = q.Where(z => z.AktifMi);

            if (kullaniciId.HasValue)
                q = q.Where(z => z.KullaniciId == kullaniciId.Value);

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var s = searchString.Trim();

                q = q.Where(z =>
                    EF.Functions.Like(z.AdSoyad, $"%{s}%") ||
                    EF.Functions.Like(z.TcKimlikNo ?? "", $"%{s}%") ||
                    EF.Functions.Like(z.Telefon ?? "", $"%{s}%") ||
                    EF.Functions.Like(z.KartNo ?? "", $"%{s}%"));
            }

            q = q.OrderByDescending(z => z.GirisZamani);

            return await SayfalanmisListeModel<ZiyaretciModel>.CreateAsync(q, page, pageSize, ct);
        }

        public async Task<List<ZiyaretciModel>> GetZiyaretGecmisiAsync(
            string? tcKimlikNo,
            string adSoyad,
            CancellationToken ct = default)
        {
            var q = _db.Ziyaretciler
                .Include(z => z.Kullanici)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(tcKimlikNo))
            {
                q = q.Where(z => z.TcKimlikNo == tcKimlikNo);
            }
            else
            {
                q = q.Where(z => z.AdSoyad == adSoyad);
            }

            return await q
                .OrderByDescending(z => z.GirisZamani)
                .ToListAsync(ct);
        }

        public async Task<ZiyaretciKartOkumaViewModel> KartOkutAsync(string kartNo, CancellationToken ct = default)
        {
            var ziyaretci = await GetAktifByKartAsync(kartNo, ct);

            if (ziyaretci == null)
            {
                return new ZiyaretciKartOkumaViewModel
                {
                    KartNo = kartNo,
                    KartOkumaZamani = DateTime.Now,
                    Mesaj = "Bu karta ait aktif ziyaretçi bulunamadı.",
                    AktifMi = false
                };
            }

            return new ZiyaretciKartOkumaViewModel
            {
                ZiyaretciId = ziyaretci.ZiyaretciId,
                KartNo = ziyaretci.KartNo,
                KartOkumaZamani = DateTime.Now,
                AdSoyad = ziyaretci.AdSoyad,
                TcKimlikNo = ziyaretci.TcKimlikNo,
                Telefon = ziyaretci.Telefon,
                Adres = ziyaretci.Adres,
                KullaniciId = ziyaretci.KullaniciId,
                KullaniciAdi = ziyaretci.Kullanici?.KullaniciAdi,
                ZiyaretSebebi = ziyaretci.ZiyaretSebebi,
                KartVerildiMi = ziyaretci.KartVerildiMi,
                GirisZamani = ziyaretci.GirisZamani,
                CikisZamani = ziyaretci.CikisZamani,
                AktifMi = ziyaretci.AktifMi,
                Mesaj = ziyaretci.AktifMi
                    ? "Ziyaretçi şu anda içeride."
                    : "Ziyaretçi çıkış yapmış görünüyor."
            };
        }

        // ----------------------------------------------------
        // RAPOR ORTAK SORGU
        // ----------------------------------------------------
        private IQueryable<ZiyaretciModel> BuildRaporQuery(
            string? query,
            DateTime? startDate,
            DateTime? endDate)
        {
            var q = _db.Ziyaretciler
                .Include(z => z.Kullanici)
                .AsNoTracking()
                .AsQueryable();

            // Arama (ad, tc, telefon, adres)
            if (!string.IsNullOrWhiteSpace(query))
            {
                var s = query.Trim();
                q = q.Where(z =>
                    EF.Functions.Like(z.AdSoyad, $"%{s}%") ||
                    EF.Functions.Like(z.TcKimlikNo ?? "", $"%{s}%") ||
                    EF.Functions.Like(z.Telefon ?? "", $"%{s}%") ||
                    EF.Functions.Like(z.Adres ?? "", $"%{s}%"));
            }

            // Tarih aralığı (Giriş zamanı üzerinden)
            if (startDate.HasValue)
            {
                var d = startDate.Value.Date;
                q = q.Where(z => z.GirisZamani >= d);
            }

            if (endDate.HasValue)
            {
                var d = endDate.Value.Date.AddDays(1); // gün sonu dahil
                q = q.Where(z => z.GirisZamani < d);
            }

            return q;
        }

        // ----------------------------------------------------
        // RAPOR (LISTE)
        // ----------------------------------------------------
        public async Task<List<ZiyaretciRaporDto>> GetRaporAsync(
            string? query,
            DateTime? startDate,
            DateTime? endDate,
            CancellationToken ct = default)
        {
            var baseQuery = BuildRaporQuery(query, startDate, endDate);

            var list = await baseQuery
                .OrderByDescending(z => z.GirisZamani)
                .Select(z => new ZiyaretciRaporDto
                {
                    ZiyaretciId = z.ZiyaretciId,
                    AdSoyad = z.AdSoyad,
                    TcKimlikNo = z.TcKimlikNo,
                    Telefon = z.Telefon,
                    Adres = z.Adres,

                    KullaniciId = z.KullaniciId,
                    KullaniciAdi = z.Kullanici != null ? z.Kullanici.KullaniciAdi : null,
                    BirimAd = z.Kullanici != null && z.Kullanici.OgretmenProfil != null && z.Kullanici.OgretmenProfil.Birim != null
                        ? z.Kullanici.OgretmenProfil.Birim.BirimAd
                        : null,

                    ZiyaretSebebi = z.ZiyaretSebebi,

                    KartNo = z.KartNo,
                    KartVerildiMi = z.KartVerildiMi,

                    GirisZamani = z.GirisZamani,
                    CikisZamani = z.CikisZamani,

                    SureText = z.CikisZamani.HasValue
                        ? (z.CikisZamani.Value - z.GirisZamani).ToString(@"hh\:mm")
                        : null
                })
                .ToListAsync(ct);

            return list;
        }

        // ----------------------------------------------------
        // RAPOR (EXCEL)
        // ----------------------------------------------------
        public async Task<byte[]> GetRaporExcelAsync(
            string? query,
            DateTime? startDate,
            DateTime? endDate,
            CancellationToken ct = default)
        {
            var baseQuery = BuildRaporQuery(query, startDate, endDate);

            var list = await baseQuery
                .OrderByDescending(z => z.GirisZamani)
                .Select(z => new
                {
                    z.AdSoyad,
                    z.TcKimlikNo,
                    z.Telefon,
                    z.Adres,
                    KullaniciAdi = z.Kullanici != null ? z.Kullanici.KullaniciAdi : null,
                    BirimAd = z.Kullanici != null && z.Kullanici.OgretmenProfil != null && z.Kullanici.OgretmenProfil.Birim != null
                        ? z.Kullanici.OgretmenProfil.Birim.BirimAd
                        : null,
                    z.ZiyaretSebebi,
                    z.KartNo,
                    z.KartVerildiMi,
                    z.GirisZamani,
                    z.CikisZamani
                })
                .ToListAsync(ct);

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Ziyaretçi Raporu");

            var row = 1;

            // Başlık satırı
            ws.Cell(row, 1).Value = "Ziyaretçi";
            ws.Cell(row, 2).Value = "TC Kimlik No";
            ws.Cell(row, 3).Value = "Telefon";
            ws.Cell(row, 4).Value = "Adres";
            ws.Cell(row, 5).Value = "Görüştüğü Personel";
            ws.Cell(row, 6).Value = "Birim";
            ws.Cell(row, 7).Value = "Ziyaret Sebebi";
            ws.Cell(row, 8).Value = "Kart No";
            ws.Cell(row, 9).Value = "Kart Verildi";
            ws.Cell(row, 10).Value = "Giriş Tarihi / Saati";
            ws.Cell(row, 11).Value = "Çıkış Tarihi / Saati";
            ws.Cell(row, 12).Value = "Süre (hh:mm)";

            row++;

            foreach (var x in list)
            {
                ws.Cell(row, 1).Value = x.AdSoyad;
                ws.Cell(row, 2).Value = x.TcKimlikNo;
                ws.Cell(row, 3).Value = x.Telefon;
                ws.Cell(row, 4).Value = x.Adres;
                ws.Cell(row, 5).Value = x.KullaniciAdi;
                ws.Cell(row, 6).Value = x.BirimAd;
                ws.Cell(row, 7).Value = x.ZiyaretSebebi;
                ws.Cell(row, 8).Value = x.KartNo;
                ws.Cell(row, 9).Value = x.KartVerildiMi ? "Evet" : "Hayır";
                ws.Cell(row, 10).Value = x.GirisZamani;
                ws.Cell(row, 11).Value = x.CikisZamani;

                if (x.CikisZamani.HasValue)
                {
                    var sure = x.CikisZamani.Value - x.GirisZamani;
                    ws.Cell(row, 12).Value = sure.ToString(@"hh\:mm");
                }

                row++;
            }

            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }
    }
}