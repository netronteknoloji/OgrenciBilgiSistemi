using System.Collections.Concurrent;

namespace OgrenciBilgiSistemi.Services.Implementations
{
    /// <summary>
    /// Aynı ZKTeco cihazına aynı anda tek bağlantı: senkron işleyici, yemekhane log taraması ve panel işlemleri
    /// (kullanıcı listele/sil) sırayla bağlanır. Cihazlar eşzamanlı bağlantıda hata (ör. -8) verebiliyor ve
    /// bir işlem cihazı kapatırken (EnableDevice false) diğeri yarım kalabiliyor.
    /// ZKTecoService'in kalıcı olay bağlantısı bu kilide dahil değildir.
    /// Tek süreçli IIS app pool varsayımıyla bellek içi kilit yeterlidir.
    /// </summary>
    public static class ZkCihazKilidi
    {
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _kilitler = new();

        public static async Task<IDisposable> AlAsync(string ip, int port, CancellationToken ct = default)
        {
            var kilit = _kilitler.GetOrAdd($"{ip.Trim()}:{port}", _ => new SemaphoreSlim(1, 1));
            await kilit.WaitAsync(ct);
            return new SerbestBirakici(kilit);
        }

        private sealed class SerbestBirakici(SemaphoreSlim kilit) : IDisposable
        {
            public void Dispose() => kilit.Release();
        }
    }
}
