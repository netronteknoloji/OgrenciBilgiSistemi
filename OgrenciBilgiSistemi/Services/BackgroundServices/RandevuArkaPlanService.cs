using Microsoft.EntityFrameworkCore;
using OgrenciBilgiSistemi.Data;
using OgrenciBilgiSistemi.Services.Interfaces;
using OgrenciBilgiSistemi.Shared.Enums;

namespace OgrenciBilgiSistemi.Services.BackgroundServices;

public sealed class RandevuArkaPlanService : BackgroundService
{
    private static readonly TimeSpan POLL_INTERVAL = TimeSpan.FromMinutes(15);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RandevuArkaPlanService> _logger;

    public RandevuArkaPlanService(IServiceScopeFactory scopeFactory, ILogger<RandevuArkaPlanService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Randevu arka plan servisi başlatıldı.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TamamlanmisRandevulariIsaretle(stoppingToken);
                await HatirlatmaGonder(stoppingToken);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Randevu arka plan turu sırasında hata.");
            }

            try { await Task.Delay(POLL_INTERVAL, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task TamamlanmisRandevulariIsaretle(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var simdi = DateTime.Now;
        var gecmisRandevular = await db.Randevular
            .Where(r => r.Durum == RandevuDurumu.Onaylandi)
            .Where(r => r.RandevuTarihi.AddMinutes(r.SureDakika) < simdi)
            .ToListAsync(ct);

        if (gecmisRandevular.Count == 0) return;

        foreach (var randevu in gecmisRandevular)
        {
            randevu.Durum = RandevuDurumu.Tamamlandi;
            randevu.GuncellenmeTarihi = simdi;
        }

        await db.SaveChangesAsync(ct);
        _logger.LogInformation("{Adet} randevu tamamlandı olarak işaretlendi.", gecmisRandevular.Count);
    }

    private async Task HatirlatmaGonder(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var bildirimService = scope.ServiceProvider.GetRequiredService<IBildirimService>();

        var simdi = DateTime.Now;
        var birSaatSonra = simdi.AddHours(1);

        var yaklasanRandevular = await db.Randevular
            .Include(r => r.Ogretmen)
            .Include(r => r.Veli)
            .Where(r => r.Durum == RandevuDurumu.Onaylandi)
            .Where(r => r.RandevuTarihi > simdi && r.RandevuTarihi <= birSaatSonra)
            .ToListAsync(ct);

        foreach (var randevu in yaklasanRandevular)
        {
            var dahaOnceGonderildi = await db.Bildirimler
                .IgnoreQueryFilters()
                .AnyAsync(b => b.RandevuId == randevu.RandevuId
                            && b.Tur == BildirimTuru.RandevuHatirlatma, ct);

            if (dahaOnceGonderildi) continue;

            var tarihStr = randevu.RandevuTarihi.ToString("dd.MM.yyyy HH:mm");

            await bildirimService.Olustur(
                randevu.OgretmenKullaniciId,
                BildirimTuru.RandevuHatirlatma,
                $"{tarihStr} tarihli randevunuz yaklaşıyor.",
                randevu.RandevuId, ct);

            await bildirimService.Olustur(
                randevu.VeliKullaniciId,
                BildirimTuru.RandevuHatirlatma,
                $"{tarihStr} tarihli randevunuz yaklaşıyor.",
                randevu.RandevuId, ct);
        }

        if (yaklasanRandevular.Count > 0)
            _logger.LogInformation("Randevu hatırlatmaları kontrol edildi, {Adet} yaklaşan randevu.", yaklasanRandevular.Count);
    }
}
