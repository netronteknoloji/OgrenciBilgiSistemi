using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using OgrenciBilgiSistemi.Dtos;
using OgrenciBilgiSistemi.Hubs;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Services.Interfaces;
using OgrenciBilgiSistemi.Shared.Services;
using OgrenciBilgiSistemi.ViewModels;

namespace OgrenciBilgiSistemi.Controllers
{
    [Route("KartOku")]
    public class KartOkuController : Controller
    {
        private readonly IGecisService _gecisService;
        private readonly ICihazService _cihazService;
        private readonly IKartOkuService _kartOkuService;
        private readonly IHubContext<KartOkuHub> _hub;
        private readonly TenantKapsamFabrikasi _tenantKapsam;
        private readonly ILogger<KartOkuController> _logger;
        private readonly TenantBaglami _tenant;
        private readonly KartOkumaAyarlari _ayar;

        // Aynı öğrencinin eşzamanlı okutmaları (okuyucunun kartı iki kez göndermesi) sırayla işlenir;
        // aksi halde iki istek de "bugün çıkış yok" görüp iki ÇIKIŞ + iki SMS yazabilir.
        // Tek süreçli IIS app pool varsayımıyla bellek içi kilit yeterlidir.
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _ogrenciKilitleri = new();

        public KartOkuController(
            IGecisService gecisService,
            ICihazService cihazService,
            IKartOkuService kartOkuService,
            IHubContext<KartOkuHub> hub,
            TenantKapsamFabrikasi tenantKapsam,
            ILogger<KartOkuController> logger,
            TenantBaglami tenant,
            IOptions<KartOkumaAyarlari> ayar)
        {
            _gecisService = gecisService;
            _cihazService = cihazService;
            _kartOkuService = kartOkuService;
            _hub = hub;
            _tenantKapsam = tenantKapsam;
            _logger = logger;
            _tenant = tenant;
            _ayar = ayar.Value;
        }

        private sealed class KilitSerbestBirakici(SemaphoreSlim kilit) : IDisposable
        {
            public void Dispose() => kilit.Release();
        }

        private async Task<IDisposable> OgrenciKilidiAlAsync(int ogrenciId, CancellationToken ct)
        {
            var kilit = _ogrenciKilitleri.GetOrAdd($"{_tenant.OkulKodu}:{ogrenciId}", _ => new SemaphoreSlim(1, 1));
            await kilit.WaitAsync(ct);
            return new KilitSerbestBirakici(kilit);
        }

        // Yavaş/ölü client'ın kart okumayı blocklamasını engellemek için
        // SignalR yayınlarını kısa bir timeout ile sarmalar.
        private static async Task GuvenliYayinAsync<T>(
            IHubContext<KartOkuHub> hub, T dto, CancellationToken ct, int timeoutMs = 3000)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(timeoutMs);
            try
            {
                await hub.Clients.All.SendAsync("OgrenciBilgisiAl", dto, cts.Token);
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested && !ct.IsCancellationRequested)
            {
                // Sessiz: client cevap vermedi, kart akışını blocklamayalım
            }
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
            public const string InfoOgleDonusOk = "ÖĞLE DÖNÜŞÜ ONAYLANDI.";
            public const string ErrKartTanimsiz = "KART TANIMSIZ!";
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(string? cihazKodu, CancellationToken ct)
        {
            try
            {
                CihazModel? cihaz = null;

                if (Guid.TryParse(cihazKodu?.Trim().Trim('<', '>'), out var guid))
                    cihaz = await _cihazService.CihazBulByKodAsync(guid, ct);
                else
                    cihaz = await _cihazService.CihazBulVarsayilanAsync(ct);

                if (cihaz is null)
                    return View(new KartOkumaVm { HataMesaji = "Cihaz bilgisi eksik." });

                return View(new KartOkumaVm { CihazKodu = cihaz.CihazKodu.ToString() });
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

                var cihaz = await _cihazService.CihazBulAktifByKodAsync(guid, ct);

                if (cihaz is null)
                    return NotFound("Cihaz bulunamadı veya pasif.");

                var no = NormalizeKartNo(kartNo);
                if (string.IsNullOrEmpty(no))
                    return BadRequest("Kart numarası geçersiz.");

                var ogrenci = await _kartOkuService.GetOgrenciByKartNoAsync(no, ct);

                if (ogrenci is null)
                {
                    await GuvenliYayinAsync(_hub, new OgrenciBilgisiDto
                    {
                        OgrenciAdSoyad = "-",
                        OgrenciSinif = "-",
                        OgrenciGirisSaati = "-",
                        OgrenciCikisSaati = "-",
                        GecisTipi = "Reddedildi",
                        Istasyon = cihaz.IstasyonTipi.ToString(),
                        CihazAdi = cihaz.CihazAdi,
                        CihazKodu = cihaz.CihazKodu,
                        Reason = "KART_TANIMSIZ",
                        Error = Msg.ErrKartTanimsiz
                    }, ct);

                    return NotFound("Kart tanımsız.");
                }

                // Kontrol + kayıt bu öğrenci için tek seferde (istek sonunda serbest bırakılır)
                using var ogrenciKilidi = await OgrenciKilidiAlAsync(ogrenci.OgrenciId, ct);

                var ogrenciSinif = ogrenci.Birim?.BirimAd ?? "-";
                var now = DateTime.Now;
                var today = now.Date;
                var tomorrow = today.AddDays(1);
                string? forcedGecisTipi = null;

                // --- İzin kontrolleri (LOG ATMADAN) ---
                if (cihaz.IstasyonTipi == IstasyonTipi.AnaKapi &&
                    ogrenci.OgrenciCikisDurumu == OglenCikisDurumu.Hayir)
                {
                    await GuvenliYayinAsync(_hub, new OgrenciBilgisiDto
                    {
                        OgrenciAdSoyad = ogrenci.OgrenciAdSoyad,
                        OgrenciNo = ogrenci.OgrenciNo,
                        OgrenciSinif = ogrenciSinif,
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
                    var yemekIzniVar = await _kartOkuService.YemekIzniVarMiAsync(ogrenci.OgrenciId, yil, ay, ct);

                    if (!yemekIzniVar)
                    {
                        await GuvenliYayinAsync(_hub, new OgrenciBilgisiDto
                        {
                            OgrenciAdSoyad = ogrenci.OgrenciAdSoyad,
                            OgrenciNo = ogrenci.OgrenciNo,
                            OgrenciSinif = ogrenciSinif,
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
                    var bugunYemekVar = await _kartOkuService.BugunYemekGirisiVarMiAsync(ogrenci.OgrenciId, today, tomorrow, ct);

                    if (bugunYemekVar)
                    {
                        await GuvenliYayinAsync(_hub, new OgrenciBilgisiDto
                        {
                            OgrenciAdSoyad = ogrenci.OgrenciAdSoyad,
                            OgrenciNo = ogrenci.OgrenciNo,
                            OgrenciSinif = ogrenciSinif,
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
                    var (cikisVarMi, girisVarMi, cikisZamani) = await _kartOkuService.BugunAnaKapiHareketleriAsync(
                        ogrenci.OgrenciId, today, tomorrow, ct);

                    if (!cikisVarMi && !_ayar.OgleCikisSaatiMi(now))
                    {
                        // Öğle çıkış saati dışında: kayıt/SMS yok
                        await GuvenliYayinAsync(_hub, new OgrenciBilgisiDto
                        {
                            OgrenciAdSoyad = ogrenci.OgrenciAdSoyad,
                            OgrenciNo = ogrenci.OgrenciNo,
                            OgrenciSinif = ogrenciSinif,
                            OgrenciGorsel = ogrenci.OgrenciGorsel,
                            OgrenciGirisSaati = "-",
                            OgrenciCikisSaati = "-",
                            OglenCikisDurumu = ogrenci.OgrenciCikisDurumu,
                            GecisTipi = "Reddedildi",
                            Istasyon = cihaz.IstasyonTipi.ToString(),
                            CihazAdi = cihaz.CihazAdi,
                            CihazKodu = cihaz.CihazKodu,
                            Reason = "ANA_KAPI_OGLE_SAAT_DISI",
                            Error = $"ÖĞLE ÇIKIŞ SAATİ DEĞİL ({_ayar.OgleCikisAraligiMetni})!"
                        }, ct);

                        return Ok(new { durum = "YOK_SAYILDI", gerekce = "Öğle çıkış saati dışında.", reason = "ANA_KAPI_OGLE_SAAT_DISI" });
                    }
                    else if (!cikisVarMi)
                    {
                        forcedGecisTipi = "Çıkış";
                    }
                    else if (!girisVarMi && cikisZamani.HasValue && now - cikisZamani.Value < _ayar.OgleDonusMinSure)
                    {
                        // Çıkıştan hemen sonra tekrar okutma: dönüş sayma, kayıt/SMS yok
                        await GuvenliYayinAsync(_hub, new OgrenciBilgisiDto
                        {
                            OgrenciAdSoyad = ogrenci.OgrenciAdSoyad,
                            OgrenciNo = ogrenci.OgrenciNo,
                            OgrenciSinif = ogrenciSinif,
                            OgrenciGorsel = ogrenci.OgrenciGorsel,
                            OgrenciGirisSaati = "-",
                            OgrenciCikisSaati = cikisZamani.Value.ToString("HH:mm"),
                            OglenCikisDurumu = ogrenci.OgrenciCikisDurumu,
                            GecisTipi = "Reddedildi",
                            Istasyon = cihaz.IstasyonTipi.ToString(),
                            CihazAdi = cihaz.CihazAdi,
                            CihazKodu = cihaz.CihazKodu,
                            Reason = "ANA_KAPI_OGLE_CIKIS_YAPILDI",
                            Error = $"Öğle çıkışı zaten yapıldı ({cikisZamani.Value:HH:mm})."
                        }, ct);

                        return Ok(new { durum = "YOK_SAYILDI", gerekce = "Öğle çıkışı az önce yapıldı.", reason = "ANA_KAPI_OGLE_CIKIS_YAPILDI" });
                    }
                    else if (!girisVarMi)
                    {
                        forcedGecisTipi = "Giriş";
                    }
                    else
                    {
                        await GuvenliYayinAsync(_hub, new OgrenciBilgisiDto
                        {
                            OgrenciAdSoyad = ogrenci.OgrenciAdSoyad,
                            OgrenciNo = ogrenci.OgrenciNo,
                            OgrenciSinif = ogrenciSinif,
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

                // --- Kayıt: istasyona göre netleştir ---
                if (forcedGecisTipi is null)
                {
                    forcedGecisTipi = cihaz.IstasyonTipi switch
                    {
                        IstasyonTipi.Yemekhane => "Giriş",
                        IstasyonTipi.AnaKapi when ogrenci.OgrenciCikisDurumu != OglenCikisDurumu.Evet => null,
                        _ => null
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
                    OgrenciSinif = ogrenciSinif,
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
                        cihaz.IstasyonTipi != IstasyonTipi.AnaKapi ? "GENEL_OK" :
                        string.Equals(sonuc.GecisTipi, "Çıkış", StringComparison.OrdinalIgnoreCase) ? "ANA_KAPI_OGLE_OK" :
                        string.Equals(sonuc.GecisTipi, "Giriş", StringComparison.OrdinalIgnoreCase) ? "ANA_KAPI_OGLE_DONUS_OK" :
                        "GENEL_OK",
                    Info =
                        cihaz.IstasyonTipi == IstasyonTipi.Yemekhane ? Msg.InfoYemekOk :
                        cihaz.IstasyonTipi != IstasyonTipi.AnaKapi ? Msg.InfoGenelOk :
                        string.Equals(sonuc.GecisTipi, "Çıkış", StringComparison.OrdinalIgnoreCase) ? Msg.InfoOglenOk :
                        string.Equals(sonuc.GecisTipi, "Giriş", StringComparison.OrdinalIgnoreCase) ? Msg.InfoOgleDonusOk :
                        Msg.InfoGenelOk
                };

                await GuvenliYayinAsync(_hub, dto, ct);

                // Ana Kapı geçişlerinde veliye SMS bildirimi (fire-and-forget)
                if (cihaz.IstasyonTipi == IstasyonTipi.AnaKapi &&
                    (string.Equals(sonuc.GecisTipi, "Giriş", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(sonuc.GecisTipi, "Çıkış", StringComparison.OrdinalIgnoreCase)))
                {
                    var ogrId = ogrenci.OgrenciId;
                    var ogrAdSoyad = ogrenci.OgrenciAdSoyad;
                    var gecisTipiKopya = sonuc.GecisTipi;
                    var anlikZaman = now;

                    var okulKodu = _tenant.OkulKodu;

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            // İstek kapsamı bitmiş olabilir; SMS aynı okulun kapsamında gönderilir
                            await using var smsScope = _tenantKapsam.Olustur(okulKodu);
                            var smsSvc = smsScope.ServiceProvider.GetRequiredService<ISmsGonderimService>();
                            await smsSvc.GecisSmsBildir(ogrId, ogrAdSoyad, gecisTipiKopya, anlikZaman);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "SMS gönderim hatası. Öğrenci: {OgrId}", ogrId);
                        }
                    });
                }

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
