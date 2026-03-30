using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace OgrenciBilgiSistemi.Sms;

public static class SmsAltyapiExtensions
{
    public static IServiceCollection AddSmsAltyapisi(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SmsAyarlari>(configuration.GetSection(SmsAyarlari.SectionName));

        services.AddHttpClient<ISmsService, SmsService>((sp, client) =>
        {
            var ayar = sp.GetRequiredService<IOptions<SmsAyarlari>>().Value;
            if (!string.IsNullOrWhiteSpace(ayar.ApiUrl)
                && Uri.IsWellFormedUriString(ayar.ApiUrl, UriKind.Absolute))
                client.BaseAddress = new Uri(ayar.ApiUrl);
            client.Timeout = TimeSpan.FromSeconds(ayar.ZamanAsimiSaniye > 0 ? ayar.ZamanAsimiSaniye : 30);
        });

        return services;
    }
}
