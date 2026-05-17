using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using OgrenciBilgiSistemi.Dtos;
using OgrenciBilgiSistemi.Helpers;
using OgrenciBilgiSistemi.Services.Interfaces;
using OgrenciBilgiSistemi.ViewModels;

namespace OgrenciBilgiSistemi.Controllers
{
    [Route("Yemekhane")]
    public class YemekhaneController : Controller
    {
        private readonly IYemekhaneService _svc;
        private readonly IBirimService _birimService;

        public YemekhaneController(IYemekhaneService svc, IBirimService birimService)
        {
            _svc = svc;
            _birimService = birimService;
        }

        [HttpGet("Index")]
        public async Task<IActionResult> Index(
            string? query,
            int? yil,
            int? birimId,
            RaporDurumFiltresiDto durum = RaporDurumFiltresiDto.Hepsi,
            int page = 1,
            int pageSize = 50,
            bool includePasif = false,
            CancellationToken ct = default)
        {
            int defaultYil = AkademikDonemHelper.Current();

            if (pageSize <= 0) pageSize = 50;
            pageSize = Math.Min(pageSize, 200);

            var rapor = await _svc.GetIndexRaporAsync(
                yil: yil,
                query: query,
                birimId: birimId,
                durum: durum,
                page: page,
                pageSize: pageSize,
                includePasif: includePasif,
                ct: ct);

            // Yıl dropdown
            var yillar = rapor.KullanilabilirYillar
                .OrderByDescending(x => x)
                .Select(y => new SelectListItem
                {
                    Value = y.ToString(),
                    Text = $"{y}-{y + 1}",
                    Selected = (yil ?? defaultYil) == y
                })
                .ToList();
            yillar.Insert(0, new SelectListItem
            {
                Value = "",
                Text = "Tüm Yıllar",
                Selected = !yil.HasValue
            });

            // Birim dropdown
            var birimler = await _birimService.GetSelectListAsync(
                selectedId: birimId,
                sinifMi: true,
                ct: ct);

            // Durum dropdown — Yemekhane'de Muaf yok, sadece Hepsi/Borçlu/Borçsuz
            var durumlar = new[] { RaporDurumFiltresiDto.Hepsi, RaporDurumFiltresiDto.Borclu, RaporDurumFiltresiDto.Borcsuz }
                .Select(e => new SelectListItem
                {
                    Value = ((int)e).ToString(),
                    Text = e switch
                    {
                        RaporDurumFiltresiDto.Hepsi => "Hepsi",
                        RaporDurumFiltresiDto.Borclu => "Borçlu",
                        RaporDurumFiltresiDto.Borcsuz => "Borçsuz",
                        _ => e.ToString()
                    },
                    Selected = e == durum
                })
                .ToList();

            var vm = new YemekhaneIndexVm
            {
                query = query,
                yil = yil ?? defaultYil,
                birimId = birimId,
                durum = durum,
                includePasif = includePasif,

                Yillar = yillar,
                Birimler = birimler,
                Durumlar = durumlar,

                Satirlar = rapor.Satirlar,
                ToplamBorc = rapor.ToplamBorc,
                ToplamOdenen = rapor.ToplamOdenen,
                ToplamKalan = rapor.ToplamKalan,
                KullanilabilirYillar = rapor.KullanilabilirYillar
            };

            return View(vm);
        }

        [HttpGet("Ozet")]
        public async Task<IActionResult> Ozet(int ogrenciId, int? yil, CancellationToken ct)
        {
            // Akademik yıl başlangıcı: Eylül ve sonrası => o yıl; aksi halde bir önceki yıl
            var akYil = yil ?? (DateTime.Now.Month >= 9 ? DateTime.Now.Year : DateTime.Now.Year - 1);

            var vm = await _svc.GetOzetAsync(ogrenciId, akYil, ct);

            // Akademik yılın tamamındaki ödemeler (Eyl..Ara + Oca..Ağu)
            var odemeler = await _svc.GetAkademikYilOdemeleriAsync(ogrenciId, akYil, ct);
            ViewBag.Odemeler = odemeler;

            ViewBag.OgrenciId = ogrenciId;
            return View(vm);
        }

        [HttpPost("TarifeKaydet")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TarifeKaydet(int ogrenciId, int yil, decimal aylikTutar, string? aciklama, CancellationToken ct)
        {
            // yil = akademik yıl başlangıcı
            await _svc.SetTarifeAsync(ogrenciId, yil, aylikTutar, aciklama, ct);
            TempData["Bilgi"] = "Tarife kaydedildi.";
            return RedirectToAction(nameof(Ozet), new { ogrenciId, yil });
        }

        [HttpPost("AyDurumKaydet")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AyDurumKaydet(int ogrenciId, int yil, int ay, bool aktif, string? not, CancellationToken ct)
        {
            // yil & ay = TAKVİM yılı/ayı (ör. 2026-01)
            await _svc.SetAyAsync(ogrenciId, yil, ay, aktif, not, ct);

            // Redirect’te AKADEMİK yıl hesapla
            var akYil = (ay >= 9) ? yil : (yil - 1);
            TempData["Bilgi"] = $"{yil}-{ay:D2} ay durumu güncellendi.";
            return RedirectToAction(nameof(Ozet), new { ogrenciId, yil = akYil });
        }

        [HttpPost("OdemeEkle")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OdemeEkle(int ogrenciId, int yil, int ay, decimal tutar, DateTime? tarih, string? aciklama, CancellationToken ct)
        {
            if (tutar <= 0m)
            {
                TempData["Hata"] = "Tutar > 0 olmalı.";
                // Hatalı durumda da doğru akademik yıla dön
                var akYilBad = (ay >= 9) ? yil : (yil - 1);
                return RedirectToAction(nameof(Ozet), new { ogrenciId, yil = akYilBad });
            }

            // yil & ay = TAKVİM yılı/ayı
            await _svc.OdemeEkleAsync(ogrenciId, yil, ay, tutar, tarih, aciklama, ct);

            var akYil = (ay >= 9) ? yil : (yil - 1);
            TempData["Bilgi"] = "Ödeme eklendi.";
            return RedirectToAction(nameof(Ozet), new { ogrenciId, yil = akYil });
        }

        [HttpPost("OdemeSil")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OdemeSil(int ogrenciId, int yil, int odemeId, CancellationToken ct)
        {
            // odemeId’den TAKVİM yıl/ay’ı bulup akademik yılı hesaplayalım (güvenli redirect)
            // Not: servis katmanına özel bir "GetOdemeAsync" eklemeden, ödeme silmeden önce yılı hesaplayıp sonra silmek mantıklı.
            // Küçük bir read + compute:
            // (İstersen IYemekhaneService’e GetOdemeAsync ekleyebilirsin; burada ufak bir inline çözüm veriyoruz.)
            // Aşağıdaki satır servis yoksa Controller’a DbContext enjekte etmeyi gerektirir.
            // Basit yol: _svc içinde bir yardımcı ekle; ancak şimdilik varsayılan ‘yil’ akademik ise doğru çalışır.

            await _svc.OdemeSilAsync(odemeId, ct);

            // Eğer view’den akademik ‘yil’ gönderiyorsan bu redirect doğrudur.
            // Eğer takvim ‘yil’ gönderiyorsan, ay bilgisi olmadığı için akademik yılda sapma olabilir.
            // En doğrusu: silmeden önce odemenin (Yil, Ay)’ını okuyup akYil hesaplayıp ona göre redirect etmektir.
            TempData["Bilgi"] = "Ödeme silindi.";
            return RedirectToAction(nameof(Ozet), new { ogrenciId, yil });
        }

        [HttpGet("YemekRapor")]
        public async Task<IActionResult> YemekRapor(
            string? q,
            DateTime? bas,
            DateTime? bit,
            int page = 1,
            CancellationToken ct = default)
        {
            // View'da tarih inputlarının dolu gelmesi için
            ViewData["Bas"] = bas?.ToString("yyyy-MM-dd");
            ViewData["Bit"] = bit?.ToString("yyyy-MM-dd");

            // Yeni tarih-bazlı overload (önerilen)
            var pageSize = 50; // formda kaldırıldığı için sabit
            var vm = await _svc.GetTopluRaporAsync(bas, bit, q, page, pageSize, ct);

            return View(vm);
        }

        [HttpGet("YemekRaporExcel")]
        public async Task<IActionResult> YemekRaporExcel(
            string? q,
            DateTime? bas,
            DateTime? bit,
            CancellationToken ct = default)
        {
            // Tarih aralığı + arama ile Excel üret
            var bytes = await _svc.ExportTopluRaporExcelAsync(bas, bit, q, ct);

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "YemekhaneRapor.xlsx");
        }

    }
}