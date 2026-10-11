using OgrenciBilgiSistemi.Api.Services.Interfaces;
using OgrenciBilgiSistemi.Shared.Services;

namespace OgrenciBilgiSistemi.Api.Services;

/// <summary>
/// Güvenlik ağı: yoklama kaydedildikten sonra kuyruğa alınamamış SMS'leri (ör. istek yarıda kesildi)
/// her okul için periyodik olarak kuyruğa alır. Gönderim ve tekrar deneme MVC'deki GonderimKuyruguIsleyici'dedir.
/// </summary>
public sealed class YoklamaSmsKuyrukTarayici : BackgroundService
{
    private static readonly TimeSpan Periyot = TimeSpan.FromMinutes(1);

    private readonly TenantKapsamFabrikasi _tenantKapsam;
    private readonly ILogger<YoklamaSmsKuyrukTarayici> _logger;

    public YoklamaSmsKuyrukTarayici(TenantKapsamFabrikasi tenantKapsam, ILogger<YoklamaSmsKuyrukTarayici> logger)
    {
        _tenantKapsam = tenantKapsam;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Yoklama SMS kuyruk tarayıcısı başlatıldı.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _tenantKapsam.HerOkulIcinAsync("Yoklama SMS kuyruk taraması", async (sp, ct) =>
                {
                    var eklenen = await sp.GetRequiredService<IYoklamaSmsBildirimService>().BekleyenleriKuyrugaAl(ct);
                    if (eklenen > 0)
                        _logger.LogInformation("Taramada {Adet} yoklama SMS'i kuyruğa alındı.", eklenen);
                }, stoppingToken);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Yoklama SMS kuyruk taraması sırasında hata.");
            }

            try { await Task.Delay(Periyot, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }
}
