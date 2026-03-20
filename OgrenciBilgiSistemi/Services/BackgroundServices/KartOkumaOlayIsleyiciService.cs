using Microsoft.EntityFrameworkCore;
using OgrenciBilgiSistemi.Data;
using OgrenciBilgiSistemi.Models.Enums;
using OgrenciBilgiSistemi.Services.Interfaces;

public class KartOkumaOlayIsleyiciService : IHostedService
{
    private readonly IZKTecoService _zkTecoService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<KartOkumaOlayIsleyiciService> _logger;

    public KartOkumaOlayIsleyiciService(
        IZKTecoService zkTecoService,
        IServiceScopeFactory scopeFactory,
        ILogger<KartOkumaOlayIsleyiciService> logger)
    {
        _zkTecoService = zkTecoService;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // (opsiyonel) bağlantı kur
        try { await _zkTecoService.ConnectAsync(); } catch { /* loglanabilir */ }

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
            await svc.KaydetAsync(cihaz.CihazId, ogr.OgrenciId, cihaz.IstasyonTipi, now);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kart okuma işleminde hata. Kart: {Kart}", norm);
        }
    }
}