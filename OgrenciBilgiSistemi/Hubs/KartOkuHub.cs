using Microsoft.AspNetCore.SignalR;
using OgrenciBilgiSistemi.Dtos;
using OgrenciBilgiSistemi.Services.Interfaces;

namespace OgrenciBilgiSistemi.Hubs
{
    public class KartOkuHub : Hub
    {
        public async Task KartOkunduBildir(OgrenciBilgisiDto dto, CancellationToken ct)
            => await Clients.All.SendAsync("OgrenciBilgisiAl", dto, ct);
    }
}
