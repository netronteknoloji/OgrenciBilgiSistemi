using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Services.Interfaces;
using OgrenciBilgiSistemi.Shared.Constants;
using OgrenciBilgiSistemi.ViewModels;

namespace OgrenciBilgiSistemi.Controllers
{
    public class OgrenciGirisCikisController : Controller
    {
        private readonly IGecisRaporService _gecisRapor;
        private readonly ILogger<OgrenciGirisCikisController> _logger;

        public OgrenciGirisCikisController(IGecisRaporService gecisRapor, ILogger<OgrenciGirisCikisController> logger)
        {
            _gecisRapor = gecisRapor;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Detay(
            string? sortOrder,
            string? searchString,
            int page = 1,
            RaporTipi raporTipi = RaporTipi.Tumu,
            DateTime? startDate = null,
            DateTime? endDate = null,
            CancellationToken ct = default)
        {
            if (page < 1) page = 1;

            IstasyonTipi? istasyonTipi = raporTipi switch
            {
                RaporTipi.AnaKapiGecisleri => IstasyonTipi.AnaKapi,
                RaporTipi.YemekhaneGecisleri => IstasyonTipi.Yemekhane,
                _ => null
            };

            var vm = new OgrenciDetayRaporVm
            {
                RaporTipi = raporTipi,
                CurrentSort = sortOrder,
                CurrentFilter = searchString,
                StartDate = startDate?.ToString("yyyy-MM-dd"),
                EndDate = endDate?.ToString("yyyy-MM-dd"),
                RaporTipiStr = ((int)raporTipi).ToString(),
            };

            if (raporTipi == RaporTipi.SinifYoklamasi)
            {
                vm.SinifYoklamalar = await _gecisRapor.SinifYoklamaListeleAsync(searchString, startDate, endDate, page, ct);
                return View(vm);
            }

            if (raporTipi == RaporTipi.ServisYoklamasi)
            {
                vm.ServisYoklamalar = await _gecisRapor.ServisYoklamaListeleAsync(searchString, startDate, endDate, page, ct);
                return View(vm);
            }

            vm.Gecisler = await _gecisRapor.GecisListeleAsync(sortOrder, searchString, startDate, endDate, istasyonTipi, page, ct);
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> GirisCikisDetay(
            int id,
            DateTime? startDate,
            DateTime? endDate,
            RaporTipi raporTipi = RaporTipi.Tumu,
            int? pageNumber = null,
            CancellationToken ct = default)
        {
            IstasyonTipi? istasyonTipi = raporTipi switch
            {
                RaporTipi.AnaKapiGecisleri => IstasyonTipi.AnaKapi,
                RaporTipi.YemekhaneGecisleri => IstasyonTipi.Yemekhane,
                _ => null
            };

            bool gecisGoster = raporTipi == RaporTipi.Tumu
                            || raporTipi == RaporTipi.AnaKapiGecisleri
                            || raporTipi == RaporTipi.YemekhaneGecisleri;

            var ogrenci = await _gecisRapor.OgrenciBulAsync(id, ct);
            if (ogrenci is null) return NotFound();

            var pageIndex = pageNumber.GetValueOrDefault(1);

            var hareketlerPaged = gecisGoster
                ? await _gecisRapor.OgrenciGecisListeleAsync(id, startDate, endDate, istasyonTipi, pageIndex, ct)
                : SayfalanmisListeModel<OgrenciGirisCikisVm>.FromList(new List<OgrenciGirisCikisVm>(), 1, 25);

            var sinifYoklamalar = (raporTipi == RaporTipi.Tumu || raporTipi == RaporTipi.SinifYoklamasi)
                ? await _gecisRapor.OgrenciSinifYoklamaListeleAsync(id, startDate, endDate, ct)
                : new List<SinifYoklamaModel>();

            var servisYoklamalar = (raporTipi == RaporTipi.Tumu || raporTipi == RaporTipi.ServisYoklamasi)
                ? await _gecisRapor.OgrenciServisYoklamaListeleAsync(id, startDate, endDate, ct)
                : new List<ServisYoklamaModel>();

            var vm = new OgrenciGirisCikisListViewModel
            {
                Ogrenci = ogrenci,
                Hareketler = hareketlerPaged,
                SinifYoklamalar = sinifYoklamalar,
                ServisYoklamalar = servisYoklamalar,
                RaporTipi = raporTipi,
                StartDate = startDate?.ToString("yyyy-MM-dd"),
                EndDate = endDate?.ToString("yyyy-MM-dd"),
                RaporTipiStr = ((int)raporTipi).ToString(),
            };

            return View("GirisCikisDetay", vm);
        }

        [HttpGet]
        public async Task<IActionResult> DetayExportToExcel(
            int id,
            string? searchName,
            DateTime? startDate,
            DateTime? endDate,
            RaporTipi raporTipi = RaporTipi.Tumu,
            CancellationToken ct = default)
        {
            var ogrenci = await _gecisRapor.OgrenciBulAsync(id, ct);
            if (ogrenci is null) return NotFound();

            IstasyonTipi? istasyonTipi = raporTipi switch
            {
                RaporTipi.AnaKapiGecisleri => IstasyonTipi.AnaKapi,
                RaporTipi.YemekhaneGecisleri => IstasyonTipi.Yemekhane,
                _ => null
            };

            bool gecisGoster = raporTipi == RaporTipi.Tumu || raporTipi == RaporTipi.AnaKapiGecisleri || raporTipi == RaporTipi.YemekhaneGecisleri;
            bool sinifGoster = raporTipi == RaporTipi.Tumu || raporTipi == RaporTipi.SinifYoklamasi;
            bool servisGoster = raporTipi == RaporTipi.Tumu || raporTipi == RaporTipi.ServisYoklamasi;

            var gecisler = gecisGoster
                ? await _gecisRapor.OgrenciGecislerGetirAsync(id, startDate, endDate, istasyonTipi, ct)
                : new List<OgrenciDetayModel>();

            var sinifList = sinifGoster
                ? await _gecisRapor.OgrenciSinifYoklamaListeleAsync(id, startDate, endDate, ct)
                : new List<SinifYoklamaModel>();

            var servisList = servisGoster
                ? await _gecisRapor.OgrenciServisYoklamaListeleAsync(id, startDate, endDate, ct)
                : new List<ServisYoklamaModel>();

            using var workbook = new XLWorkbook();

            if (gecisGoster)
            {
                var ws1 = workbook.Worksheets.Add("Geçişler");
                ws1.Cell(1, 1).Value = "#";
                ws1.Cell(1, 2).Value = "Giriş Tarihi";
                ws1.Cell(1, 3).Value = "Çıkış Tarihi";
                ws1.Cell(1, 4).Value = "Geçiş Tipi";
                ws1.Cell(1, 5).Value = "Cihaz Adı";
                BasligiBicimle(ws1.Range(1, 1, 1, 5));

                int r = 2;
                foreach (var g in gecisler)
                {
                    ws1.Cell(r, 1).Value = g.OgrenciDetayId;
                    var c2 = ws1.Cell(r, 2);
                    if (g.OgrenciGTarih.HasValue) { c2.Value = g.OgrenciGTarih.Value; c2.Style.DateFormat.Format = "dd.MM.yyyy HH:mm"; }
                    else c2.Value = "-";
                    var c3 = ws1.Cell(r, 3);
                    if (g.OgrenciCTarih.HasValue) { c3.Value = g.OgrenciCTarih.Value; c3.Style.DateFormat.Format = "dd.MM.yyyy HH:mm"; }
                    else c3.Value = "-";
                    ws1.Cell(r, 4).Value = YoklamaMetinleri.GecisMetinGetir(g.OgrenciGecisTipi);
                    ws1.Cell(r, 5).Value = g.Cihaz?.CihazAdi ?? "-";
                    r++;
                }
                SayfayiSonlandir(ws1, 5, r, $"Toplam {gecisler.Count} kayıt");
            }

            if (sinifGoster)
            {
                var ws2 = workbook.Worksheets.Add("Sınıf Yoklaması");
                ws2.Cell(1, 1).Value = "Tarih";
                for (int i = 1; i <= 8; i++) ws2.Cell(1, 1 + i).Value = $"Ders {i}";
                ws2.Cell(1, 10).Value = "Kaydeden";
                BasligiBicimle(ws2.Range(1, 1, 1, 10));

                int r = 2;
                foreach (var sy in sinifList)
                {
                    var ct2 = ws2.Cell(r, 1);
                    ct2.Value = sy.OlusturulmaTarihi;
                    ct2.Style.DateFormat.Format = "dd.MM.yyyy HH:mm";
                    ws2.Cell(r, 2).Value = YoklamaMetinleri.MetinGetir(sy.Ders1);
                    ws2.Cell(r, 3).Value = YoklamaMetinleri.MetinGetir(sy.Ders2);
                    ws2.Cell(r, 4).Value = YoklamaMetinleri.MetinGetir(sy.Ders3);
                    ws2.Cell(r, 5).Value = YoklamaMetinleri.MetinGetir(sy.Ders4);
                    ws2.Cell(r, 6).Value = YoklamaMetinleri.MetinGetir(sy.Ders5);
                    ws2.Cell(r, 7).Value = YoklamaMetinleri.MetinGetir(sy.Ders6);
                    ws2.Cell(r, 8).Value = YoklamaMetinleri.MetinGetir(sy.Ders7);
                    ws2.Cell(r, 9).Value = YoklamaMetinleri.MetinGetir(sy.Ders8);
                    ws2.Cell(r, 10).Value = sy.Kullanici?.KullaniciAdi ?? "-";
                    r++;
                }
                SayfayiSonlandir(ws2, 10, r, $"Toplam {sinifList.Count} kayıt");
            }

            if (servisGoster)
            {
                var ws3 = workbook.Worksheets.Add("Servis Yoklaması");
                ws3.Cell(1, 1).Value = "Tarih";
                ws3.Cell(1, 2).Value = "Periyot";
                ws3.Cell(1, 3).Value = "Durum";
                ws3.Cell(1, 4).Value = "Şoför";
                BasligiBicimle(ws3.Range(1, 1, 1, 4));

                int r = 2;
                foreach (var sv in servisList)
                {
                    var ct3 = ws3.Cell(r, 1);
                    ct3.Value = sv.OlusturulmaTarihi;
                    ct3.Style.DateFormat.Format = "dd.MM.yyyy HH:mm";
                    ws3.Cell(r, 2).Value = sv.Periyot == 1 ? "Sabah" : (sv.Periyot == 2 ? "Akşam" : sv.Periyot.ToString());
                    ws3.Cell(r, 3).Value = YoklamaMetinleri.ServisMetinGetir(sv.DurumId);
                    ws3.Cell(r, 4).Value = sv.Kullanici?.KullaniciAdi ?? "-";
                    r++;
                }
                SayfayiSonlandir(ws3, 4, r, $"Toplam {servisList.Count} kayıt");
            }

            if (workbook.Worksheets.Count == 0)
                workbook.Worksheets.Add("Bos");

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var raporAdi = raporTipi switch
            {
                RaporTipi.AnaKapiGecisleri => "AnaKapiGecisleri",
                RaporTipi.YemekhaneGecisleri => "YemekhaneGecisleri",
                RaporTipi.SinifYoklamasi => "SinifYoklamasi",
                RaporTipi.ServisYoklamasi => "ServisYoklamasi",
                _ => "OgrenciDetay"
            };
            var fileName = $"{raporAdi}_{ogrenci.OgrenciAdSoyad}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        [HttpGet]
        public async Task<IActionResult> DetayExcel(
            string? searchString,
            DateTime? startDate,
            DateTime? endDate,
            RaporTipi raporTipi = RaporTipi.Tumu,
            CancellationToken ct = default)
        {
            using var workbook = new XLWorkbook();

            if (raporTipi == RaporTipi.SinifYoklamasi)
            {
                var liste = await _gecisRapor.TumSinifYoklamaListeleAsync(searchString, startDate, endDate, ct);

                var ws2 = workbook.Worksheets.Add("Sınıf Yoklaması");
                ws2.Cell(1, 1).Value = "Ad Soyad";
                ws2.Cell(1, 2).Value = "Sınıf/Birim";
                ws2.Cell(1, 3).Value = "Tarih";
                for (int i = 1; i <= 8; i++) ws2.Cell(1, 3 + i).Value = $"Ders {i}";
                ws2.Cell(1, 12).Value = "Kaydeden";
                BasligiBicimle(ws2.Range(1, 1, 1, 12));

                int r = 2;
                foreach (var sy in liste)
                {
                    ws2.Cell(r, 1).Value = sy.Ogrenci?.OgrenciAdSoyad ?? "-";
                    ws2.Cell(r, 2).Value = sy.Ogrenci?.Birim?.BirimAd ?? "-";
                    var ct2 = ws2.Cell(r, 3);
                    ct2.Value = sy.OlusturulmaTarihi;
                    ct2.Style.DateFormat.Format = "dd.MM.yyyy HH:mm";
                    ws2.Cell(r, 4).Value = YoklamaMetinleri.MetinGetir(sy.Ders1);
                    ws2.Cell(r, 5).Value = YoklamaMetinleri.MetinGetir(sy.Ders2);
                    ws2.Cell(r, 6).Value = YoklamaMetinleri.MetinGetir(sy.Ders3);
                    ws2.Cell(r, 7).Value = YoklamaMetinleri.MetinGetir(sy.Ders4);
                    ws2.Cell(r, 8).Value = YoklamaMetinleri.MetinGetir(sy.Ders5);
                    ws2.Cell(r, 9).Value = YoklamaMetinleri.MetinGetir(sy.Ders6);
                    ws2.Cell(r, 10).Value = YoklamaMetinleri.MetinGetir(sy.Ders7);
                    ws2.Cell(r, 11).Value = YoklamaMetinleri.MetinGetir(sy.Ders8);
                    ws2.Cell(r, 12).Value = sy.Kullanici?.KullaniciAdi ?? "-";
                    r++;
                }
                SayfayiSonlandir(ws2, 12, r, $"Toplam {liste.Count} kayıt");

                using var ms = new MemoryStream();
                workbook.SaveAs(ms);
                return File(ms.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"SinifYoklamasi_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
            }

            if (raporTipi == RaporTipi.ServisYoklamasi)
            {
                var liste = await _gecisRapor.TumServisYoklamaListeleAsync(searchString, startDate, endDate, ct);

                var ws3 = workbook.Worksheets.Add("Servis Yoklaması");
                ws3.Cell(1, 1).Value = "Ad Soyad";
                ws3.Cell(1, 2).Value = "Sınıf/Birim";
                ws3.Cell(1, 3).Value = "Tarih";
                ws3.Cell(1, 4).Value = "Periyot";
                ws3.Cell(1, 5).Value = "Durum";
                ws3.Cell(1, 6).Value = "Şoför";
                BasligiBicimle(ws3.Range(1, 1, 1, 6));

                int r = 2;
                foreach (var sv in liste)
                {
                    ws3.Cell(r, 1).Value = sv.Ogrenci?.OgrenciAdSoyad ?? "-";
                    ws3.Cell(r, 2).Value = sv.Ogrenci?.Birim?.BirimAd ?? "-";
                    var ct3 = ws3.Cell(r, 3);
                    ct3.Value = sv.OlusturulmaTarihi;
                    ct3.Style.DateFormat.Format = "dd.MM.yyyy HH:mm";
                    ws3.Cell(r, 4).Value = sv.Periyot == 1 ? "Sabah" : (sv.Periyot == 2 ? "Akşam" : sv.Periyot.ToString());
                    ws3.Cell(r, 5).Value = YoklamaMetinleri.ServisMetinGetir(sv.DurumId);
                    ws3.Cell(r, 6).Value = sv.Kullanici?.KullaniciAdi ?? "-";
                    r++;
                }
                SayfayiSonlandir(ws3, 6, r, $"Toplam {liste.Count} kayıt");

                using var ms = new MemoryStream();
                workbook.SaveAs(ms);
                return File(ms.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"ServisYoklamasi_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
            }

            // Geçişler (Tumu / AnaKapı / Yemekhane)
            IstasyonTipi? istasyonTipi = raporTipi switch
            {
                RaporTipi.AnaKapiGecisleri => IstasyonTipi.AnaKapi,
                RaporTipi.YemekhaneGecisleri => IstasyonTipi.Yemekhane,
                _ => null
            };

            var filteredLogs = await _gecisRapor.TumGecislerGetirAsync(searchString, startDate, endDate, istasyonTipi, ct);

            var ws = workbook.Worksheets.Add("Öğrenci Giriş-Çıkış");
            ws.Cell(1, 1).Value = "#";
            ws.Cell(1, 2).Value = "Ad Soyad";
            ws.Cell(1, 3).Value = "Sınıf/Birim";
            ws.Cell(1, 4).Value = "Kart No";
            ws.Cell(1, 5).Value = "Giriş Tarihi";
            ws.Cell(1, 6).Value = "Çıkış Tarihi";
            ws.Cell(1, 7).Value = "Geçiş Tipi";
            ws.Cell(1, 8).Value = "Cihaz Adı";

            var headerRange = ws.Range(1, 1, 1, 8);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            int row = 2;
            foreach (var log in filteredLogs)
            {
                ws.Cell(row, 1).Value = log.OgrenciDetayId;
                ws.Cell(row, 2).Value = log.Ogrenci?.OgrenciAdSoyad ?? "-";
                ws.Cell(row, 3).Value = log.Ogrenci?.Birim?.BirimAd ?? "-";
                ws.Cell(row, 4).Value = log.Ogrenci?.OgrenciKartNo ?? "-";

                var cGiris = ws.Cell(row, 5);
                if (log.OgrenciGTarih.HasValue) { cGiris.Value = log.OgrenciGTarih.Value; cGiris.Style.DateFormat.Format = "dd.MM.yyyy HH:mm"; }
                else cGiris.Value = "-";

                var cCikis = ws.Cell(row, 6);
                if (log.OgrenciCTarih.HasValue) { cCikis.Value = log.OgrenciCTarih.Value; cCikis.Style.DateFormat.Format = "dd.MM.yyyy HH:mm"; }
                else cCikis.Value = "-";

                ws.Cell(row, 7).Value = YoklamaMetinleri.GecisMetinGetir(log.OgrenciGecisTipi);
                ws.Cell(row, 8).Value = log.Cihaz?.CihazAdi ?? "-";
                row++;
            }

            ws.SheetView.FreezeRows(1);
            ws.Range(1, 1, Math.Max(1, row - 1), 8).SetAutoFilter();

            int summaryRow = row + 1;
            ws.Range(summaryRow, 1, summaryRow, 7).Merge();
            var totalCell = ws.Cell(summaryRow, 8);
            totalCell.Value = $"Toplam {filteredLogs.Count} kayıt";
            totalCell.Style.Font.Bold = true;
            totalCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            totalCell.Style.Border.TopBorder = XLBorderStyleValues.Thin;

            ws.Columns(1, 8).AdjustToContents(1, summaryRow);
            foreach (var col in ws.Columns(1, 8))
                if (col.Width < 15) col.Width = 15;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "OgrenciGirisCikis.xlsx");
        }

        private static void BasligiBicimle(IXLRange headerRange)
        {
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        }

        private static void SayfayiSonlandir(IXLWorksheet ws, int colCount, int nextRow, string ozet)
        {
            ws.SheetView.FreezeRows(1);
            ws.Range(1, 1, Math.Max(1, nextRow - 1), colCount).SetAutoFilter();

            int summaryRow = nextRow + 1;
            if (colCount > 1)
                ws.Range(summaryRow, 1, summaryRow, colCount - 1).Merge();
            var totalCell = ws.Cell(summaryRow, colCount);
            totalCell.Value = ozet;
            totalCell.Style.Font.Bold = true;
            totalCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            totalCell.Style.Border.TopBorder = XLBorderStyleValues.Thin;

            ws.Columns(1, colCount).AdjustToContents(1, summaryRow);
            foreach (var col in ws.Columns(1, colCount))
                if (col.Width < 12) col.Width = 12;
        }
    }
}
