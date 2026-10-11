using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OgrenciBilgiSistemi.Data;
using OgrenciBilgiSistemi.Dtos;
using OgrenciBilgiSistemi.Hubs;
using OgrenciBilgiSistemi.Services.Implementations;
using OgrenciBilgiSistemi.Services.Interfaces;
using OgrenciBilgiSistemi.Shared.Helpers;
using OgrenciBilgiSistemi.Shared.Kurallar;
using OgrenciBilgiSistemi.Shared.Services;

public class KartOkumaOlayIsleyiciService : IHostedService
{
    private readonly IZKTecoService _zkTecoService;
    private readonly TenantKapsamFabrikasi _tenantKapsam;
    private readonly IHubContext<KartOkuHub> _hub;
    private readonly ILogger<KartOkumaOlayIsleyiciService> _logger;

    public KartOkumaOlayIsleyiciService(
        IZKTecoService zkTecoService,
        TenantKapsamFabrikasi tenantKapsam,
        IHubContext<KartOkuHub> hub,
        ILogger<KartOkumaOlayIsleyiciService> logger)
    {
        _zkTecoService = zkTecoService;
        _tenantKapsam = tenantKapsam;
        _hub = hub;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // (opsiyonel) bağlantı kur
        try { await _zkTecoService.ConnectAsync(); }
        catch (Exception ex) { _logger.LogWarning(ex, "ZKTeco bağlantısı kurulamadı, arka planda tekrar denenecek."); }

        _zkTecoService.OnCardReadAsync -= KartOkumaIsleyiciAsync; // güvenlik
        _zkTecoService.OnCardReadAsync += KartOkumaIsleyiciAsync;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _zkTecoService.OnCardReadAsync -= KartOkumaIsleyiciAsync;
        return Task.CompletedTask;
    }

    private async Task KartOkumaIsleyiciAsync(string kartNo)
    {
        var now = DateTime.Now;
        var norm = KartNo.Normalize(kartNo);
        if (norm is null) return;

        // Olay, bağlı cihazın okulunda işlenir; okul bilinmiyorsa hiçbir DB'ye yazılmaz
        var okulKodu = _zkTecoService.BagliOkulKodu;
        if (string.IsNullOrEmpty(okulKodu))
        {
            _logger.LogWarning("ZKTeco kart olayı geldi ama bağlı cihazın okulu bilinmiyor; işlenmedi.");
            return;
        }

        try
        {
            await using var scope = _tenantKapsam.Olustur(okulKodu);
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var kartOku = scope.ServiceProvider.GetRequiredService<IKartOkuService>();
            var gecisKarari = scope.ServiceProvider.GetRequiredService<IGecisKarariService>();
            var svc = scope.ServiceProvider.GetRequiredService<IGecisService>();

            // 1) Olayı üreten cihaz: bağlı cihaz (yoksa okulun ilk aktif ZKTeco cihazı)
            var bagliCihazId = _zkTecoService.CurrentCihazId;
            var cihaz = await db.Cihazlar.AsNoTracking()
                .Where(c => c.DonanimTipi == DonanimTipi.ZKTeco && !c.IsDeleted
                         && (bagliCihazId == null || c.CihazId == bagliCihazId))
                .OrderBy(c => c.CihazId)
                .FirstOrDefaultAsync();

            if (cihaz is null)
            {
                _logger.LogWarning("Aktif ZKTeco cihazı bulunamadı.");
                return;
            }

            // 2) Öğrenciyi bul
            var ogr = await kartOku.GetOgrenciByKartNoAsync(norm);
            if (ogr is null)
            {
                _logger.LogWarning("ZKTeco okutması: kart tanımsız (CihazId: {CihazId}).", cihaz.CihazId);
                return;
            }

            // 3) USB yoluyla aynı kilit ve aynı kural: izin, saat penceresi, günlük limit
            using var ogrenciKilidi = await GecisKilidi.AlAsync(okulKodu, ogr.OgrenciId);

            var karar = await gecisKarari.KararVerAsync(cihaz.IstasyonTipi, ogr, now);
            if (!karar.KayitYaz)
            {
                await KartOkumaYayini.GonderAsync(_hub, okulKodu,
                    OgrenciBilgisiDto.Olustur(ogr, cihaz, "Reddedildi", karar.Reason, error: karar.Mesaj,
                        cikisSaati: karar.OncekiCikisZamani?.ToString("HH:mm") ?? "-"), CancellationToken.None);
                return;
            }

            // 4) Geçişi kaydet — yön, kural zorunlu kılmadıysa GecisService.KaydetAsync içinde belirlenir
            var sonuc = await svc.KaydetAsync(cihaz.CihazId, ogr.OgrenciId, cihaz.IstasyonTipi, now,
                CancellationToken.None, karar.ZorunluGecisTipi);

            var girisMi = string.Equals(sonuc.GecisTipi, GecisKurallari.Giris, StringComparison.OrdinalIgnoreCase);
            var cikisMi = string.Equals(sonuc.GecisTipi, GecisKurallari.Cikis, StringComparison.OrdinalIgnoreCase);

            // 5) SMS bildirimi - sadece Ana Kapı geçişlerinde (Yemekhane SMS'i ayrı serviste).
            // Fire-and-forget: SMS gönderimi kart okuma akışını bloklamaz.
            if (cihaz.IstasyonTipi == IstasyonTipi.AnaKapi && (girisMi || cikisMi))
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await using var smsScope = _tenantKapsam.Olustur(okulKodu);
                        var smsSvc = smsScope.ServiceProvider.GetRequiredService<ISmsGonderimService>();
                        await smsSvc.GecisSmsBildir(ogr.OgrenciId, ogr.OgrenciAdSoyad, sonuc.GecisTipi, now);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "SMS gönderim hatası. Öğrenci: {OgrId}", ogr.OgrenciId);
                    }
                });
            }

            // 6) Kart okuma ekranına sonuç
            var (reason, info) = GecisKurallari.BasariMesaji(cihaz.IstasyonTipi, sonuc.GecisTipi);
            await KartOkumaYayini.GonderAsync(_hub, okulKodu,
                OgrenciBilgisiDto.Olustur(ogr, cihaz, sonuc.GecisTipi, reason, info: info,
                    girisSaati: girisMi ? now.ToString("HH:mm") : "-",
                    cikisSaati: cikisMi ? now.ToString("HH:mm") : "-"), CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kart okuma işleminde hata (okul {OkulKodu}).", okulKodu);
        }
    }
}
