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
    }
}
