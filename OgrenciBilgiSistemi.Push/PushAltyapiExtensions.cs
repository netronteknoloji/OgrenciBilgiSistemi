using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace OgrenciBilgiSistemi.Push;

public static class PushAltyapiExtensions
{
    public static IServiceCollection AddPushAltyapisi(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PushAyarlari>(configuration.GetSection(PushAyarlari.SectionName));
        services.AddScoped<IPushBildirimGonderici, FirebasePushBildirimGonderici>();
        return services;
    }

    /// <summary>
    /// FirebaseApp.DefaultInstance'ı uygulama başlangıcında bir kez kurar.
    /// Service account JSON yolu env variable (FIREBASE_CREDENTIALS_PATH) veya
    /// Push:ServiceAccountJsonYolu config anahtarından okunur.
    /// </summary>
    public static void FirebaseUygulamasiniBaslat(IConfiguration configuration, ILogger logger)
    {
        if (FirebaseApp.DefaultInstance is not null)
            return;

        var ayarlar = new PushAyarlari();
        configuration.GetSection(PushAyarlari.SectionName).Bind(ayarlar);

        if (!ayarlar.Aktif)
        {
            logger.LogInformation("Push altyapısı pasif. FirebaseApp başlatılmadı.");
            return;
        }

        var jsonYolu = Environment.GetEnvironmentVariable("FIREBASE_CREDENTIALS_PATH")
                       ?? ayarlar.ServiceAccountJsonYolu;

        if (string.IsNullOrWhiteSpace(jsonYolu) || !File.Exists(jsonYolu))
        {
            logger.LogWarning("Firebase service account JSON bulunamadı. Push gönderimi devre dışı. Yol: {yol}",
                string.IsNullOrWhiteSpace(jsonYolu) ? "(boş)" : "(mevcut değil)");
            return;
        }

        FirebaseApp.Create(new AppOptions
        {
            Credential = GoogleCredential.FromFile(jsonYolu)
        });

        logger.LogInformation("FirebaseApp başlatıldı.");
    }
}
