using OgrenciBilgiSistemi.Services.Interfaces;

public sealed class ZkBaglantiIzleyiciHostedService : BackgroundService
{
    private readonly IZKTecoService _zkTecoService;
    private readonly ILogger<ZkBaglantiIzleyiciHostedService> _logger;

    public ZkBaglantiIzleyiciHostedService(
        IZKTecoService zkTecoService,
        ILogger<ZkBaglantiIzleyiciHostedService> logger)
    {
        _zkTecoService = zkTecoService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await _zkTecoService.MonitorConnectionsAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("ZKTeco bağlantı izleme servisi durduruldu.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ZKTeco bağlantı izleme servisinde beklenmeyen hata.");
        }
    }
}
