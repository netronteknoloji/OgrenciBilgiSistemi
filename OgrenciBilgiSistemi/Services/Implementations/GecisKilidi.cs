using System.Collections.Concurrent;

namespace OgrenciBilgiSistemi.Services.Implementations
{
    /// <summary>
    /// Aynı öğrencinin eşzamanlı okutmaları (okuyucunun kartı iki kez göndermesi, USB ve ZKTeco'dan
    /// aynı anda okutma) sırayla işlenir; aksi halde iki okutma da "bugün çıkış yok" görüp iki ÇIKIŞ + iki SMS yazabilir.
    /// Tek süreçli IIS app pool varsayımıyla bellek içi kilit yeterlidir.
    /// </summary>
    public static class GecisKilidi
    {
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _kilitler = new();

        public static async Task<IDisposable> AlAsync(string okulKodu, int ogrenciId, CancellationToken ct = default)
        {
            var kilit = _kilitler.GetOrAdd($"{okulKodu}:{ogrenciId}", _ => new SemaphoreSlim(1, 1));
            await kilit.WaitAsync(ct);
            return new SerbestBirakici(kilit);
        }

        private sealed class SerbestBirakici(SemaphoreSlim kilit) : IDisposable
        {
            public void Dispose() => kilit.Release();
        }
    }
}
