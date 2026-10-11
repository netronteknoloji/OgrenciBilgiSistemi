using Microsoft.AspNetCore.SignalR;

namespace OgrenciBilgiSistemi.Hubs
{
    public static class KartOkumaYayini
    {
        /// <summary>
        /// Okutma sonucunu okulun kart okuma ekranlarına gönderir. Yavaş/ölü bir istemci kart akışını
        /// bloklamasın diye kısa bir zaman aşımıyla sarılır.
        /// </summary>
        public static async Task GonderAsync<T>(
            IHubContext<KartOkuHub> hub, string okulKodu, T dto, CancellationToken ct, int timeoutMs = 3000)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(timeoutMs);
            try
            {
                await hub.Clients.Group(KartOkuHub.OkulGrubu(okulKodu)).SendAsync("OgrenciBilgisiAl", dto, cts.Token);
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested && !ct.IsCancellationRequested)
            {
                // Sessiz: client cevap vermedi, kart akışını bloklamayalım
            }
        }
    }
}
