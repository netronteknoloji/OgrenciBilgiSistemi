//using Microsoft.Extensions.Logging;
//using Microsoft.Maui.LifecycleEvents;
//using CommunityToolkit.Maui;
//using OgrenciBilgiSistemi.Mobil.Services;
//using OgrenciBilgiSistemi.Mobil.Views;
//using Plugin.LocalNotification;
//using Plugin.Firebase.CloudMessaging;
//using System.Reflection;
//using System.Text.Json;
//#if IOS
//using Plugin.Firebase.Bundled.Platforms.iOS;
//#endif

//namespace OgrenciBilgiSistemi.Mobil
//{
//    public static class MauiProgram
//    {
//        public static MauiApp CreateMauiApp()
//        {
//            // appsettings.json'dan API URL'ini oku ve Preferences'a kaydet
//            YukleApiAyarlari();

//            var builder = MauiApp.CreateBuilder();

//            builder
//                .UseMauiApp<App>()
//                .UseMauiCommunityToolkit()
//                .UseLocalNotification()
//                .RegisterFirebaseServices()
//                .ConfigureFonts(fonts =>
//                {
//                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
//                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
//                });

//            // Servis kayitlari (Dependency Injection)
//            builder.Services.AddSingleton<GirisService>();
//            builder.Services.AddSingleton<SinifService>();
//            builder.Services.AddSingleton<OgrenciService>();
//            builder.Services.AddSingleton<ServisService>();
//            builder.Services.AddSingleton<VeliService>();
//            builder.Services.AddSingleton<GuncellemeKontrolService>();
//            builder.Services.AddSingleton<OkulKayitServisi>();
//            builder.Services.AddSingleton<RandevuService>();
//            builder.Services.AddSingleton<OgretmenRandevuService>();
//            builder.Services.AddSingleton<BildirimService>();
//            builder.Services.AddSingleton<OgretmenListeService>();
//            builder.Services.AddSingleton<DuyuruService>();
//            builder.Services.AddSingleton<AdminService>();
//            builder.Services.AddSingleton<VeliListeService>();
//            builder.Services.AddSingleton<GenelAdminGirisGecisService>();
//            builder.Services.AddSingleton<PushKayitServisi>();
//            builder.Services.AddSingleton<BildirimYonlendirmeServisi>();

//            // Sayfa kayitlari
//            // GirisView ve SinifListeView Shell tarafindan DI ile cozumleniyor
//            builder.Services.AddTransient<GirisView>();
//            builder.Services.AddTransient<OkulSecimView>();
//            builder.Services.AddTransient<SinifListeView>();
//            builder.Services.AddTransient<ServisEkraniView>();
//            builder.Services.AddTransient<VeliAnaSayfaView>();
//            builder.Services.AddTransient<RandevuListeView>();
//            builder.Services.AddTransient<RandevuDetayView>();
//            builder.Services.AddTransient<RandevuOlusturView>();
//            builder.Services.AddTransient<OgretmenRandevuYonetimView>();
//            builder.Services.AddTransient<BildirimListeView>();
//            builder.Services.AddTransient<OgretmenDuyuruOlusturView>();
//            builder.Services.AddTransient<VeliDuyurularView>();
//            builder.Services.AddTransient<AdminAnaSayfaView>();
//            builder.Services.AddTransient<AdminOgrenciListeView>();
//            builder.Services.AddTransient<AdminOgretmenListeView>();
//            builder.Services.AddTransient<AdminSinifListeView>();
//            builder.Services.AddTransient<AdminVeliListeView>();
//            builder.Services.AddTransient<AdminYemekhaneBugunView>();
//            builder.Services.AddTransient<AdminAnakapiCikisBugunView>();
//            builder.Services.AddTransient<AdminServisListeView>();
//            // AdminServisDetayView constructor'da runtime parametre (ServisListeOgesi) aldigi icin DI'a kaydedilmedi.

//#if DEBUG
//            builder.Logging.AddDebug();
//#endif

//            return builder.Build();
//        }

//        /// <summary>
//        /// Plugin.Firebase platform-spesifik baslaticilarini cagirir.
//        /// iOS: CrossFirebase.Initialize() + FirebaseCloudMessagingImplementation.Initialize().
//        /// UNUserNotificationCenter delegate'i AppDelegate'te baglanir.
//        /// Android: CrossFirebase.Initialize MainApplication / MainActivity'de cagrilir.
//        /// </summary>
//        private static MauiAppBuilder RegisterFirebaseServices(this MauiAppBuilder builder)
//        {
//            builder.ConfigureLifecycleEvents(events =>
//            {
//#if IOS
//                events.AddiOS(iOS => iOS.FinishedLaunching((app, launchOptions) =>
//                {
//                    try
//                    {
//                        CrossFirebase.Initialize();
//                        FirebaseCloudMessagingImplementation.Initialize();
//                    }
//                    catch (Exception ex)
//                    {
//                        System.Diagnostics.Debug.WriteLine($"[Firebase init] {ex.Message}");
//                    }
//                    return false;
//                }));
//#endif
//            });

//            return builder;
//        }

//        /// <summary>
//        /// Gomulu appsettings.json dosyasini okur ve KayitSunucuUrl degerini Preferences'a yazar.
//        /// Merkezi okul kayit sunucusu URL'ini yapilandirir.
//        /// </summary>
//        private static void YukleApiAyarlari()
//        {
//            try
//            {
//                var assembly = Assembly.GetExecutingAssembly();
//                var kaynak = assembly.GetManifestResourceNames()
//                    .FirstOrDefault(r => r.EndsWith("appsettings.json"));

//                if (kaynak == null) return;

//                using var stream = assembly.GetManifestResourceStream(kaynak);
//                if (stream == null) return;

//                using var reader = new StreamReader(stream);
//                var json = reader.ReadToEnd();

//                var ayarlar = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
//                if (ayarlar != null && ayarlar.TryGetValue("KayitSunucuUrl", out var kayitUrl) && !string.IsNullOrWhiteSpace(kayitUrl))
//                {
//                    Preferences.Default.Set("KayitSunucuUrl", kayitUrl);
//                }
//            }
//            catch
//            {
//                // Okuma basarisiz olursa Constants.KayitSunucuUrl varsayilan olarak kullanilir
//            }
//        }
//    }
//}

using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using OgrenciBilgiSistemi.Mobil.Services;
using OgrenciBilgiSistemi.Mobil.ViewModels;
using OgrenciBilgiSistemi.Mobil.Views;
using Plugin.Firebase.Bundled.Shared;
using Plugin.Firebase.CloudMessaging;
using Plugin.LocalNotification;
using System.Reflection;
using System.Text.Json;
#if IOS
using Plugin.Firebase.Bundled.Platforms.iOS;
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

            // Servis kayitlari (Dependency Injection)
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

            // GorunumModel kayitlari (Transient — her View instance'ı kendi VM'ini alır)
            builder.Services.AddTransient<AdminAnaSayfaGorunumModel>();
            builder.Services.AddTransient<AdminOgrenciListeGorunumModel>();
            builder.Services.AddTransient<AdminSinifListeGorunumModel>();
            builder.Services.AddTransient<AdminOgretmenListeGorunumModel>();
            builder.Services.AddTransient<AdminVeliListeGorunumModel>();
            builder.Services.AddTransient<AdminServisListeGorunumModel>();
            builder.Services.AddTransient<AdminYemekhaneBugunGorunumModel>();
            builder.Services.AddTransient<AdminAnakapiCikisBugunGorunumModel>();
            builder.Services.AddTransient<AdminOgretmenDetayGorunumModel>();
            builder.Services.AddTransient<AdminVeliDetayGorunumModel>();
            builder.Services.AddTransient<AdminServisDetayGorunumModel>();
            builder.Services.AddTransient<AdminSinifOgrenciListeGorunumModel>();
            // Non-admin GorunumModel'lar
            builder.Services.AddTransient<GirisGorunumModel>();
            builder.Services.AddTransient<OkulSecimGorunumModel>();
            builder.Services.AddTransient<SinifListeGorunumModel>();
            builder.Services.AddTransient<ServisEkraniGorunumModel>();
            // OgrenciListeGorunumModel runtime parametre (birimId, birimAd) aldigi icin DI'a kaydedilmedi.
            builder.Services.AddTransient<OgretmenDuyuruOlusturGorunumModel>();
            builder.Services.AddTransient<OgretmenRandevuYonetimGorunumModel>();
            builder.Services.AddTransient<BildirimListeGorunumModel>();
            builder.Services.AddTransient<VeliDuyurularGorunumModel>();
            builder.Services.AddTransient<RandevuListeGorunumModel>();
            builder.Services.AddTransient<VeliAnaSayfaGorunumModel>();
            builder.Services.AddTransient<RandevuOlusturGorunumModel>();
            // OgrenciDetayGorunumModel runtime parametre (ogrenciId) aldigi icin DI'a kaydedilmedi.

            // Sayfa kayitlari
            // GirisView ve SinifListeView Shell tarafindan DI ile cozumleniyor
            builder.Services.AddTransient<GirisView>();
            builder.Services.AddTransient<OkulSecimView>();
            builder.Services.AddTransient<SinifListeView>();
            builder.Services.AddTransient<ServisEkraniView>();
            builder.Services.AddTransient<VeliAnaSayfaView>();
            builder.Services.AddTransient<RandevuListeView>();
            builder.Services.AddTransient<RandevuOlusturView>();
            // RandevuDetayView constructor'da runtime parametre (RandevuDetayGorunumModel - static factory) aldigi icin DI'a kaydedilmedi.
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
            // AdminServisDetayView constructor'da runtime parametre (ServisListeOgesi) aldigi icin DI'a kaydedilmedi.

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }

        /// <summary>
        /// Plugin.Firebase platform-spesifik baslaticilarini cagirir.
        /// iOS: CrossFirebase.Initialize(settings) + FirebaseCloudMessagingImplementation.Initialize().
        /// UNUserNotificationCenter delegate'i AppDelegate'te baglanir.
        /// Android: CrossFirebase.Initialize MainApplication / MainActivity'de cagrilir.
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
                        CrossFirebase.Initialize(CreateCrossFirebaseSettings());
                        FirebaseCloudMessagingImplementation.Initialize();
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
        /// Plugin.Firebase 4.x icin CrossFirebaseSettings olusturur.
        /// Yalnizca Cloud Messaging (push bildirim) etkin; diger servisler kapali.
        /// </summary>
        private static CrossFirebaseSettings CreateCrossFirebaseSettings()
        {
            return new CrossFirebaseSettings(isCloudMessagingEnabled: true);
        }

        /// <summary>
        /// Gomulu appsettings.json dosyasini okur ve KayitSunucuUrl degerini Preferences'a yazar.
        /// Merkezi okul kayit sunucusu URL'ini yapilandirir.
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
                // Okuma basarisiz olursa Constants.KayitSunucuUrl varsayilan olarak kullanilir
            }
        }
    }
}