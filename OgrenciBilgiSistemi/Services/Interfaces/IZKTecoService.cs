namespace OgrenciBilgiSistemi.Services.Interfaces
{
    public interface IZKTecoService
    {
        Task<bool> ConnectAsync();
        Task DisconnectAsync();
        bool IsConnected { get; }

        /// <summary>Bağlı cihazın ait olduğu okul; kart olayları bu okulun kapsamında işlenir.</summary>
        string? BagliOkulKodu { get; }

        /// <summary>Bağlı cihazın Id'si (BagliOkulKodu DB'sinde).</summary>
        int? CurrentCihazId { get; }

        /// <summary>
        /// ZKTeco cihazdan kart okunduğunda tetiklenen async event.
        /// </summary>
        event Func<string, Task> OnCardReadAsync;

        /// <summary>
        /// Cihaz bağlantısını izler ve yeniden bağlanma sağlar.
        /// </summary>
        Task MonitorConnectionsAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Cihaz işlemleri (log okuma/silme, kullanıcı senkronu, panel) için bağlantı verir; yalnızca ZkCihazKilidi
        /// altında çağrılır. Olay bağlantısı bu cihazdaysa aynı COM nesnesi ödünç verilir (cihaz ikinci SDK
        /// oturumunu -307 ile reddediyor), değilse geçici bağlantı açılır. Bağlanılamazsa (null, hata kodu).
        /// </summary>
        Task<(Implementations.ZkBaglanti? Baglanti, int HataKodu)> CihazBaglantisiAcAsync(string ip, int port, CancellationToken ct = default);
    }
}
