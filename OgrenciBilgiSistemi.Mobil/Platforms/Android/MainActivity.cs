using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using OgrenciBilgiSistemi.Mobil.Services;

namespace OgrenciBilgiSistemi.Mobil
{
    [Activity(
        Theme = "@style/Maui.SplashTheme",
        MainLauncher = true,
        LaunchMode = LaunchMode.SingleTop,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density,
        ScreenOrientation = ScreenOrientation.Portrait)] // Uygulamayı dikey moda sabitler
    public class MainActivity : MauiAppCompatActivity
    {
        // FCM data payload'ında bu anahtarlar varsa intent'i push tap'i olarak değerlendir.
        private static readonly string[] PushAnahtarlari = { "okulKodu", "tur", "randevuId" };

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Plugin.Firebase Android başlatma — google-services.json'ı okur, FCM'i hazırlar
            try
            {
                Plugin.Firebase.Core.Platforms.Android.CrossFirebase.Initialize(this);
            }
            catch (Exception ex)
            {
                Android.Util.Log.Warn("OBS-Push", $"Firebase init hatası: {ex.Message}");
            }

            // Uygulamanın en üstteki durum çubuğuyla (StatusBar) bütünleşmesini sağlar
            if (Window != null)
            {
                // İçeriği durum çubuğunun altına kaydırır
                Window.SetFlags(WindowManagerFlags.LayoutNoLimits, WindowManagerFlags.LayoutNoLimits);

                // Durum çubuğunu tamamen şeffaf yapar
                Window.SetStatusBarColor(Android.Graphics.Color.Transparent);
            }

            // Killed state: uygulama tamamen kapalıyken FCM bildirimine tıklanınca launcher
            // intent'i extras içinde data payload'ı taşır. Shell henüz hazır değil — gecikmeli işle.
            IslePushIntenti(Intent, gecikmeMs: 1500);
        }

        protected override void OnNewIntent(Intent? intent)
        {
            base.OnNewIntent(intent);
            if (intent != null) Intent = intent;

            // Background state: uygulama ayakta + arka planda iken bildirime tıklanınca buraya düşer.
            // Shell hazır olduğu için gecikmeye gerek yok.
            IslePushIntenti(intent, gecikmeMs: 0);
        }

        private static void IslePushIntenti(Intent? intent, int gecikmeMs)
        {
            try
            {
                var extras = intent?.Extras;
                if (extras == null) return;

                var veri = new Dictionary<string, string>();
                foreach (var anahtar in extras.KeySet() ?? Enumerable.Empty<string>())
                {
                    var deger = extras.GetString(anahtar);
                    if (!string.IsNullOrEmpty(deger))
                        veri[anahtar] = deger;
                }

                // Push ile alakasız launcher intent'lerini (normal app açılışı) yut
                if (!PushAnahtarlari.Any(k => veri.ContainsKey(k))) return;

                _ = Task.Run(async () =>
                {
                    try
                    {
                        if (gecikmeMs > 0) await Task.Delay(gecikmeMs);
                        var yonlendirme = IPlatformApplication.Current?.Services
                            .GetService<BildirimYonlendirmeServisi>();
                        if (yonlendirme != null)
                            await yonlendirme.IsleAsync(veri);
                    }
                    catch (Exception ex)
                    {
                        Android.Util.Log.Warn("OBS-Push", $"Intent yönlendirme hatası: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Android.Util.Log.Warn("OBS-Push", $"Intent parse hatası: {ex.Message}");
            }
        }
    }
}