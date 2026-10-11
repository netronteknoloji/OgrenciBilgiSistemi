using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using OgrenciBilgiSistemi.Dtos;
using OgrenciBilgiSistemi.Hubs;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Services.Implementations;
using OgrenciBilgiSistemi.Services.Interfaces;
using OgrenciBilgiSistemi.Shared.Helpers;
using OgrenciBilgiSistemi.Shared.Kurallar;
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
        private readonly IGecisKarariService _gecisKarari;
        private readonly IHubContext<KartOkuHub> _hub;
        private readonly TenantKapsamFabrikasi _tenantKapsam;
        private readonly ILogger<KartOkuController> _logger;
        private readonly TenantBaglami _tenant;

        public KartOkuController(
            IGecisService gecisService,
            ICihazService cihazService,
            IKartOkuService kartOkuService,
            IGecisKarariService gecisKarari,
            IHubContext<KartOkuHub> hub,
            TenantKapsamFabrikasi tenantKapsam,
            ILogger<KartOkuController> logger,
            TenantBaglami tenant)
        {
            _gecisService = gecisService;
            _cihazService = cihazService;
            _kartOkuService = kartOkuService;
            _gecisKarari = gecisKarari;
            _hub = hub;
            _tenantKapsam = tenantKapsam;
            _logger = logger;
            _tenant = tenant;
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

                var no = KartNo.Normalize(kartNo);
                if (string.IsNullOrEmpty(no))
                    return BadRequest("Kart numarası geçersiz.");

                var ogrenci = await _kartOkuService.GetOgrenciByKartNoAsync(no, ct);

                if (ogrenci is null)
                {
                    await KartOkumaYayini.GonderAsync(_hub, _tenant.OkulKodu, new OgrenciBilgisiDto
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
                        Error = GecisKurallari.Mesajlar.KartTanimsiz
                    }, ct);

                    return NotFound("Kart tanımsız.");
                }

                // Kontrol + kayıt bu öğrenci için tek seferde (istek sonunda serbest bırakılır)
                using var ogrenciKilidi = await GecisKilidi.AlAsync(_tenant.OkulKodu, ogrenci.OgrenciId, ct);

                var now = DateTime.Now;

                // İzin, saat penceresi ve günlük limit: ortak kural (ZKTeco yolu da aynısını kullanır)
                var karar = await _gecisKarari.KararVerAsync(cihaz.IstasyonTipi, ogrenci, now, ct);
                if (!karar.KayitYaz)
                {
                    await KartOkumaYayini.GonderAsync(_hub, _tenant.OkulKodu,
                        OgrenciBilgisiDto.Olustur(ogrenci, cihaz, "Reddedildi", karar.Reason, error: karar.Mesaj,
                            cikisSaati: karar.OncekiCikisZamani?.ToString("HH:mm") ?? "-"), ct);

                    return Ok(new { durum = "YOK_SAYILDI", gerekce = karar.Mesaj, reason = karar.Reason });
                }

                var sonuc = await _gecisService.KaydetAsync(
                    cihaz.CihazId, ogrenci.OgrenciId, cihaz.IstasyonTipi, now, ct, karar.ZorunluGecisTipi);

                var girisMi = string.Equals(sonuc.GecisTipi, GecisKurallari.Giris, StringComparison.OrdinalIgnoreCase);
                var cikisMi = string.Equals(sonuc.GecisTipi, GecisKurallari.Cikis, StringComparison.OrdinalIgnoreCase);
                var (reason, info) = GecisKurallari.BasariMesaji(cihaz.IstasyonTipi, sonuc.GecisTipi);

                await KartOkumaYayini.GonderAsync(_hub, _tenant.OkulKodu,
                    OgrenciBilgisiDto.Olustur(ogrenci, cihaz, sonuc.GecisTipi, reason, info: info,
                        girisSaati: girisMi ? now.ToString("HH:mm") : "-",
                        cikisSaati: cikisMi ? now.ToString("HH:mm") : "-"), ct);

                // Ana Kapı geçişlerinde veliye SMS bildirimi (fire-and-forget)
                if (cihaz.IstasyonTipi == IstasyonTipi.AnaKapi && (girisMi || cikisMi))
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
