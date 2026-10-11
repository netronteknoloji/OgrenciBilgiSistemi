using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using OgrenciBilgiSistemi.Dtos;
using OgrenciBilgiSistemi.Helpers;
using OgrenciBilgiSistemi.Infrastructure;
using OgrenciBilgiSistemi.Services.Interfaces;
using OgrenciBilgiSistemi.ViewModels;

namespace OgrenciBilgiSistemi.Controllers
{
    [Route("[controller]")]
    public class AidatController : Controller
    {
        private readonly IAidatService _aidatService;
        private readonly IBirimService _birimService;
        private readonly ILogger<AidatController> _logger;

        public AidatController(
            IAidatService aidatService,
            IBirimService birimService,
            ILogger<AidatController> logger)
        {
            _aidatService = aidatService;
            _birimService = birimService;
            _logger = logger;
        }

        [MenuIslemYetki("Aidat", "Index")]

        [HttpGet("Index")]
        public async Task<IActionResult> Index(
    string? query,
    int? yil,
    int? birimId,
    RaporDurumFiltresiDto durum = RaporDurumFiltresiDto.Hepsi,
    DateTime? bas = null,
    DateTime? bit = null,
    int page = 1,
    int pageSize = 50,
    bool includePasif = false,
    CancellationToken ct = default)
        {
            // Akademik yıl varsayılanı
            int defaultYil = AkademikDonemHelper.Current();
            int year = yil ?? defaultYil;

            if (pageSize <= 0) pageSize = 50;
            pageSize = Math.Min(pageSize, 200);

            // Raporu çek
            var rapor = await _aidatService.GetAidatRaporAsync(
                yil: yil,
                bas: bas,
                bit: bit,
                query: query,
                birimId: birimId,
                durum: durum,
                tarifeYil: null,
                page: page,
                pageSize: pageSize,
                includePasif: includePasif,
                ct: ct);

            // Yıl dropdown
            var yillar = YilSecenekleri(year, rapor.KullanilabilirYillar);

            // Birim dropdown
            var birimler = await _birimService.GetSelectListAsync(
                selectedId: birimId,
                sinifMi: true,
                ct: ct);

            // Durum dropdown
            var durumlar = Enum.GetValues<RaporDurumFiltresiDto>()
                .Select(e => new SelectListItem
                {
                    Value = ((int)e).ToString(),
                    Text = e switch
                    {
                        RaporDurumFiltresiDto.Hepsi => "Hepsi",
                        RaporDurumFiltresiDto.Borclu => "Borçlu",
                        RaporDurumFiltresiDto.Borcsuz => "Borçsuz",
                        RaporDurumFiltresiDto.Muaf => "Muaf",
                        _ => e.ToString()
                    },
                    Selected = e == durum
                })
                .ToList();

            // ViewModel
            var vm = new AidatRaporVm
            {
                query = query,
                yil = yil ?? AkademikDonemHelper.Current(),
                birimId = birimId,
                durum = durum,
                bas = bas,
                bit = bit,
                includePasif = includePasif,

                Yillar = yillar,
                Birimler = birimler,
                Durumlar = durumlar,

                Satirlar = rapor.Satirlar,
                ToplamBorc = rapor.ToplamBorc,
                ToplamOdenenGosterilen = rapor.ToplamOdenenGosterilen,
                ToplamKalan = rapor.ToplamKalan,
                KullanilabilirYillar = rapor.KullanilabilirYillar,
                Tarife = rapor.Tarife,
                OdemesiOlanSayisi = rapor.OdemesiOlanSayisi
            };

            return View(vm);
        }

        // -------------------------------------------------------------
        // Özet
        // -------------------------------------------------------------
        [HttpGet("Ozet/{ogrenciId:int}")]
        public async Task<IActionResult> Ozet(int ogrenciId, int? yil = null, string? returnUrl = null, CancellationToken ct = default)
        {
            int defaultYil = AkademikDonemHelper.Current();
            int year = yil ?? defaultYil;

            var dto = await _aidatService.GetOgrenciAidatAsync(ogrenciId, year, ct);
            if (dto is null)
            {
                TempData["Hata"] = "Aidat özeti bulunamadı.";
                return RedirectToAction("Index", "Ogrenciler");
            }

            var yillar = await _aidatService.GetKullanilabilirYillarAsync(ogrenciId, ct);
            if (yillar is null || yillar.Count == 0 || !yillar.Contains(year))
            {
                yillar ??= new List<int>();
                if (!yillar.Contains(year)) yillar.Add(year);
                yillar = yillar.OrderBy(y => y).ToList();
            }

            dto.KullanilabilirYillar = yillar;
            dto.ReturnUrl = returnUrl;

            return View("Ozet", dto);
        }

        // -------------------------------------------------------------
        // Rapor (Liste)
        // -------------------------------------------------------------
        [HttpGet("AidatRapor")]
        public async Task<IActionResult> AidatRapor(
            string? query,
            int? yil,
            int? birimId,
            RaporDurumFiltresiDto durum = RaporDurumFiltresiDto.Hepsi,
            DateTime? bas = null,
            DateTime? bit = null,
            int page = 1,
            int pageSize = 50,
            bool includePasif = false,
            CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize <= 0) pageSize = 50;
            pageSize = Math.Min(pageSize, 200);

            var rapor = await _aidatService.GetAidatRaporAsync(
                yil,
                bas,
                bit,
                query,
                birimId,
                durum,
                tarifeYil: null,
                page: page,
                pageSize: pageSize,
                includePasif: includePasif,
                ct: ct);

            // Dropdown – Yıllar
            var yillar = YilSecenekleri(yil ?? AkademikDonemHelper.Current(), rapor.KullanilabilirYillar);

            // Dropdown – Birimler (sınıflar)
            var birimler = await _birimService.GetSelectListAsync(
                selectedId: birimId,
                sinifMi: true,
                ct: ct);

            // Dropdown – Durumlar
            var durumlar = Enum.GetValues<RaporDurumFiltresiDto>()
                .Select(e => new SelectListItem
                {
                    Value = ((int)e).ToString(),
                    Text = e switch
                    {
                        RaporDurumFiltresiDto.Hepsi => "Hepsi",
                        RaporDurumFiltresiDto.Borclu => "Borçlu",
                        RaporDurumFiltresiDto.Borcsuz => "Borçsuz",
                        RaporDurumFiltresiDto.Muaf => "Muaf",
                        _ => e.ToString()
                    },
                    Selected = e == durum
                })
                .ToList();

            var vm = new AidatRaporVm
            {
                query = query,
                yil = yil ?? AkademikDonemHelper.Current(),
                birimId = birimId,
                durum = durum,
                bas = bas,
                bit = bit,
                includePasif = includePasif,

                Yillar = yillar,
                Birimler = birimler,
                Durumlar = durumlar,

                // 🔹 Servis sonucunu VM’e map ediyoruz
                Satirlar = rapor.Satirlar,
                ToplamBorc = rapor.ToplamBorc,
                ToplamOdenenGosterilen = rapor.ToplamOdenenGosterilen,
                ToplamKalan = rapor.ToplamKalan,
                KullanilabilirYillar = rapor.KullanilabilirYillar
            };

            return View(vm);
        }

        // -------------------------------------------------------------
        // Excel (XLSX) Export
        // -------------------------------------------------------------
        [HttpGet("AidatRaporExcel")]
        public async Task<IActionResult> AidatRaporExcel(
            string? query,
            int? yil,
            int? birimId,
            RaporDurumFiltresiDto durum = RaporDurumFiltresiDto.Hepsi,
            DateTime? bas = null,
            DateTime? bit = null,
            bool includePasif = false,
            CancellationToken ct = default)
        {
            var file = await _aidatService.ExportAidatRaporExcelAsync(
                yil, bas, bit, query, birimId, durum,
                tarifeYil: null,
                includePasif: includePasif,
                ct: ct);

            return File(file.Content, file.ContentType, file.FileName);
        }

        // -------------------------------------------------------------
        // Ödeme işlemleri
        // -------------------------------------------------------------
        [MenuIslemYetki("Aidat", "Index")]
        [HttpPost("OdemeEkle")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OdemeEkle(AidatOdemeEkleDto dto, string? returnUrl, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Eksik veya hatalı bilgi var. Lütfen kontrol edin.";
                return SafeRedirect(returnUrl, nameof(Index), new { yil = dto.Yil });
            }

            try
            {
                await _aidatService.OdemeEkleAsync(dto, ct);

                TempData["Success"] = $"Ödeme kaydedildi. Tutar: {dto.Tutar:N2} • Tarih: {dto.Tarih:dd.MM.yyyy HH:mm}";

                return SafeRedirect(returnUrl, nameof(Index), new { yil = dto.Yil });
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return SafeRedirect(returnUrl, nameof(Index), new { yil = dto.Yil });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ödeme ekleme sırasında hata oluştu. {@Dto}", dto);
                TempData["Error"] = "Ödeme eklenemedi. Lütfen tekrar deneyin.";
                return SafeRedirect(returnUrl, nameof(Index), new { yil = dto.Yil });
            }
        }

        [MenuIslemYetki("Aidat", "Index")]

        [HttpPost("OdemeSil")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OdemeSil(int ogrenciId, int yil, int odemeId, string? returnUrl, CancellationToken ct = default)
        {
            var ok = await _aidatService.OdemeSilAsync(odemeId, ct);
            TempData[ok ? "Bilgi" : "Hata"] = ok ? "Ödeme silindi." : "Ödeme bulunamadı.";

            return SafeRedirect(returnUrl, nameof(Ozet), new { ogrenciId, yil });
        }

        // -------------------------------------------------------------
        // Tarife işlemleri
        // -------------------------------------------------------------
        [MenuIslemYetki("Aidat", "Index")]
        [HttpPost("TarifeKaydet")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TarifeKaydet(TarifeDto dto, string? returnUrl, CancellationToken ct)
        {
            if (!ModelState.IsValid || dto.Yil < 2000 || dto.Yil > 2100 || dto.Tutar < 0m)
            {
                TempData["Error"] = "Tarife bilgileri hatalı. Lütfen kontrol edin.";
                return SafeRedirect(returnUrl, nameof(Index), new { yil = dto.Yil });
            }

            try
            {
                await _aidatService.TarifeKaydetAsync(dto, ct);
                TempData["Success"] = $"{dto.Yil}-{dto.Yil + 1} tarifesi kaydedildi: {dto.Tutar:N2} ₺";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tarife kaydedilirken hata oluştu. Yil={Yil}", dto.Yil);
                TempData["Error"] = "Tarife kaydedilemedi. Lütfen tekrar deneyin.";
            }

            return SafeRedirect(returnUrl, nameof(Index), new { yil = dto.Yil });
        }

        // -------------------------------------------------------------
        // Öğrenciye özel borç (indirim/burs)
        // -------------------------------------------------------------
        [MenuIslemYetki("Aidat", "Index")]
        [HttpPost("BorcAyarla")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BorcAyarla(int ogrenciId, int yil, decimal? borc, bool tarifeyeDon, string? returnUrl, CancellationToken ct = default)
        {
            if (!tarifeyeDon && (borc is null || borc < 0m))
            {
                TempData["Error"] = "Geçerli bir borç tutarı girin.";
            }
            else
            {
                var ok = await _aidatService.OgrenciBorcAyarlaAsync(ogrenciId, yil, tarifeyeDon ? null : borc, ct);
                TempData[ok ? "Success" : "Error"] = !ok
                    ? "Borç güncellenemedi."
                    : tarifeyeDon
                        ? "Öğrenci yılın tarifesine döndürüldü."
                        : $"Öğrenciye özel borç kaydedildi: {borc:N2} ₺";
            }

            return RedirectToAction(nameof(Ozet), new { ogrenciId, yil, returnUrl });
        }

        // -------------------------------------------------------------
        // Muafiyet işlemleri (+ alias)
        // -------------------------------------------------------------
        [MenuIslemYetki("Aidat", "Index")]
        [HttpPost("SetMuafiyet")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetMuafiyet(int ogrenciId, int yil, bool muaf, CancellationToken ct)
        {
            var ok = await _aidatService.SetYillikMuafiyetAsync(ogrenciId, yil, muaf, ct);
            return Json(new { success = ok });
        }

        [HttpGet("GetMuafiyet")]
        public async Task<IActionResult> GetMuafiyet(int ogrenciId, int yil, CancellationToken ct)
        {
            var muaf = await _aidatService.GetYillikMuafiyetAsync(ogrenciId, yil, ct);
            return Json(new { muaf });
        }

        // /OgrenciAidat/MuafiyetAyarla (alias’lar)
        [MenuIslemYetki("Aidat", "Index")]
        [HttpPost("MuafiyetAyarla")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MuafiyetAyarla(int ogrenciId, int yil, bool muaf, string? returnUrl, CancellationToken ct = default)
        {
            var ok = await _aidatService.SetYillikMuafiyetAsync(ogrenciId, yil, muaf, ct);

            TempData[ok ? "Success" : "Error"] = ok
                ? (muaf
                    ? "Öğrenci ilgili yıl için MUAF yapıldı."
                    : "Öğrencinin yıllık muafiyeti KALDIRILDI.")
                : "Muafiyet işlemi başarısız.";

            // Varsa geldiğin sayfaya dön, yoksa Ozet'e
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);

            return RedirectToAction(nameof(Ozet), new { ogrenciId, yil });
        }

        // MuafiyetAyarlaGet kaldırıldı: GET isteğiyle veritabanı değişikliği
        // CSRF saldırısına açık olduğu için bu action silinmiştir.
        // Muafiyet değişikliği yalnızca [HttpPost] MuafiyetAyarla üzerinden yapılmalıdır.

        // -------------------------------------------------------------
        // Yardımcı
        // -------------------------------------------------------------
        private static List<SelectListItem> YilSecenekleri(int seciliYil, IEnumerable<int> yillar)
        {
            return yillar
                .Append(seciliYil)
                .Distinct()
                .OrderByDescending(y => y)
                .Select(y => new SelectListItem
                {
                    Value = y.ToString(),
                    Text = $"{y}-{y + 1}",
                    Selected = y == seciliYil
                })
                .ToList();
        }

        private IActionResult SafeRedirect(string? returnUrl, string fallbackAction, object? routeValues = null)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);

            return RedirectToAction(fallbackAction, routeValues);
        }
    }
}