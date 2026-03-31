using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Services.Interfaces;
using OgrenciBilgiSistemi.ViewModels;

namespace OgrenciBilgiSistemi.Controllers
{
    public class ZiyaretcilerController : Controller
    {
        private readonly IZiyaretciService _ziyaretciService;
        private readonly IKullaniciService _kullaniciService;
        private readonly IBirimService _birimService;
        private readonly ILogger<ZiyaretcilerController> _logger;

        public ZiyaretcilerController(
            IZiyaretciService ziyaretciService,
            IKullaniciService kullaniciService,
            IBirimService birimService,
            ILogger<ZiyaretcilerController> logger)
        {
            _ziyaretciService = ziyaretciService;
            _kullaniciService = kullaniciService;
            _birimService = birimService;
            _logger = logger;
        }

        // Kullanıcı dropdown için ortak metot
        private async Task<List<SelectListItem>> GetKullaniciSelectListAsync(
            int? selectedId,
            CancellationToken ct)
        {
            var list = await _kullaniciService.GetPersonellerSelectListAsync(ct);

            if (selectedId.HasValue)
            {
                var selected = list.FirstOrDefault(x => x.Value == selectedId.Value.ToString());
                if (selected != null)
                    selected.Selected = true;
            }

            return list;
        }

        // -----------------------------
        // LISTE / INDEX
        // -----------------------------
        public async Task<IActionResult> Index(
            string? searchString,
            int? kullaniciId,
            bool sadeceAktif = true,
            int page = 1,
            int pageSize = 50,
            CancellationToken ct = default)
        {
            if (pageSize <= 0) pageSize = 50;
            pageSize = Math.Min(pageSize, 200);

            var model = await _ziyaretciService.SearchPagedAsync(
                searchString,
                page,
                pageSize,
                sadeceAktif,
                kullaniciId,
                ct);

            ViewData["CurrentFilter"] = searchString;
            ViewData["SadeceAktif"] = sadeceAktif;
            ViewData["KullaniciId"] = kullaniciId;

            ViewBag.Kullanicilar = await GetKullaniciSelectListAsync(kullaniciId, ct);

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Ekle(CancellationToken ct)
        {
            var vm = new ZiyaretciFormViewModel
            {
                KartVerildiMi = false,
                Kullanicilar = await GetKullaniciSelectListAsync(null, ct)
            };

            ViewData["Action"] = "Ekle";
            ViewData["SubmitText"] = "Kaydet";
            ViewData["IncludeId"] = false;

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ekle(ZiyaretciFormViewModel vm, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                vm.Kullanicilar = await GetKullaniciSelectListAsync(vm.KullaniciId, ct);
                ViewData["Action"] = "Ekle";
                ViewData["SubmitText"] = "Kaydet";
                return View(vm);
            }

            var model = new ZiyaretciModel
            {
                AdSoyad = vm.AdSoyad,
                TcKimlikNo = vm.TcKimlikNo,
                Telefon = vm.Telefon,
                Adres = vm.Adres,
                KullaniciId = vm.KullaniciId,
                ZiyaretSebebi = vm.ZiyaretSebebi,
                KartNo = vm.KartNo,
                KartVerildiMi = vm.KartVerildiMi,
                CihazId = vm.CihazId
            };

            await _ziyaretciService.EkleAsync(model, ct);
            TempData["Success"] = "Ziyaretçi kaydı başarıyla oluşturuldu.";

            return RedirectToAction(nameof(Index));
        }


        [HttpGet]
        public async Task<IActionResult> Guncelle(int id, CancellationToken ct)
        {
            var z = await _ziyaretciService.GetByIdAsync(id, ct);
            if (z == null)
                return NotFound();

            var vm = new ZiyaretciFormViewModel
            {
                ZiyaretciId = z.ZiyaretciId,
                AdSoyad = z.AdSoyad,
                TcKimlikNo = z.TcKimlikNo,
                Telefon = z.Telefon,
                Adres = z.Adres,
                KullaniciId = z.KullaniciId,
                ZiyaretSebebi = z.ZiyaretSebebi,
                KartNo = z.KartNo,
                KartVerildiMi = z.KartVerildiMi,
                GirisZamani = z.GirisZamani,
                CikisZamani = z.CikisZamani,
                CihazId = z.CihazId,
                Kullanicilar = await GetKullaniciSelectListAsync(z.KullaniciId, ct)
            };

            ViewData["Action"] = "Guncelle";
            ViewData["SubmitText"] = "Güncelle";
            ViewData["IncludeId"] = true;

            return View("Ekle", vm);
        }

 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Guncelle(ZiyaretciFormViewModel vm, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                vm.Kullanicilar = await GetKullaniciSelectListAsync(vm.KullaniciId, ct);
                ViewData["Action"] = "Guncelle";
                ViewData["SubmitText"] = "Güncelle";
                ViewData["IncludeId"] = true;

                return View("Ekle", vm);
            }

            if (vm.ZiyaretciId == null)
                return BadRequest();

            var z = await _ziyaretciService.GetByIdAsync(vm.ZiyaretciId.Value, ct);
            if (z == null)
                return NotFound();

            z.AdSoyad = vm.AdSoyad;
            z.TcKimlikNo = vm.TcKimlikNo;
            z.Telefon = vm.Telefon;
            z.Adres = vm.Adres;
            z.KullaniciId = vm.KullaniciId;
            z.ZiyaretSebebi = vm.ZiyaretSebebi;
            z.KartNo = vm.KartNo;
            z.KartVerildiMi = vm.KartVerildiMi;
            z.GirisZamani = vm.GirisZamani ?? z.GirisZamani;
            z.CikisZamani = vm.CikisZamani;
            z.CihazId = vm.CihazId;

            await _ziyaretciService.GuncelleAsync(z, ct);

            TempData["Success"] = "Ziyaretçi kaydı güncellendi.";
            return RedirectToAction(nameof(Index));
        }



        // -----------------------------
        // ÇIKIŞ YAPTIR
        // -----------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CikisYap(int id, CancellationToken ct)
        {
            await _ziyaretciService.CikisYapAsync(id, ct);
            TempData["Success"] = "Ziyaretçinin çıkışı işlendi.";
            return RedirectToAction(nameof(Index));
        }

        // -----------------------------
        // SİL
        // -----------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sil(int id, CancellationToken ct)
        {
            await _ziyaretciService.SilAsync(id, ct);
            TempData["Success"] = "Ziyaretçi kaydı silindi.";
            return RedirectToAction(nameof(Index));
        }

        // -----------------------------
        // KART OKUT (RFID / CİHAZ)
        // -----------------------------
        [HttpGet]
        public async Task<IActionResult> KartOkut(string kartNo, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(kartNo))
                return BadRequest("Kart numarası boş olamaz.");

            var model = await _ziyaretciService.KartOkutAsync(kartNo, ct);
            return Json(model); // ZiyaretciKartOkumaViewModel JSON
        }

        // -----------------------------
        // DETAY
        // -----------------------------
        [HttpGet]
        public async Task<IActionResult> Detay(int id, CancellationToken ct)
        {
            var z = await _ziyaretciService.GetByIdAsync(id, ct);
            if (z == null)
                return NotFound();

            var tumZiyaretler = await _ziyaretciService.GetZiyaretGecmisiAsync(
                z.TcKimlikNo,
                z.AdSoyad,
                ct);

            var vm = new ZiyaretciDetayViewModel
            {
                Ziyaretci = z,
                Ziyaretler = tumZiyaretler
            };

            return View(vm);
        }

        // -----------------------------
        // RAPOR
        // -----------------------------
        [HttpGet]
        public async Task<IActionResult> ZiyaretciRapor(
            string? query,
            DateTime? startDate,
            DateTime? endDate,
            CancellationToken ct = default)
        {
            var raporList = await _ziyaretciService.GetRaporAsync(
                query,
                startDate,
                endDate,
                ct);

            var vm = new ZiyaretciRaporVm
            {
                query = query,
                StartDate = startDate,
                EndDate = endDate,
                Rapor = raporList,
            };

            return View(vm);
        }

        // -----------------------------
        // RAPOR EXCEL
        // -----------------------------
        [HttpGet]
        public async Task<IActionResult> RaporExcel(
            string? query,
            DateTime? startDate,
            DateTime? endDate,
            CancellationToken ct = default)
        {
            var excelBytes = await _ziyaretciService.GetRaporExcelAsync(
                query,
                startDate,
                endDate,
                ct);

            return File(
                excelBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"ZiyaretciRaporu_{DateTime.Now:yyyyMMddHHmm}.xlsx");
        }
    }
}