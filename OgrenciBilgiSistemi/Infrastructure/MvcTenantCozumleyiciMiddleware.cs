using OgrenciBilgiSistemi.Shared.Services;

namespace OgrenciBilgiSistemi.Infrastructure
{
    public class MvcTenantCozumleyiciMiddleware
    {
        private readonly RequestDelegate _next;

        public MvcTenantCozumleyiciMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(
            HttpContext context,
            TenantBaglami tenantBaglami,
            OkulYapilandirmaServisi okulServisi)
        {
            if (!(context.User.Identity?.IsAuthenticated ?? false))
            {
                await _next(context);
                return;
            }

            var okulKodu = OkulKoduCoz(context);

            if (!string.IsNullOrEmpty(okulKodu))
            {
                var okul = okulServisi.OkulGetir(okulKodu);
                if (okul is not null)
                {
                    tenantBaglami.OkulKodu = okul.OkulKodu;
                    tenantBaglami.ConnectionString = okul.ConnectionString;
                    tenantBaglami.OkulAdi = okul.OkulAdi;
                }
            }

            await _next(context);
        }

        /// <summary>
        /// İsteğin okul kodu: Genel Admin için session'daki seçili okul, diğerleri için cookie claim'i.
        /// SignalR hub bağlantısı da aynı kuralı kullanır.
        /// </summary>
        public static string? OkulKoduCoz(HttpContext context)
            => context.User.IsInRole("GenelAdmin")
                ? context.Session.GetString("SeciliOkulKodu")
                : context.User.FindFirst("okulKodu")?.Value;
    }
}
