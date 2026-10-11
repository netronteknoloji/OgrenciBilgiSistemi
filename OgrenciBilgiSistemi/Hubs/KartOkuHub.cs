using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OgrenciBilgiSistemi.Constants;
using OgrenciBilgiSistemi.Data;
using OgrenciBilgiSistemi.Infrastructure;
using OgrenciBilgiSistemi.Shared.Services;

namespace OgrenciBilgiSistemi.Hubs
{
    // Yayınlar yalnızca sunucudan (IHubContext) yapılır; istemcinin çağırabileceği metot yok.
    // Her bağlantı yalnızca kendi okulunun grubuna girer; yayınlar okul grubuna gider.
    public class KartOkuHub : Hub
    {
        private readonly OkulYapilandirmaServisi _okulServisi;
        private readonly TenantKapsamFabrikasi _tenantKapsam;

        public KartOkuHub(OkulYapilandirmaServisi okulServisi, TenantKapsamFabrikasi tenantKapsam)
        {
            _okulServisi = okulServisi;
            _tenantKapsam = tenantKapsam;
        }

        public static string OkulGrubu(string okulKodu) => $"okul:{okulKodu}";

        public override async Task OnConnectedAsync()
        {
            var http = Context.GetHttpContext();
            var user = Context.User;

            if (http is not null && user is not null)
            {
                var okulKodu = MvcTenantCozumleyiciMiddleware.OkulKoduCoz(http);
                var okul = string.IsNullOrEmpty(okulKodu) ? null : _okulServisi.OkulGetir(okulKodu);

                // Öğrenci bilgisi içeren yayınları yalnızca Yönetici, Genel Yönetici ve
                // Kart Okuma menüsü atanmış öğretmenler alır
                if (okul is not null && await YayinAlabilirMiAsync(user, okul.OkulKodu))
                    await Groups.AddToGroupAsync(Context.ConnectionId, OkulGrubu(okul.OkulKodu));
            }

            await base.OnConnectedAsync();
        }

        private async Task<bool> YayinAlabilirMiAsync(ClaimsPrincipal user, string okulKodu)
        {
            if (user.IsInRole(RolAdlari.Admin) || user.IsInRole(RolAdlari.GenelAdmin))
                return true;
            if (!user.IsInRole(RolAdlari.Ogretmen) || !int.TryParse(user.FindFirst("KullaniciId")?.Value, out var kullaniciId))
                return false;

            await using var scope = _tenantKapsam.Olustur(okulKodu);
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            return await db.KullaniciMenuOgeler.AsNoTracking()
                .AnyAsync(km => km.KullaniciId == kullaniciId && km.MenuOge.Controller == "KartOku");
        }
    }
}
