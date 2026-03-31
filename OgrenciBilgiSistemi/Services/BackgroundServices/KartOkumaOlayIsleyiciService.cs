using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OgrenciBilgiSistemi.Data;
using OgrenciBilgiSistemi.Dtos;
using OgrenciBilgiSistemi.Hubs;
using OgrenciBilgiSistemi.Services.Interfaces;

public class KartOkumaOlayIsleyiciService : IHostedService
{
    private readonly IZKTecoService _zkTecoService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<KartOkuHub> _hub;
    private readonly ILogger<KartOkumaOlayIsleyiciService> _logger;

    public KartOkumaOlayIsleyiciService(
        IZKTecoService zkTecoService,
        IServiceScopeFactory scopeFactory,
        IHubContext<KartOkuHub> hub,
        ILogger<KartOkumaOlayIsleyiciService> logger)
    {
        _zkTecoService = zkTecoService;
        _scopeFactory = scopeFactory;
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

    private static string Normalize(string s) => (s ?? string.Empty).Trim().TrimStart('0');

    private async Task KartOkumaIsleyiciAsync(string kartNo)
    {
        var now = DateTime.Now;
        var norm = Normalize(kartNo);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var svc = scope.ServiceProvider.GetRequiredService<IGecisService>();

            // 1) Öğrenciyi bul
            var ogr = await db.Ogrenciler.AsNoTracking()
                .FirstOrDefaultAsync(o => o.OgrenciKartNo == norm && o.OgrenciDurum == true);
            if (ogr is null)
            {
                _logger.LogWarning("ZKTeco kart tanımsız: {Kart}", norm);
                return;
            }

            // 2) Bu worker’ın kullanacağı ZKTeco cihazını seç
            // (Basit: ilk aktif ZKTeco. İstersen appsettings ile spesifik CihazKodu/Id bağlayabilirsin.)
            var cihaz = await db.Cihazlar.AsNoTracking()
                .Where(c => c.DonanimTipi == DonanimTipi.ZKTeco && c.Aktif)
                .OrderBy(c => c.CihazId)
                .FirstOrDefaultAsync();

            if (cihaz is null)
            {
                _logger.LogWarning("Aktif ZKTeco cihazı bulunamadı.");
                return;
            }

            // 3) Geçişi kaydet — yön kararı (giriş/çıkış) GecisService.KaydetAsync içinde belirlenir
            var sonuc = await svc.KaydetAsync(cihaz.CihazId, ogr.OgrenciId, cihaz.IstasyonTipi, now);

            // 3.5) SMS bildirimi (fire-and-forget)
            _ = Task.Run(async () =>
            {
                try
                {
                    using var smsScope = _scopeFactory.CreateScope();
                    var smsSvc = smsScope.ServiceProvider.GetRequiredService<ISmsGonderimService>();
                    await smsSvc.GecisSmsBildir(ogr.OgrenciId, ogr.OgrenciAdSoyad, sonuc.GecisTipi, now);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "SMS gönderim hatası. Öğrenci: {OgrId}", ogr.OgrenciId);
                }
            });

            // 4) Sınıf bilgisini çek
            var sinifAdi = await db.Birimler.AsNoTracking()
                .Where(b => b.BirimId == ogr.BirimId)
                .Select(b => b.BirimAd)
                .FirstOrDefaultAsync() ?? "-";

            // 5) SignalR ile öğrenci bilgisini gönder
            var girisSaati = "-";
            var cikisSaati = "-";
            if (string.Equals(sonuc.GecisTipi, "Giriş", StringComparison.OrdinalIgnoreCase))
                girisSaati = now.ToString("HH:mm");
            else if (string.Equals(sonuc.GecisTipi, "Çıkış", StringComparison.OrdinalIgnoreCase))
                cikisSaati = now.ToString("HH:mm");

            var dto = new OgrenciBilgisiDto
            {
                OgrenciAdSoyad = ogr.OgrenciAdSoyad,
                OgrenciNo = ogr.OgrenciNo,
                OgrenciSinif = sinifAdi,
                OgrenciGorsel = ogr.OgrenciGorsel,
                OgrenciGirisSaati = girisSaati,
                OgrenciCikisSaati = cikisSaati,
                OglenCikisDurumu = ogr.OgrenciCikisDurumu,
                GecisTipi = sonuc.GecisTipi,
                Istasyon = cihaz.IstasyonTipi.ToString(),
                CihazAdi = cihaz.CihazAdi,
                CihazKodu = cihaz.CihazKodu,
                Reason = "GENEL_OK",
                Info = "Geçiş başarılı."
            };

            await _hub.Clients.All.SendAsync("OgrenciBilgisiAl", dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kart okuma işleminde hata. Kart: {Kart}", norm);
        }
    }
}