using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OgrenciBilgiSistemi.Shared.Models;

namespace OgrenciBilgiSistemi.Shared.Services
{
    /// <summary>
    /// HTTP isteği dışında (arka plan servisi, Task.Run) belirli bir okul için DI kapsamı açar.
    /// Açılan kapsamdaki <see cref="TenantBaglami"/> doldurulmuş gelir; okul belirtilmeden
    /// DB'ye erişilmemesi için tek giriş noktasıdır. Singleton olarak kayıt edilmelidir.
    /// </summary>
    public sealed class TenantKapsamFabrikasi
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly OkulYapilandirmaServisi _okulServisi;
        private readonly ILogger<TenantKapsamFabrikasi> _logger;

        public TenantKapsamFabrikasi(
            IServiceScopeFactory scopeFactory,
            OkulYapilandirmaServisi okulServisi,
            ILogger<TenantKapsamFabrikasi> logger)
        {
            _scopeFactory = scopeFactory;
            _okulServisi = okulServisi;
            _logger = logger;
        }

        public IReadOnlyList<OkulBilgiAyari> Okullar => _okulServisi.TumOkullariGetir();

        /// <summary>Verilen okul için tenant'ı doldurulmuş kapsam açar; okul tanımsızsa hata verir.</summary>
        public AsyncServiceScope Olustur(string okulKodu)
        {
            if (string.IsNullOrWhiteSpace(okulKodu))
                throw new InvalidOperationException("Okul kodu belirtilmeden kapsam açılamaz.");
            var okul = _okulServisi.OkulGetir(okulKodu)
                ?? throw new InvalidOperationException($"Tanımsız okul kodu: {okulKodu}");

            var scope = _scopeFactory.CreateAsyncScope();
            var tenant = scope.ServiceProvider.GetRequiredService<TenantBaglami>();
            tenant.OkulKodu = okul.OkulKodu;
            tenant.ConnectionString = okul.ConnectionString;
            tenant.OkulAdi = okul.OkulAdi;
            return scope;
        }

        /// <summary>
        /// İşi her okul için ayrı kapsamda sırayla çalıştırır. Bir okulun hatası diğerlerini durdurmaz.
        /// </summary>
        public async Task HerOkulIcinAsync(
            string isAdi,
            Func<IServiceProvider, CancellationToken, Task> isTanimi,
            CancellationToken ct)
        {
            foreach (var okul in Okullar)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    await using var scope = Olustur(okul.OkulKodu);
                    await isTanimi(scope.ServiceProvider, ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "{Is} okul {OkulKodu} için başarısız.", isAdi, okul.OkulKodu);
                }
            }
        }
    }
}
