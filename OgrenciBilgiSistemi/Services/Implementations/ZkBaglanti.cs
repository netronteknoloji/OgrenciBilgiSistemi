using zkemkeeper;

namespace OgrenciBilgiSistemi.Services.Implementations
{
    /// <summary>
    /// IZKTecoService.CihazBaglantisiAcAsync'in verdiği cihaz oturumu. Kullanan kod Connect/Disconnect çağırmaz;
    /// Dispose cihazı tekrar etkinleştirir ve oturumu kapatır (olay bağlantısıysa yalnızca geri verir).
    /// </summary>
    public sealed class ZkBaglanti : IDisposable
    {
        private Action? _birak;

        public CZKEM Zk { get; }

        /// <summary>true: ZKTecoService'in kalıcı olay bağlantısı ödünç alındı; false: geçici bağlantı.</summary>
        public bool OlayBaglantisi { get; }

        internal ZkBaglanti(CZKEM zk, bool olayBaglantisi, Action birak)
        {
            Zk = zk;
            OlayBaglantisi = olayBaglantisi;
            _birak = birak;
        }

        public void Dispose() => Interlocked.Exchange(ref _birak, null)?.Invoke();
    }
}
