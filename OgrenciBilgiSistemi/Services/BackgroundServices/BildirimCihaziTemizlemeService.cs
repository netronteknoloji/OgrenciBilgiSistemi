using Microsoft.EntityFrameworkCore;
using OgrenciBilgiSistemi.Data;
using OgrenciBilgiSistemi.Shared.Services;

namespace OgrenciBilgiSistemi.Services.BackgroundServices;

/// <summary>
/// 90 günden eski soft-deleted FCM token kayıtlarını günlük olarak fiziksel siler.
/// Soft-delete unique index yalnızca IsDeleted=0 satırlarını kapsar; eski silinmiş
/// token'lar birikmesin diye periyodik temizlik gerekir.
/// </summary>
public sealed class BildirimCihaziTemizlemeService : BackgroundService
{
    private static readonly TimeSpan POLL_INTERVAL = TimeSpan.FromDays(1);
    private const int TOKEN_OMUR_GUN = 90;

    private readonly TenantKapsamFabrikasi _tenantKapsam;
    private readonly ILogger<BildirimCihaziTemizlemeService> _logger;

    public BildirimCihaziTemizlemeService(
        TenantKapsamFabrikasi tenantKapsam,
        ILogger<BildirimCihaziTemizlemeService> logger)
    {
        _tenantKapsam = tenantKapsam;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("FCM cihaz token temizleme servisi başlatıldı.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _tenantKapsam.HerOkulIcinAsync("FCM token temizliği", EskiTokenlariSil, stoppingToken);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FCM token temizleme turu sırasında hata.");
            }

            try { await Task.Delay(POLL_INTERVAL, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task EskiTokenlariSil(IServiceProvider sp, CancellationToken ct)
    {
        var db = sp.GetRequiredService<AppDbContext>();

        await RefreshTokenlariSil(db, ct);

        var sinir = DateTime.Now.AddDays(-TOKEN_OMUR_GUN);

        // IgnoreQueryFilters: soft-delete filtresini bypass et; IsDeleted=1 olanları hedefliyoruz.
        // RemoveRange burada kasıtlı hard-delete — bu servisin amacı fiziksel temizlik.
        var silinecekler = await db.BildirimCihazlari
            .IgnoreQueryFilters()
            .Where(b => b.IsDeleted && b.SonGuncelleme < sinir)
            .ToListAsync(ct);

        if (silinecekler.Count == 0) return;

        db.BildirimCihazlari.RemoveRange(silinecekler);
        await db.SaveChangesAsync(ct);

        _logger.LogInformation("FCM token temizliği: {Count} eski kayıt fiziksel olarak silindi.", silinecekler.Count);
    }

    // Mobil API refresh token'ları: kullanılmış/iptal edilmiş veya süresi dolmuş olanlar 7 gün sonra silinir (UTC)
    private async Task RefreshTokenlariSil(AppDbContext db, CancellationToken ct)
    {
        var sinir = DateTime.UtcNow.AddDays(-7);
        var silinen = await db.RefreshTokenlar
            .Where(r => (r.IptalZamani != null && r.IptalZamani < sinir) || r.SonKullanim < sinir)
            .ExecuteDeleteAsync(ct);

        if (silinen > 0)
            _logger.LogInformation("Refresh token temizliği: {Count} eski kayıt silindi.", silinen);
    }
}
