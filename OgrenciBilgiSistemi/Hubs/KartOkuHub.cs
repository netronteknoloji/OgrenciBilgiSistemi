using Microsoft.AspNetCore.SignalR;
using OgrenciBilgiSistemi.Constants;
using OgrenciBilgiSistemi.Infrastructure;
using OgrenciBilgiSistemi.Shared.Services;

namespace OgrenciBilgiSistemi.Hubs
{
    // Yayınlar yalnızca sunucudan (IHubContext) yapılır; istemcinin çağırabileceği metot yok.
    // Her bağlantı yalnızca kendi okulunun grubuna girer; yayınlar okul grubuna gider.
    public class KartOkuHub : Hub
    {
        private readonly OkulYapilandirmaServisi _okulServisi;

        public KartOkuHub(OkulYapilandirmaServisi okulServisi) => _okulServisi = okulServisi;

        public static string OkulGrubu(string okulKodu) => $"okul:{okulKodu}";

        public override async Task OnConnectedAsync()
        {
            var http = Context.GetHttpContext();
            var user = Context.User;

            // Veli ve servis hesapları kart okuma yayınlarını (öğrenci bilgisi) almaz
            if (http is not null && user is not null
                && !user.IsInRole(RolAdlari.Veli) && !user.IsInRole(RolAdlari.Servis))
            {
                var okulKodu = MvcTenantCozumleyiciMiddleware.OkulKoduCoz(http);
                var okul = string.IsNullOrEmpty(okulKodu) ? null : _okulServisi.OkulGetir(okulKodu);
                if (okul is not null)
                    await Groups.AddToGroupAsync(Context.ConnectionId, OkulGrubu(okul.OkulKodu));
            }

            await base.OnConnectedAsync();
        }
    }
}
