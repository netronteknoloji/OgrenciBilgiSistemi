using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using OgrenciBilgiSistemi.Helpers;
using OgrenciBilgiSistemi.Services.Interfaces;
using OgrenciBilgiSistemi.Shared.Constants;
using OgrenciBilgiSistemi.ViewModels;

namespace OgrenciBilgiSistemi.Controllers
{
    // Raporlar > Servis Şoför Raporu: şoför listesi, şoför-öğrenci listesi, servis yoklaması
    public class ServisRaporController : Controller
    {
        private readonly IServisRaporService _servisRapor;

        public ServisRaporController(IServisRaporService servisRapor)
        {
            _servisRapor = servisRapor;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? sekme,
            int? servisId,
            string? searchString,
            DateTime? startDate,
            DateTime? endDate,
            int page = 1,
            CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            sekme = ServisRaporSekme.Normalize(sekme);

            var vm = new ServisRaporVm
            {
                Sekme = sekme,
                ServisId = servisId,
                AramaMetni = searchString,
                StartDate = startDate?.ToString("yyyy-MM-dd"),
                EndDate = endDate?.ToString("yyyy-MM-dd"),
                Servisler = await _servisRapor.ServisleriGetirAsync(ct),
                Soforler = sekme == ServisRaporSekme.Soforler
                    ? await _servisRapor.SoforListesiAsync(searchString, ct)
                    : new(),
                Ogrenciler = sekme == ServisRaporSekme.Ogrenciler
                    ? await _servisRapor.SoforOgrenciListesiAsync(servisId, searchString, ct)
                    : new(),
                Yoklamalar = sekme == ServisRaporSekme.Yoklama
                    ? await _servisRapor.ServisYoklamaListeleAsync(servisId, searchString, startDate, endDate, page, ct)
                    : null,
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Excel(
            string? sekme,
            int? servisId,
            string? searchString,
            DateTime? startDate,
            DateTime? endDate,
            CancellationToken ct = default)
        {
            sekme = ServisRaporSekme.Normalize(sekme);
            using var workbook = new XLWorkbook();
            string dosyaAdi;

            if (sekme == ServisRaporSekme.Ogrenciler)
            {
                var servisler = (await _servisRapor.ServisleriGetirAsync(ct)).ToDictionary(s => s.KullaniciId);
                var liste = await _servisRapor.SoforOgrenciListesiAsync(servisId, searchString, ct);

                var ws = workbook.Worksheets.Add("Şoför-Öğrenci Listesi");
                string[] basliklar = ["Plaka", "Şoför", "Şoför Telefon", "Hostes", "Öğrenci Ad Soyad", "Öğrenci No", "Sınıf/Birim"];
                for (int i = 0; i < basliklar.Length; i++) ws.Cell(1, i + 1).Value = basliklar[i];
                ExcelYardimci.BasligiBicimle(ws.Range(1, 1, 1, basliklar.Length));

                int r = 2;
                foreach (var o in liste
                    .OrderBy(o => servisler.GetValueOrDefault(o.ServisId!.Value)?.Plaka)
                    .ThenBy(o => o.OgrenciAdSoyad))
                {
                    var sp = servisler.GetValueOrDefault(o.ServisId!.Value);
                    ws.Cell(r, 1).Value = sp?.Plaka ?? "-";
                    ws.Cell(r, 2).Value = sp?.SoforAdSoyad ?? sp?.Kullanici?.KullaniciAdi ?? "-";
                    ws.Cell(r, 3).Value = sp?.Kullanici?.Telefon ?? "-";
                    ws.Cell(r, 4).Value = sp?.HostesAdSoyad ?? "-";
                    ws.Cell(r, 5).Value = o.OgrenciAdSoyad;
                    ws.Cell(r, 6).Value = o.OgrenciNo;
                    ws.Cell(r, 7).Value = o.Birim?.BirimAd ?? "-";
                    r++;
                }
                ExcelYardimci.SayfayiSonlandir(ws, basliklar.Length, r, $"Toplam {liste.Count} öğrenci");
                dosyaAdi = "ServisSoforOgrenciListesi";
            }
            else if (sekme == ServisRaporSekme.Yoklama)
            {
                var liste = await _servisRapor.TumServisYoklamaListeleAsync(servisId, searchString, startDate, endDate, ct);

                var ws = workbook.Worksheets.Add("Servis Yoklaması");
                string[] basliklar = ["Ad Soyad", "Sınıf/Birim", "Tarih", "Periyot", "Durum", "Plaka", "Şoför"];
                for (int i = 0; i < basliklar.Length; i++) ws.Cell(1, i + 1).Value = basliklar[i];
                ExcelYardimci.BasligiBicimle(ws.Range(1, 1, 1, basliklar.Length));

                int r = 2;
                foreach (var sv in liste)
                {
                    ws.Cell(r, 1).Value = sv.Ogrenci?.OgrenciAdSoyad ?? "-";
                    ws.Cell(r, 2).Value = sv.Ogrenci?.Birim?.BirimAd ?? "-";
                    var c = ws.Cell(r, 3);
                    c.Value = sv.OlusturulmaTarihi;
                    c.Style.DateFormat.Format = "dd.MM.yyyy HH:mm";
                    ws.Cell(r, 4).Value = sv.Periyot == 1 ? "Sabah" : (sv.Periyot == 2 ? "Akşam" : sv.Periyot.ToString());
                    ws.Cell(r, 5).Value = YoklamaMetinleri.ServisMetinGetir(sv.DurumId);
                    ws.Cell(r, 6).Value = sv.Kullanici?.ServisProfil?.Plaka ?? "-";
                    ws.Cell(r, 7).Value = sv.Kullanici?.ServisProfil?.SoforAdSoyad ?? sv.Kullanici?.KullaniciAdi ?? "-";
                    r++;
                }
                ExcelYardimci.SayfayiSonlandir(ws, basliklar.Length, r, $"Toplam {liste.Count} kayıt");
                dosyaAdi = "ServisYoklamasi";
            }
            else
            {
                var liste = await _servisRapor.SoforListesiAsync(searchString, ct);

                var ws = workbook.Worksheets.Add("Şoför Listesi");
                string[] basliklar = ["Plaka", "Servis Kullanıcı", "Şoför", "Şoför Telefon", "Hostes", "Hostes Telefon", "Öğrenci Sayısı", "Evrak", "Eksik Evraklar", "Durum"];
                for (int i = 0; i < basliklar.Length; i++) ws.Cell(1, i + 1).Value = basliklar[i];
                ExcelYardimci.BasligiBicimle(ws.Range(1, 1, 1, basliklar.Length));

                int r = 2;
                foreach (var s in liste)
                {
                    ws.Cell(r, 1).Value = s.Plaka;
                    ws.Cell(r, 2).Value = s.Kullanici?.KullaniciAdi ?? "-";
                    ws.Cell(r, 3).Value = s.SoforAdSoyad ?? "-";
                    ws.Cell(r, 4).Value = s.Kullanici?.Telefon ?? "-";
                    ws.Cell(r, 5).Value = s.HostesAdSoyad ?? "-";
                    ws.Cell(r, 6).Value = s.HostesTelefon ?? "-";
                    ws.Cell(r, 7).Value = s.OgrenciSayisi;
                    ws.Cell(r, 8).Value = s.EvrakTamMi ? "Tam" : "Eksik";
                    ws.Cell(r, 9).Value = s.EvrakTamMi ? "" : (s.EksikEvraklar ?? "");
                    ws.Cell(r, 10).Value = s.IsDeleted ? "Pasif" : "Aktif";
                    r++;
                }
                ExcelYardimci.SayfayiSonlandir(ws, basliklar.Length, r, $"Toplam {liste.Count} servis");
                dosyaAdi = "ServisSoforListesi";
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"{dosyaAdi}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
        }
    }
}
