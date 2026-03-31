using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using OgrenciBilgiSistemi.Mobil.Services;
using OgrenciBilgiSistemi.Mobil.Views;
using System.Reflection;
using System.Text.Json;

namespace OgrenciBilgiSistemi.Mobil
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            // appsettings.json'dan API URL'ini oku ve Preferences'a kaydet
            YukleApiAyarlari();

            var builder = MauiApp.CreateBuilder();

            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Servis kayıtları (Dependency Injection)
            builder.Services.AddSingleton<GirisService>();
            builder.Services.AddSingleton<SinifService>();
            builder.Services.AddSingleton<OgrenciService>();
            builder.Services.AddSingleton<ServisService>();
            builder.Services.AddSingleton<VeliService>();

            // Sayfa kayıtları
            // GirisView ve SinifListeView Shell tarafından DI ile çözümleniyor
            builder.Services.AddTransient<GirisView>();
            builder.Services.AddTransient<SinifListeView>();
            builder.Services.AddTransient<ServisEkraniView>();
            builder.Services.AddTransient<VeliAnaSayfaView>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }

        /// <summary>
        /// Gömülü appsettings.json dosyasını okur ve ApiBaseUrl değerini Preferences'a yazar.
        /// Her okul kurulumunda sadece bu JSON dosyasındaki IP değiştirilir.
        /// </summary>
        private static void YukleApiAyarlari()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var kaynak = assembly.GetManifestResourceNames()
                    .FirstOrDefault(r => r.EndsWith("appsettings.json"));

                if (kaynak == null) return;

                using var stream = assembly.GetManifestResourceStream(kaynak);
                if (stream == null) return;

                using var reader = new StreamReader(stream);
                var json = reader.ReadToEnd();

                var ayarlar = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (ayarlar != null && ayarlar.TryGetValue("ApiBaseUrl", out var apiUrl) && !string.IsNullOrWhiteSpace(apiUrl))
                {
                    Preferences.Default.Set("ApiBaseUrl", apiUrl);
                }
            }
            catch
            {
                // Okuma başarısız olursa TemelApiService'teki varsayılan URL kullanılır
            }
        }
    }
}
