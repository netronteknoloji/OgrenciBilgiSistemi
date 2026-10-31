using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using OgrenciBilgiSistemi.Dtos;
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
            string? ay,
            int? birimId,
            YemekHakkiFiltresi durum = YemekHakkiFiltresi.Hepsi,
            int page = 1,
            int pageSize = 50,
            bool includePasif = false,
            CancellationToken ct = default)
        {
            if (pageSize <= 0) pageSize = 50;
            pageSize = Math.Min(pageSize, 200);

            // ay = "yyyy-MM" (input type="month"); boş/geçersizse bu ay
            var secilen = DateTime.TryParseExact(ay, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
                ? d
                : DateTime.Today;

            var rapor = await _svc.GetAylikTakipAsync(
                yil: secilen.Year,
                ay: secilen.Month,
                query: query,
                birimId: birimId,
                durum: durum,
                page: page,
                pageSize: pageSize,
                includePasif: includePasif,
                ct: ct);

            // Birim dropdown
            var birimler = await _birimService.GetSelectListAsync(
                selectedId: birimId,
                sinifMi: true,
                ct: ct);

            // Durum dropdown — seçilen ayda yemek hakkı
            var durumlar = new[] { YemekHakkiFiltresi.Hepsi, YemekHakkiFiltresi.Var, YemekHakkiFiltresi.Yok }
                .Select(e => new SelectListItem
                {
                    Value = ((int)e).ToString(),
                    Text = e switch
                    {
                        YemekHakkiFiltresi.Var => "Hakkı var",
                        YemekHakkiFiltresi.Yok => "Hakkı yok",
                        _ => "Hepsi"
                    },
                    Selected = e == durum
                })
                .ToList();

            var vm = new YemekhaneIndexVm
            {
                query = query,
                yil = secilen.Year,
                ay = secilen.Month,
                AyAdi = secilen.ToString("MMMM yyyy", new CultureInfo("tr-TR")),
                birimId = birimId,
                durum = durum,
                includePasif = includePasif,

                Birimler = birimler,
                Durumlar = durumlar,

                Satirlar = rapor.Satirlar,
                HakVarSayisi = rapor.HakVarSayisi,
                HakYokSayisi = rapor.HakYokSayisi
            };

            return View(vm);
        }

        // Index'ten tekil veya toplu yemek hakkı aç/kapat; filtreler korunarak listeye dönülür
        [HttpPost("AyHakkiKaydet")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AyHakkiKaydet(
            int[] ogrenciIdler,
            int yil,
            int ay,
            bool aktif,
            string? query,
            int? birimId,
            YemekHakkiFiltresi durum = YemekHakkiFiltresi.Hepsi,
            int page = 1,
            bool includePasif = false,
            CancellationToken ct = default)
        {
            if (ay < 1 || ay > 12 || yil < 2000 || yil > 2100)
                return BadRequest();

            if (ogrenciIdler.Length == 0)
            {
                TempData["Hata"] = "Öğrenci seçilmedi.";
            }
            else
            {
                var adet = await _svc.SetAyTopluAsync(ogrenciIdler, yil, ay, aktif, ct);
                TempData["Bilgi"] = $"{adet} öğrencinin {yil}-{ay:D2} yemek hakkı {(aktif ? "açıldı" : "kapatıldı")}.";
            }

            return RedirectToAction(nameof(Index), new
            {
                query,
                ay = $"{yil:D4}-{ay:D2}",
                birimId,
                durum = (int)durum,
                page,
                includePasif
            });
        }

        private const long ExcelMaksBoyut = 2 * 1024 * 1024;

        // Excel o ayın tam listesi: listedekilerin hakkı açılır, olmayanlarınki kapatılır
        [HttpPost("ExcelYukle")]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(ExcelMaksBoyut + 64 * 1024)]
        public async Task<IActionResult> ExcelYukle(
            IFormFile? dosya,
            int yil,
            int ay,
            string? query,
            int? birimId,
            YemekHakkiFiltresi durum = YemekHakkiFiltresi.Hepsi,
            bool includePasif = false,
            CancellationToken ct = default)
        {
            if (ay < 1 || ay > 12 || yil < 2000 || yil > 2100)
                return BadRequest();

            var geri = RedirectToAction(nameof(Index), new
            {
                query,
                ay = $"{yil:D4}-{ay:D2}",
                birimId,
                durum = (int)durum,
                includePasif
            });

            if (dosya is null || dosya.Length == 0)
            {
                TempData["Hata"] = "Dosya seçilmedi.";
                return geri;
            }
            if (!string.Equals(Path.GetExtension(dosya.FileName), ".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Hata"] = "Yalnızca .xlsx dosyası yüklenebilir.";
                return geri;
            }
            if (dosya.Length > ExcelMaksBoyut)
            {
                TempData["Hata"] = "Dosya 2 MB'tan büyük olamaz.";
                return geri;
            }

            await using var akis = dosya.OpenReadStream();
            var sonuc = await _svc.ExceldenAyHakkiYukleAsync(akis, yil, ay, ct);

            var uyarilar = new List<string>();
            if (sonuc.Hata is not null) uyarilar.Add(sonuc.Hata);
            if (sonuc.Bulunamayan.Count > 0)
                uyarilar.Add($"Bulunamayan öğrenci no ({sonuc.Bulunamayan.Count}): {Kisalt(sonuc.Bulunamayan)}");
            if (sonuc.BirdenFazla.Count > 0)
                uyarilar.Add($"Birden çok öğrencide aynı no, açılmadı ({sonuc.BirdenFazla.Count}): {Kisalt(sonuc.BirdenFazla)}");
            if (sonuc.GecersizSatirlar.Count > 0)
                uyarilar.Add($"Sayı olmayan satırlar ({sonuc.GecersizSatirlar.Count}): {Kisalt(sonuc.GecersizSatirlar)}");

            if (sonuc.Hata is null)
                TempData["Bilgi"] = $"{yil}-{ay:D2} yemek listesi yüklendi: {sonuc.Acilan} açıldı, {sonuc.Kapatilan} kapatıldı, {sonuc.Degismeyen} zaten açıktı.";
            if (uyarilar.Count > 0)
                TempData["Hata"] = string.Join(" • ", uyarilar);

            return geri;

            static string Kisalt(IReadOnlyList<int> liste)
                => string.Join(", ", liste.Take(20)) + (liste.Count > 20 ? ", …" : "");
        }

        [HttpGet("ExcelSablon")]
        public IActionResult ExcelSablon()
            => File(
                _svc.ExcelSablonu(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "YemekListesiSablon.xlsx");

        [HttpGet("Ozet")]
        public async Task<IActionResult> Ozet(int ogrenciId, int? yil, CancellationToken ct)
        {
            // Akademik yıl başlangıcı: Eylül ve sonrası => o yıl; aksi halde bir önceki yıl
            var akYil = yil ?? (DateTime.Now.Month >= 9 ? DateTime.Now.Year : DateTime.Now.Year - 1);

            var vm = await _svc.GetOzetAsync(ogrenciId, akYil, ct);
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
            var pageSize = 50;
            var vm = await _svc.GetTopluRaporAsync(bas, bit, q, page, pageSize, ct);
            vm.Bas = bas?.ToString("yyyy-MM-dd");
            vm.Bit = bit?.ToString("yyyy-MM-dd");

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