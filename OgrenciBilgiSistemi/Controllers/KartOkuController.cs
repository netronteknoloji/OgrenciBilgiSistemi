using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OgrenciBilgiSistemi.Data;
using OgrenciBilgiSistemi.Dtos;
using OgrenciBilgiSistemi.Hubs;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Services.Interfaces;
using OgrenciBilgiSistemi.ViewModels;

namespace OgrenciBilgiSistemi.Controllers
{
    [Route("KartOku")]
    public class KartOkuController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IGecisService _gecisService;
        private readonly IHubContext<KartOkuHub> _hub;
        private readonly ILogger<KartOkuController> _logger;

        public KartOkuController(
            AppDbContext db,
            IGecisService gecisService,
            IHubContext<KartOkuHub> hub,
            ILogger<KartOkuController> logger)
        {
            _db = db;
            _gecisService = gecisService;
            _hub = hub;
            _logger = logger;
        }

        private static string NormalizeKartNo(string? kartNo)
        {
            if (string.IsNullOrWhiteSpace(kartNo)) return string.Empty;
            var s = kartNo.Trim();
            var trimmed = s.TrimStart('0');
            return trimmed.Length == 0 ? "0" : trimmed;
        }

        private static class Msg
        {
            public const string ErrOglenYok = "ÖĞRENCİNİN ÖĞLE ÇIKIŞ İZNİ YOK!";
            public const string ErrOglenLimit = "Bugün için öğle çıkış hakkı kullanıldı!";
            public const string ErrYemekYok = "ÖĞRENCİNİN YEMEKHANE GEÇİŞ İZNİ YOK!";
            public const string ErrYemekLimit = "Bugün için yemekhane geçiş hakkı kullanıldı!";
            public const string InfoOglenOk = "ÖĞLE ÇIKIŞI ONAYLANDI.";
            public const string InfoYemekOk = "YEMEKHANE GEÇİŞİ ONAYLANDI.";
            public const string InfoGenelOk = "Geçiş başarılı.";
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(string? cihazKodu, CancellationToken ct)
        {
            try
            {
                CihazModel? cihaz = null;

                if (Guid.TryParse(cihazKodu?.Trim().Trim('<', '>'), out var guid))
                {
                    cihaz = await _db.Cihazlar.AsNoTracking()
                        .FirstOrDefaultAsync(c => c.CihazKodu == guid, ct);
                }
                else
                {
                    cihaz = await _db.Cihazlar.AsNoTracking()
                        .OrderByDescending(c => c.Aktif)
                        .ThenBy(c => c.CihazId)
                        .FirstOrDefaultAsync(ct);
                }

                if (cihaz is null)
                    return View(new KartOkumaVm { HataMesaji = "Cihaz bilgisi eksik." });

                ViewBag.CihazKodu = cihaz.CihazKodu.ToString();
                return View(new KartOkumaVm());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "KartOku/Index çalıştırılırken hata.");
                return View(new KartOkumaVm { HataMesaji = "Beklenmeyen bir hata oluştu." });
            }
        }

        [HttpPost("UsbKartOkundu")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> UsbKartOkundu([FromForm] string? kartNo, [FromForm] string? cihazKodu, CancellationToken ct)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(kartNo))
                    return BadRequest("Kart numarası boş.");

                cihazKodu = cihazKodu?.Trim().Trim('<', '>');
                if (!Guid.TryParse(cihazKodu, out var guid))
                    return BadRequest("Cihaz kodu geçersiz.");

                var cihaz = await _db.Cihazlar.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.CihazKodu == guid && c.Aktif, ct);

                if (cihaz is null)
                    return NotFound("Cihaz bulunamadı veya pasif.");

                var no = NormalizeKartNo(kartNo);
                if (string.IsNullOrEmpty(no))
                    return BadRequest("Kart numarası geçersiz.");

                var ogrenci = await _db.Ogrenciler
                    .AsNoTracking()
                    .Select(o => new
                    {
                        o.OgrenciId,
                        o.OgrenciNo,
                        o.OgrenciAdSoyad,
                        o.OgrenciKartNo,
                        o.OgrenciDurum,
                        o.OgrenciCikisDurumu,
                        o.OgrenciGorsel,
                        Sinif = o.Birim != null ? o.Birim.BirimAd : "-"
                    })
                    .FirstOrDefaultAsync(o => o.OgrenciKartNo == no && o.OgrenciDurum, ct);

                if (ogrenci is null)
                    return NotFound("Kart tanımsız.");

                var now = DateTime.Now;
                var today = now.Date;
                var tomorrow = today.AddDays(1);
                string? forcedGecisTipi = null;

                // --- İzin kontrolleri (LOG ATMADAN) ---
                if (cihaz.IstasyonTipi == IstasyonTipi.AnaKapi &&
                    ogrenci.OgrenciCikisDurumu == OglenCikisDurumu.Hayir)
                {
                    await _hub.Clients.All.SendAsync("OgrenciBilgisiAl", new OgrenciBilgisiDto
                    {
                        OgrenciAdSoyad = ogrenci.OgrenciAdSoyad,
                        OgrenciNo = ogrenci.OgrenciNo,
                        OgrenciSinif = ogrenci.Sinif ?? "-",
                        OgrenciGorsel = ogrenci.OgrenciGorsel,
                        OgrenciGirisSaati = "-",
                        OgrenciCikisSaati = "-",
                        OglenCikisDurumu = ogrenci.OgrenciCikisDurumu,
                        GecisTipi = "Reddedildi",
                        Istasyon = cihaz.IstasyonTipi.ToString(),
                        CihazAdi = cihaz.CihazAdi,
                        CihazKodu = cihaz.CihazKodu,
                        Reason = "ANA_KAPI_OGLE_RED",
                        Error = Msg.ErrOglenYok
                    }, ct);

                    return Ok(new { durum = "YOK_SAYILDI", gerekce = "Öğle çıkış izni yok." });
                }

                if (cihaz.IstasyonTipi == IstasyonTipi.Yemekhane)
                {
                    var yil = now.Year;
                    var ay = now.Month;

                    var ayAktif = await _db.OgrenciYemekler.AsNoTracking()
                        .AnyAsync(x => x.OgrenciId == ogrenci.OgrenciId && x.Yil == yil && x.Ay == ay && x.Aktif, ct);

                    var odemeVarMi = await _db.OgrenciYemekOdemeler.AsNoTracking()
                        .AnyAsync(p => p.OgrenciId == ogrenci.OgrenciId && p.Yil == yil && p.Ay == ay && p.Tutar > 0m, ct);

                    if (!ayAktif && !odemeVarMi)
                    {
                        await _hub.Clients.All.SendAsync("OgrenciBilgisiAl", new OgrenciBilgisiDto
                        {
                            OgrenciAdSoyad = ogrenci.OgrenciAdSoyad,
                            OgrenciNo = ogrenci.OgrenciNo,
                            OgrenciSinif = ogrenci.Sinif ?? "-",
                            OgrenciGorsel = ogrenci.OgrenciGorsel,
                            OgrenciGirisSaati = "-",
                            OgrenciCikisSaati = "-",
                            OglenCikisDurumu = ogrenci.OgrenciCikisDurumu,
                            GecisTipi = "Reddedildi",
                            Istasyon = cihaz.IstasyonTipi.ToString(),
                            CihazAdi = cihaz.CihazAdi,
                            CihazKodu = cihaz.CihazKodu,
                            Reason = "YEMEKHANE_RED",
                            Error = Msg.ErrYemekYok
                        }, ct);

                        return Ok(new { durum = "YOK_SAYILDI", gerekce = "Yemekhane izni yok." });
                    }
                }

                // --- Günlük limit kontrolleri (LOG ATMADAN) ---
                if (cihaz.IstasyonTipi == IstasyonTipi.Yemekhane)
                {
                    var bugunYemekhaneVarMi = await _db.OgrenciDetaylar
                        .AsNoTracking()
                        .AnyAsync(l => l.OgrenciId == ogrenci.OgrenciId
                                       && l.Cihaz != null
                                       && l.Cihaz.IstasyonTipi == IstasyonTipi.Yemekhane
                                       && (
                                           (l.OgrenciGTarih >= today && l.OgrenciGTarih < tomorrow)
                                           || (l.OgrenciCTarih >= today && l.OgrenciCTarih < tomorrow)
                                       ), ct);

                    if (bugunYemekhaneVarMi)
                    {
                        await _hub.Clients.All.SendAsync("OgrenciBilgisiAl", new OgrenciBilgisiDto
                        {
                            OgrenciAdSoyad = ogrenci.OgrenciAdSoyad,
                            OgrenciNo = ogrenci.OgrenciNo,
                            OgrenciSinif = ogrenci.Sinif ?? "-",
                            OgrenciGorsel = ogrenci.OgrenciGorsel,
                            OgrenciGirisSaati = "-",
                            OgrenciCikisSaati = "-",
                            OglenCikisDurumu = ogrenci.OgrenciCikisDurumu,
                            GecisTipi = "Reddedildi",
                            Istasyon = cihaz.IstasyonTipi.ToString(),
                            CihazAdi = cihaz.CihazAdi,
                            CihazKodu = cihaz.CihazKodu,
                            Reason = "YEMEKHANE_LIMIT",
                            Error = Msg.ErrYemekLimit
                        }, ct);

                        return Ok(new { durum = "YOK_SAYILDI", gerekce = "Yemekhane günlük limit dolu." });
                    }
                }
                else if (cihaz.IstasyonTipi == IstasyonTipi.AnaKapi &&
                         ogrenci.OgrenciCikisDurumu == OglenCikisDurumu.Evet)
                {
                    // 1) Bugün ÇIKIŞ var mı?
                    var cikisVarMi = await _db.OgrenciDetaylar
                        .AsNoTracking()
                        .AnyAsync(l => l.OgrenciId == ogrenci.OgrenciId
                                       && l.Cihaz != null
                                       && l.Cihaz.IstasyonTipi == IstasyonTipi.AnaKapi
                                       && l.OgrenciCTarih >= today && l.OgrenciCTarih < tomorrow, ct);

                    // 2) Bugün GİRİŞ var mı?
                    var girisVarMi = await _db.OgrenciDetaylar
                        .AsNoTracking()
                        .AnyAsync(l => l.OgrenciId == ogrenci.OgrenciId
                                       && l.Cihaz != null
                                       && l.Cihaz.IstasyonTipi == IstasyonTipi.AnaKapi
                                       && l.OgrenciGTarih >= today && l.OgrenciGTarih < tomorrow, ct);

                    if (!cikisVarMi)
                    {
                        // İlk okutma -> forced ÇIKIŞ
                        forcedGecisTipi = "Çıkış";
                    }
                    else if (!girisVarMi)
                    {
                        // İkinci okutma (okula dönüş) -> forced GİRİŞ
                        forcedGecisTipi = "Giriş";
                    }
                    else
                    {
                        // Çıkış + Giriş zaten yapılmış -> üçüncü ve sonrası RED
                        await _hub.Clients.All.SendAsync("OgrenciBilgisiAl", new OgrenciBilgisiDto
                        {
                            OgrenciAdSoyad = ogrenci.OgrenciAdSoyad,
                            OgrenciNo = ogrenci.OgrenciNo,
                            OgrenciSinif = ogrenci.Sinif ?? "-",
                            OgrenciGorsel = ogrenci.OgrenciGorsel,
                            OgrenciGirisSaati = "-",
                            OgrenciCikisSaati = "-",
                            OglenCikisDurumu = ogrenci.OgrenciCikisDurumu,
                            GecisTipi = "Reddedildi",
                            Istasyon = cihaz.IstasyonTipi.ToString(),
                            CihazAdi = cihaz.CihazAdi,
                            CihazKodu = cihaz.CihazKodu,
                            Reason = "ANA_KAPI_OGLE_DONUS_LIMIT",
                            Error = "Bugün öğle çıkışı ve dönüşü zaten yapılmış!"
                        }, ct);

                        return Ok(new { durum = "YOK_SAYILDI", gerekce = "Öğle çıkış/dönüş tamam.", reason = "ANA_KAPI_OGLE_DONUS_LIMIT" });
                    }
                }

                // --- Kayıt: istasyona göre netleştir (isteğe bağlı "force" geçiş tipi) ---
                if (forcedGecisTipi is null)
                {
                    forcedGecisTipi = cihaz.IstasyonTipi switch
                    {
                        IstasyonTipi.Yemekhane => "Giriş", // yemekhane her zaman giriş (forced)
                        IstasyonTipi.AnaKapi when ogrenci.OgrenciCikisDurumu != OglenCikisDurumu.Evet
                            => null,                        // Ana Kapı + öğle izni yok -> toggle
                        _ => null                           // diğer durumlar -> toggle
                    };
                }

                var sonuc = await _gecisService.KaydetAsync(
                    cihaz.CihazId, ogrenci.OgrenciId, cihaz.IstasyonTipi, now, ct, forcedGecisTipi);

                var girisSaati = "-";
                var cikisSaati = "-";
                if (string.Equals(sonuc.GecisTipi, "Giriş", StringComparison.OrdinalIgnoreCase))
                    girisSaati = now.ToString("HH:mm");
                else if (string.Equals(sonuc.GecisTipi, "Çıkış", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(sonuc.GecisTipi, "Cikis", StringComparison.OrdinalIgnoreCase))
                    cikisSaati = now.ToString("HH:mm");

                var dto = new OgrenciBilgisiDto
                {
                    OgrenciAdSoyad = ogrenci.OgrenciAdSoyad,
                    OgrenciNo = ogrenci.OgrenciNo,
                    OgrenciSinif = ogrenci.Sinif ?? "-",
                    OgrenciGorsel = ogrenci.OgrenciGorsel,
                    OgrenciGirisSaati = girisSaati,
                    OgrenciCikisSaati = cikisSaati,
                    OglenCikisDurumu = ogrenci.OgrenciCikisDurumu,
                    GecisTipi = sonuc.GecisTipi,
                    Istasyon = cihaz.IstasyonTipi.ToString(),
                    CihazAdi = cihaz.CihazAdi,
                    CihazKodu = cihaz.CihazKodu,
                    Reason =
                        cihaz.IstasyonTipi == IstasyonTipi.Yemekhane ? "YEMEKHANE_OK" :
                        (cihaz.IstasyonTipi == IstasyonTipi.AnaKapi &&
                         string.Equals(sonuc.GecisTipi, "Çıkış", StringComparison.OrdinalIgnoreCase))
                            ? "ANA_KAPI_OGLE_OK" : "GENEL_OK",
                    Info =
                        cihaz.IstasyonTipi == IstasyonTipi.Yemekhane ? Msg.InfoYemekOk :
                        (cihaz.IstasyonTipi == IstasyonTipi.AnaKapi &&
                         string.Equals(sonuc.GecisTipi, "Çıkış", StringComparison.OrdinalIgnoreCase))
                            ? Msg.InfoOglenOk : Msg.InfoGenelOk
                };

                await _hub.Clients.All.SendAsync("OgrenciBilgisiAl", dto, ct);

                return Ok(new
                {
                    durum = "OK",
                    gecisTipi = sonuc.GecisTipi,
                    saat = now.ToString("HH:mm"),
                    cihaz = cihaz.CihazAdi
                });
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("UsbKartOkundu iptal edildi.");
                return StatusCode(499);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UsbKartOkundu sırasında hata.");
                return StatusCode(500, "Beklenmeyen bir hata oluştu.");
            }
        }
    }
}