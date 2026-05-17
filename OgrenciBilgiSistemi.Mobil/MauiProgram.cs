using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using CommunityToolkit.Maui;
using OgrenciBilgiSistemi.Mobil.Services;
using OgrenciBilgiSistemi.Mobil.Views;
using Plugin.LocalNotification;
using System.Reflection;
using System.Text.Json;
#if IOS
using Plugin.Firebase.Core.Platforms.iOS;
#endif

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
                .UseLocalNotification()
                .RegisterFirebaseServices()
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
            builder.Services.AddSingleton<GuncellemeKontrolService>();
            builder.Services.AddSingleton<OkulKayitServisi>();
            builder.Services.AddSingleton<RandevuService>();
            builder.Services.AddSingleton<OgretmenRandevuService>();
            builder.Services.AddSingleton<BildirimService>();
            builder.Services.AddSingleton<OgretmenListeService>();
            builder.Services.AddSingleton<DuyuruService>();
            builder.Services.AddSingleton<AdminService>();
            builder.Services.AddSingleton<VeliListeService>();
            builder.Services.AddSingleton<GenelAdminGirisGecisService>();
            builder.Services.AddSingleton<PushKayitServisi>();
            builder.Services.AddSingleton<BildirimYonlendirmeServisi>();

            // Sayfa kayıtları
            // GirisView ve SinifListeView Shell tarafından DI ile çözümleniyor
            builder.Services.AddTransient<GirisView>();
            builder.Services.AddTransient<OkulSecimView>();
            builder.Services.AddTransient<SinifListeView>();
            builder.Services.AddTransient<ServisEkraniView>();
            builder.Services.AddTransient<VeliAnaSayfaView>();
            builder.Services.AddTransient<RandevuListeView>();
            builder.Services.AddTransient<RandevuDetayView>();
            builder.Services.AddTransient<RandevuOlusturView>();
            builder.Services.AddTransient<OgretmenRandevuYonetimView>();
            builder.Services.AddTransient<BildirimListeView>();
            builder.Services.AddTransient<OgretmenDuyuruOlusturView>();
            builder.Services.AddTransient<VeliDuyurularView>();
            builder.Services.AddTransient<AdminAnaSayfaView>();
            builder.Services.AddTransient<AdminOgrenciListeView>();
            builder.Services.AddTransient<AdminOgretmenListeView>();
            builder.Services.AddTransient<AdminSinifListeView>();
            builder.Services.AddTransient<AdminVeliListeView>();
            builder.Services.AddTransient<AdminYemekhaneBugunView>();
            builder.Services.AddTransient<AdminAnakapiCikisBugunView>();
            builder.Services.AddTransient<AdminServisListeView>();
            // AdminServisDetayView constructor'da runtime parametre (ServisListeOgesi) aldığı için DI'a kaydedilmedi.

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }

        /// <summary>
        /// Plugin.Firebase platform-spesifik başlatıcılarını çağırır.
        /// iOS: Firebase.Core.App.Configure() + UNUserNotificationCenter delegate'i AppDelegate'te bağlanır.
        /// Android: CrossFirebase.Initialize MainApplication'da çağrılır.
        /// </summary>
        private static MauiAppBuilder RegisterFirebaseServices(this MauiAppBuilder builder)
        {
            builder.ConfigureLifecycleEvents(events =>
            {
#if IOS
                events.AddiOS(iOS => iOS.FinishedLaunching((app, launchOptions) =>
                {
                    try
                    {
                        CrossFirebase.Initialize();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Firebase init] {ex.Message}");
                    }
                    return false;
                }));
#endif
            });

            return builder;
        }

        /// <summary>
        /// Gömülü appsettings.json dosyasını okur ve KayitSunucuUrl değerini Preferences'a yazar.
        /// Merkezi okul kayıt sunucusu URL'ini yapılandırır.
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
                if (ayarlar != null && ayarlar.TryGetValue("KayitSunucuUrl", out var kayitUrl) && !string.IsNullOrWhiteSpace(kayitUrl))
                {
                    Preferences.Default.Set("KayitSunucuUrl", kayitUrl);
                }
            }
            catch
            {
                // Okuma başarısız olursa Constants.KayitSunucuUrl varsayılan olarak kullanılır
            }
        }
    }
}
