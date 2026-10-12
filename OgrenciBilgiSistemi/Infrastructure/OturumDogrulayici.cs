using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using OgrenciBilgiSistemi.Constants;
using OgrenciBilgiSistemi.Data;
using OgrenciBilgiSistemi.Shared.Kurallar;
using OgrenciBilgiSistemi.Shared.Services;

namespace OgrenciBilgiSistemi.Infrastructure
{
    /// <summary>
    /// Panel cookie'sinin her istekte hâlâ geçerli olup olmadığını denetler: kullanıcı pasife alındıysa,
    /// rolü değiştiyse, öğretmen profili pasifse veya rolü panele kapalıysa (Veli/Servis) oturum kapatılır.
    /// DB'ye kullanıcı başına en fazla 5 dakikada bir gidilir; değişiklik en geç bu sürede etkili olur.
    /// Genel Yönetici yapılandırmadan doğrulandığı için denetlenmez.
    /// </summary>
    public static class OturumDogrulayici
    {
        private static readonly TimeSpan Onbellek = TimeSpan.FromMinutes(5);

        public static async Task DogrulaAsync(CookieValidatePrincipalContext ctx)
        {
            var user = ctx.Principal;
            if (user?.Identity?.IsAuthenticated != true || user.IsInRole(RolAdlari.GenelAdmin))
                return;

            var okulKodu = user.FindFirst("okulKodu")?.Value;
            var rol = user.FindFirst(ClaimTypes.Role)?.Value;
            if (string.IsNullOrEmpty(okulKodu) || !int.TryParse(user.FindFirst("KullaniciId")?.Value, out var kullaniciId)
                || rol is RolAdlari.Veli or RolAdlari.Servis)
            {
                await ReddetAsync(ctx);
                return;
            }

            var sp = ctx.HttpContext.RequestServices;
            string? guncelRol;
            try
            {
                guncelRol = await sp.GetRequiredService<IMemoryCache>().GetOrCreateAsync(
                    OnbellekAnahtari(okulKodu, kullaniciId),
                    async e =>
                    {
                        e.AbsoluteExpirationRelativeToNow = Onbellek;
                        await using var scope = sp.GetRequiredService<TenantKapsamFabrikasi>().Olustur(okulKodu);
                        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        var k = await db.Kullanicilar.AsNoTracking()
                            .Where(x => x.KullaniciId == kullaniciId)
                            .Select(x => new
                            {
                                x.IsDeleted,
                                x.Rol,
                                OgretmenProfilSilinmis = x.OgretmenProfil != null && x.OgretmenProfil.IsDeleted
                            })
                            .FirstOrDefaultAsync();

                        // Girişle aynı kural (KimlikDogrulamaService)
                        return k is not null
                               && PanelOturumKurallari.HesapAktifMi(k.Rol, k.IsDeleted, k.OgretmenProfilSilinmis)
                               && PanelOturumKurallari.PaneleGirebilirMi(k.Rol)
                            ? k.Rol.ToString()
                            : null;
                    });
            }
            catch (InvalidOperationException)
            {
                // Okul artık tanımlı değil
                guncelRol = null;
            }

            if (guncelRol is null || !string.Equals(guncelRol, rol, StringComparison.Ordinal))
                await ReddetAsync(ctx);
        }

        private static string OnbellekAnahtari(string okulKodu, int kullaniciId) => $"oturum:{okulKodu}:{kullaniciId}";

        /// <summary>
        /// Önbellekteki kararı siler; sonraki istek DB'den yeniden denetlenir. Başarılı girişte ve kullanıcı
        /// güncellenince/silinince çağrılır — aksi halde eski karar (eski rol, pasif) yeni oturumu da 5 dk reddeder.
        /// </summary>
        public static void OnbellegiTemizle(IMemoryCache cache, string okulKodu, int kullaniciId)
            => cache.Remove(OnbellekAnahtari(okulKodu, kullaniciId));

        private static async Task ReddetAsync(CookieValidatePrincipalContext ctx)
        {
            ctx.RejectPrincipal();
            await ctx.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}
