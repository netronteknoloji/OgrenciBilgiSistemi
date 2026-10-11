namespace OgrenciBilgiSistemi.Services.Implementations
{
    /// <summary>
    /// Kuyruğa yeni iş eklendiğinde göndericiyi periyot beklemeden uyandırır (Singleton).
    /// Tetik kaçarsa gönderici yine periyodik olarak kuyruğu tarar.
    /// </summary>
    public sealed class KuyrukTetikleyici
    {
        private readonly SemaphoreSlim _sinyal = new(0, 1);

        public void Tetikle()
        {
            try { _sinyal.Release(); }
            catch (SemaphoreFullException) { /* zaten tetiklenmiş */ }
        }

        /// <summary>Tetiklenene veya süre dolana kadar bekler.</summary>
        public Task BekleAsync(TimeSpan enFazla, CancellationToken ct) => _sinyal.WaitAsync(enFazla, ct);
    }
}
