using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using OgrenciBilgiSistemi.Helpers;
using OgrenciBilgiSistemi.Services.Interfaces;
using OgrenciBilgiSistemi.ViewModels;

namespace OgrenciBilgiSistemi.Controllers
{
    [AutoValidateAntiforgeryToken]
    public class CihazlarController : Controller
    {
        private readonly ILogger<CihazlarController> _logger;
        private readonly ICihazService _cihazService;
        private readonly ICihazSenkronService _cihazSenkron;

        public CihazlarController(ILogger<CihazlarController> logger, ICihazService cihazService,
            ICihazSenkronService cihazSenkron)
        {
            _logger = logger;
            _cihazService = cihazService;
            _cihazSenkron = cihazSenkron;
        }

        public async Task<IActionResult> Index(string searchString, int page = 1, CancellationToken ct = default)
        {
            var paged = await _cihazService.SearchPagedAsync(searchString, page, 10, ct);
            var senkron = await _cihazSenkron.OzetlerAsync(paged.Select(c => c.CihazId).ToList(), ct);
            return View(new CihazIndexVm { Cihazlar = paged, CurrentFilter = searchString, SenkronOzetleri = senkron });
        }

        // Cihazı tüm öğrencilerle eşitler: eksikler yazılır, olmaması gerekenler silinir (toptan silme yok)
        [HttpPost]
        public async Task<IActionResult> Esitle(int cihazId, CancellationToken ct = default)
        {
            var eklenen = await _cihazSenkron.CihaziEsitleAsync(cihazId, ct);
            TempData["Info"] = $"Eşitleme başlatıldı ({eklenen} öğrenci sıraya alındı). İlerlemeyi bu sayfadaki senkron durumundan izleyebilirsiniz.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> HatalariYenidenDene(int cihazId, CancellationToken ct = default)
        {
            var donen = await _cihazSenkron.HatalariYenidenDeneAsync(cihazId, ct);
            TempData["Info"] = $"{donen} başarısız işlem yeniden denenecek.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> ExportToExcel(string? searchString, CancellationToken ct = default)
        {
            // Cihaz sayısı küçüktür; filtreye uyanların tamamı tek sayfada alınır
            var liste = await _cihazService.SearchPagedAsync(searchString, 1, 10_000, ct);

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Cihazlar");
            string[] basliklar = ["Cihaz Adı", "Tip", "İstasyon", "Cihaz Kodu", "IP", "Port", "Durum"];
            for (int i = 0; i < basliklar.Length; i++) ws.Cell(1, i + 1).Value = basliklar[i];
            ExcelYardimci.BasligiBicimle(ws.Range(1, 1, 1, basliklar.Length));

            int r = 2;
            foreach (var c in liste)
            {
                ws.Cell(r, 1).Value = c.CihazAdi;
                ws.Cell(r, 2).Value = c.DonanimTipi.ToString();
                ws.Cell(r, 3).Value = c.IstasyonTipi.ToString();
                ws.Cell(r, 4).Value = c.CihazKodu.ToString();
                ws.Cell(r, 5).Value = string.IsNullOrWhiteSpace(c.IpAdresi) ? "-" : c.IpAdresi;
                ws.Cell(r, 6).Value = c.PortNo is > 0 ? c.PortNo.Value.ToString() : "-";
                ws.Cell(r, 7).Value = c.IsDeleted ? "Pasif" : "Aktif";
                r++;
            }
            ExcelYardimci.SayfayiSonlandir(ws, basliklar.Length, r, $"Toplam {liste.Count} cihaz");

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Cihazlar_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
        }

        [HttpGet]
        public IActionResult Ekle() => View(new CihazFormVm());

        [HttpPost]
        public async Task<IActionResult> Ekle(CihazFormVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid) return View(vm);

            var ok = await _cihazService.CihazEkleAsync(vm.ToModel(), ct);
            if (!ok)
            {
                TempData["Error"] = "Cihaz kaydedilemedi. Bilgileri kontrol edin.";
                return View(vm);
            }

            TempData["Success"] = "Cihaz başarıyla kaydedildi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Guncelle(int id, CancellationToken ct = default)
        {
            var cihaz = await _cihazService.CihazGetByIdAsync(id, ct);
            if (cihaz is null) return NotFound();
            return View(CihazFormVm.FromModel(cihaz));
        }

        [HttpPost]
        public async Task<IActionResult> Guncelle(CihazFormVm vm, CancellationToken ct = default)
        {
            vm.FormAction = "Guncelle"; vm.SubmitText = "Güncelle"; vm.IncludeId = true; vm.ShowGuid = true;
            if (!ModelState.IsValid)
            {
                TempData["Warning"] = "Lütfen formu kontrol edin.";
                return View(vm);
            }

            var ok = await _cihazService.CihazGuncelleAsync(vm.ToModel(), ct);
            if (!ok)
            {
                TempData["Error"] = "Cihaz güncellenemedi. Bilgileri kontrol edin.";
                return View(vm);
            }

            TempData["Success"] = "Cihaz başarıyla güncellendi.";
            return RedirectToAction(nameof(Index));
        }

        // Onay Index'teki modalda alınır; ayrı onay sayfası yok
        [HttpPost]
        public async Task<IActionResult> Sil(int id, CancellationToken ct = default)
        {
            var ok = await _cihazService.CihazSilAsync(id, ct);

            TempData[ok ? "Success" : "Error"] = ok
                ? "Cihaz başarıyla silindi."
                : "Silme işlemi sırasında beklenmeyen bir hata oluştu.";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> TumKullanicilariListele(int cihazId, CancellationToken ct = default)
        {
            try
            {
                var users = await _cihazService.CihazdanKullanicilariListeleAsync(cihazId, ct);
                return View(new CihazKullanicilariVm { Kullanicilar = users, CihazId = cihazId });
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı listesi alınırken hata (cihazId={Id})", cihazId);
                TempData["Error"] = "Kullanıcı listesi alınamadı.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        public async Task<IActionResult> TumKullanicilariSil(int cihazId, CancellationToken ct = default)
        {
            try
            {
                var ok = await _cihazService.CihazdakiTumKullanicilariSilAsync(cihazId, ct);
                TempData[ok ? "Message" : "Error"] = ok
                    ? "Cihazdaki tüm kullanıcılar başarıyla silindi."
                    : "Kullanıcılar silinemedi. Cihaz bağlantısını/firmware'i kontrol edin.";
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tüm kullanıcılar silinirken hata (cihazId={Id})", cihazId);
                TempData["Error"] = "Silme işlemi sırasında beklenmeyen bir hata oluştu.";
            }

            return RedirectToAction(nameof(TumKullanicilariListele), new { cihazId });
        }
    }
}
