using Microsoft.AspNetCore.Mvc;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Services.Interfaces;
using OgrenciBilgiSistemi.ViewModels;

namespace OgrenciBilgiSistemi.Controllers
{
    public class OgrencilerController : Controller
    {
        private readonly ILogger<OgrencilerController> _logger;
        private readonly IOgrenciService _ogrenciService;
        private readonly IYemekhaneService _yemekhaneService;
        private readonly IBirimService _birimService;
        private readonly IKullaniciService _kullaniciService;

        public OgrencilerController(
            ILogger<OgrencilerController> logger,
            IOgrenciService ogrenciService,
            IYemekhaneService yemekhaneService,
            IBirimService birimService,
            IKullaniciService kullaniciService)
        {
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
            OgrenciFiltre durum = OgrenciFiltre.Aktif,
            CancellationToken ct = default)
        {
            var page = await _ogrenciService.SearchPagedAsync(
                sortOrder: sortOrder,
                searchString: searchString,
                pageNumber: pageNumber.GetValueOrDefault(1),
                birimId: birimId,
                filtre: durum,
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
                BirimId = birimId,
                Durum = durum,
                YemekDurumMap = map
            };

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
            var birimler = await _birimService.GetSelectListAsync(
                selectedId: ogrenci?.BirimId,
                sinifMi: true,
                filtre: BirimFiltre.Aktif,
                ct: ct);

            var servisler = await _kullaniciService.GetServislerPlakaliSelectListAsync(ct);

            var veliler = await _kullaniciService.GetKullanicilarByRolSelectListAsync(KullaniciRolu.Veli, ct);

            return new OgrenciVeliFormVm
            {
                Ogrenci = ogrenci ?? new OgrenciModel(),
                BuAyYemekhaneAktif = buAyYemekhaneAktif ?? true,
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
            var ogrenci = await _ogrenciService.GetByIdAsync(id, includeVeli: false, ct);

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
            var file = await _ogrenciService.ExportOgrenciListesiExcelAsync(
                sortOrder, searchString, birimId, ct);

            return File(file.Content, file.ContentType, file.FileName);
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

            var rapor = await _ogrenciService.GetVeliRaporAsync(query, birimId, page, pageSize, ct);

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
            var file = await _ogrenciService.ExportVeliRaporExcelAsync(query, birimId, ct);

            return File(file.Content, file.ContentType, file.FileName);
        }

    }
}
