using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OgrenciBilgiSistemi.Data;
using OgrenciBilgiSistemi.Dtos;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Services.Interfaces;
using OgrenciBilgiSistemi.ViewModels;

namespace OgrenciBilgiSistemi.Controllers
{
    public class OgrencilerController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<OgrencilerController> _logger;
        private readonly IOgrenciService _ogrenciService;
        private readonly IYemekhaneService _yemekhaneService;
        private readonly IBirimService _birimService;
        private readonly IKullaniciService _kullaniciService;

        public OgrencilerController(
            AppDbContext context,
            ILogger<OgrencilerController> logger,
            IOgrenciService ogrenciService,
            IYemekhaneService yemekhaneService,
            IBirimService birimService,
            IKullaniciService kullaniciService)
        {
            _context = context;
            _logger = logger;
            _ogrenciService = ogrenciService;
            _yemekhaneService = yemekhaneService;
            _birimService = birimService;
            _kullaniciService = kullaniciService;
        }

        #region Index

        [HttpGet]
        public async Task<IActionResult> Index(
            string sortOrder,
            string searchString,
            int? pageNumber,
            int? birimId,
            bool includePasif = false,
            CancellationToken ct = default)
        {
            var page = await _ogrenciService.SearchPagedAsync(
                sortOrder: sortOrder,
                searchString: searchString,
                pageNumber: pageNumber.GetValueOrDefault(1),
                birimId: birimId,
                includePasif: includePasif,
                pageSize: 50,
                ct: ct);

            var ids = page.Select(o => o.OgrenciId).ToList();
            var map = await _yemekhaneService.GetBuAyDurumlariAsync(ids, ct);

            var birimler = await _birimService.GetSelectListAsync(
                selectedId: birimId,
                sinifMi: true,
                filtre: BirimFiltre.Aktif,
                ct: ct);

            var vm = new OgrenciListeVm
            {
                Page = page,
                Birimler = birimler,
                CurrentSort = sortOrder,
                CurrentFilter = searchString,
                BirimId = birimId
            };

            ViewData["YemekDurumMap"] = map;

            return View(vm);
        }

        #endregion

        #region Helper: Form ViewModel

        private async Task<OgrenciVeliFormVm> BuildFormVmAsync(
            OgrenciModel? ogrenci,
            string action,
            string submitText,
            bool includeId,
            bool? buAyYemekhaneAktif,
            CancellationToken ct = default)
        {
            var ogretmenler = await _kullaniciService.GetPersonellerSelectListAsync(ct);

            var birimler = await _birimService.GetSelectListAsync(
                selectedId: ogrenci?.BirimId,
                sinifMi: true,
                filtre: BirimFiltre.Aktif,
                ct: ct);

            var servisler = await _context.Kullanicilar
                .AsNoTracking()
                .Include(k => k.ServisProfil)
                .Where(k => k.Rol == KullaniciRolu.Servis && k.KullaniciDurum)
                .OrderBy(k => k.KullaniciAdi)
                .Select(k => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = k.KullaniciId.ToString(),
                    Text = k.ServisProfil != null ? k.ServisProfil.Plaka + " - " + k.KullaniciAdi : k.KullaniciAdi
                })
                .ToListAsync(ct);

            var veliler = await _context.Kullanicilar
                .AsNoTracking()
                .Where(k => k.Rol == KullaniciRolu.Veli && k.KullaniciDurum)
                .OrderBy(k => k.KullaniciAdi)
                .Select(k => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = k.KullaniciId.ToString(),
                    Text = k.KullaniciAdi
                })
                .ToListAsync(ct);

            return new OgrenciVeliFormVm
            {
                Ogrenci = ogrenci ?? new OgrenciModel(),
                BuAyYemekhaneAktif = buAyYemekhaneAktif ?? true,
                Ogretmenler = ogretmenler,
                Birimler = birimler,
                Servisler = servisler,
                Veliler = veliler,
                Action = action,
                SubmitText = submitText,
                IncludeId = includeId
            };
        }

        #endregion

        #region Ekle

        [HttpGet]
        public async Task<IActionResult> Ekle(CancellationToken ct = default)
        {
            ModelState.Clear();

            var vm = await BuildFormVmAsync(
                ogrenci: null,
                action: "Ekle",
                submitText: "Kaydet",
                includeId: false,
                buAyYemekhaneAktif: true,
                ct: ct);

            return View("OgrenciVeliForm", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ekle(
            OgrenciVeliFormVm model,
            CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                var vmRetry = await BuildFormVmAsync(
                    model.Ogrenci,
                    action: "Ekle",
                    submitText: "Kaydet",
                    includeId: false,
                    buAyYemekhaneAktif: model.BuAyYemekhaneAktif,
                    ct: ct);

                return View("OgrenciVeliForm", vmRetry);
            }

            try
            {
                await _ogrenciService.EkleAsync(
                    model.Ogrenci,
                    model.Ogrenci.OgrenciGorselFile,
                    model.BuAyYemekhaneAktif ?? false,
                    ct);

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Öğrenci eklenirken hata oluştu.");

                var vmRetry = await BuildFormVmAsync(
                    model.Ogrenci,
                    action: "Ekle",
                    submitText: "Kaydet",
                    includeId: false,
                    buAyYemekhaneAktif: model.BuAyYemekhaneAktif,
                    ct: ct);

                return View("OgrenciVeliForm", vmRetry);
            }
        }

        #endregion

        #region Guncelle

        [HttpGet]
        public async Task<IActionResult> Guncelle(int id, CancellationToken ct = default)
        {
            var ogrenci = await _context.Ogrenciler
                .FirstOrDefaultAsync(o => o.OgrenciId == id, ct);

            if (ogrenci == null)
                return NotFound();

            var map = await _yemekhaneService.GetBuAyDurumlariAsync(new[] { id }, ct);
            bool? buAyYemekhaneAktif = map.TryGetValue(id, out var v) ? (bool?)v : null;

            var vm = await BuildFormVmAsync(
                ogrenci,
                action: "Guncelle",
                submitText: "Güncelle",
                includeId: true,
                buAyYemekhaneAktif: buAyYemekhaneAktif,
                ct: ct);

            return View("OgrenciVeliForm", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Guncelle(
            int id,
            OgrenciVeliFormVm model,
            CancellationToken ct = default)
        {
            if (id != model.Ogrenci.OgrenciId)
                return BadRequest();

            if (!ModelState.IsValid)
            {
                var vmRetry = await BuildFormVmAsync(
                    model.Ogrenci,
                    action: "Guncelle",
                    submitText: "Güncelle",
                    includeId: true,
                    buAyYemekhaneAktif: model.BuAyYemekhaneAktif,
                    ct: ct);

                return View("OgrenciVeliForm", vmRetry);
            }

            try
            {
                await _ogrenciService.GuncelleAsync(
                    model.Ogrenci,
                    model.Ogrenci.OgrenciGorselFile,
                    model.BuAyYemekhaneAktif,
                    ct);

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Öğrenci güncellenirken hata oluştu.");

                var vmRetry = await BuildFormVmAsync(
                    model.Ogrenci,
                    action: "Guncelle",
                    submitText: "Güncelle",
                    includeId: true,
                    buAyYemekhaneAktif: model.BuAyYemekhaneAktif,
                    ct: ct);

                return View("OgrenciVeliForm", vmRetry);
            }
        }

        #endregion

        #region Sil

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sil(int id, CancellationToken ct = default)
        {
            try
            {
                await _ogrenciService.SilAsync(id, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Öğrenci silinirken hata oluştu.");
            }
            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Cihaza Gönder / Yemekhane

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TopluOgrenciGonder(int cihazId, CancellationToken ct)
        {
            var sonuc = await _ogrenciService.CihazaGonderAsync(cihazId, ct);

            TempData["Mesaj"] = sonuc
                ? "Tüm (aktif) öğrenciler başarıyla cihaza gönderildi."
                : "Bazı öğrenciler cihaza eklenemedi. Lütfen logları kontrol edin.";

            return RedirectToAction("Index", "Cihazlar");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetYemekhaneBuAy(
            int id,
            bool aktif,
            string? sortOrder,
            string? searchString,
            int? pageNumber,
            int? birimId)
        {
            await _yemekhaneService.SetBuAyAsync(id, aktif);
            return RedirectToAction(nameof(Index),
                new { sortOrder, searchString, pageNumber, birimId });
        }

        #endregion

        #region ExportToExcel

        public async Task<IActionResult> ExportToExcel(
            string sortOrder,
            string searchString,
            int? birimId,
            CancellationToken ct = default)
        {
            var ogrenciler = _context.Ogrenciler
                .AsNoTracking()
                .Include(o => o.Birim)
                .Include(o => o.Veli)
                    .ThenInclude(k => k!.VeliProfil)
                .Where(o => o.OgrenciDurum);

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var s = searchString.Trim();
                if (long.TryParse(s, out var no))
                {
                    ogrenciler = ogrenciler.Where(o =>
                        o.OgrenciNo == no ||
                        (o.OgrenciAdSoyad != null &&
                         (EF.Functions.Like(EF.Functions.Collate(o.OgrenciAdSoyad, "Turkish_100_CI_AI"), $"%{s}%")
                          || EF.Functions.Like(EF.Functions.Collate(o.OgrenciAdSoyad, "Latin1_General_CI_AI"), $"%{s}%"))));
                }
                else
                {
                    ogrenciler = ogrenciler.Where(o =>
                        o.OgrenciAdSoyad != null &&
                        (EF.Functions.Like(EF.Functions.Collate(o.OgrenciAdSoyad, "Turkish_100_CI_AI"), $"%{s}%")
                         || EF.Functions.Like(EF.Functions.Collate(o.OgrenciAdSoyad, "Latin1_General_CI_AI"), $"%{s}%")));
                }
            }

            if (birimId.HasValue)
                ogrenciler = ogrenciler.Where(o => o.BirimId == birimId.Value);

            ogrenciler = sortOrder == "No_desc"
                ? ogrenciler.OrderByDescending(o => o.OgrenciNo).ThenBy(o => o.OgrenciAdSoyad)
                : ogrenciler.OrderBy(o => o.OgrenciNo).ThenBy(o => o.OgrenciAdSoyad);

            var list = await ogrenciler.ToListAsync(ct);

            var ids = list.Select(o => o.OgrenciId).ToList();
            var yemekMap = await _yemekhaneService.GetBuAyDurumlariAsync(ids, ct);

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
            var content = stream.ToArray();

            var fileName = $"OgrenciListesi_{DateTime.Now:yyyyMMdd}.xlsx";

            return File(
                content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        #endregion

        [HttpGet]
        public async Task<IActionResult> OgrenciVeliRapor(
            string? query,
            int? birimId,
            int page = 1,
            int pageSize = 50,
            CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize <= 0) pageSize = 50;
            pageSize = Math.Min(pageSize, 200);

            // Filtreleri ViewData'ya basmak istersen:
            ViewData["CurrentFilter"] = query;
            ViewData["CurrentBirimId"] = birimId;
            ViewData["Page"] = page;
            ViewData["PageSize"] = pageSize;

            // Temel sorgu: aktif öğrenciler + sınıf + veli
            var q = _context.Ogrenciler
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

            // DTO'ya projeksiyon + SAYFALAMA
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

            var rapor = await SayfalanmisListeModel<OgrenciVeliRaporDto>.CreateAsync(dtoQuery, page, pageSize, ct);

            // Sınıf/Birim dropdown'u
            var birimler = await _birimService.GetSelectListAsync(
                selectedId: birimId,
                sinifMi: true,
                filtre: BirimFiltre.Aktif,
                ct: ct);

            var vm = new OgrenciVeliRaporVm
            {
                query = query,
                birimId = birimId,
                Birimler = birimler,
                Rapor = rapor   // <-- BURASI ÖNEMLİ: Satirlar yerine Rapor (Paged)
            };

            return View("OgrenciVeliRapor", vm);
        }

        [HttpGet]
        public async Task<IActionResult> OgrenciVeliRaporExcel(
        string? query,
        int? birimId,
        CancellationToken ct = default)
        {
            var q = _context.Ogrenciler
                .AsNoTracking()
                .Include(o => o.Birim)
                .Include(o => o.Veli)
                    .ThenInclude(k => k!.VeliProfil)
                .Where(o => o.OgrenciDurum);

            if (birimId.HasValue)
                q = q.Where(o => o.BirimId == birimId.Value);

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
            var content = stream.ToArray();

            var fileName = $"OgrenciVeliRaporu_{DateTime.Now:yyyyMMdd}.xlsx";

            return File(
                content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

    }
}
