using Foundation;
using OgrenciBilgiSistemi.Mobil.Services;
using UIKit;
using UserNotifications;

namespace OgrenciBilgiSistemi.Mobil
{
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate
    {
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

        public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
        {
            // Delegate'i en başta set et — Firebase init lifecycle event'i base.FinishedLaunching içinde çalışıyor
            UNUserNotificationCenter.Current.Delegate = new BildirimMerkeziDelegesi();

            var sonuc = base.FinishedLaunching(application, launchOptions);

            // Push notification kayıt akışı (UNUserNotificationCenter)
            try
            {
                UNUserNotificationCenter.Current.RequestAuthorization(
                    UNAuthorizationOptions.Alert | UNAuthorizationOptions.Sound | UNAuthorizationOptions.Badge,
                    (granted, error) =>
                    {
                        Console.WriteLine($"[PUSH] iOS bildirim izni granted={granted} error={error?.LocalizedDescription ?? "yok"}");
                        if (granted)
                        {
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                application.RegisterForRemoteNotifications();
                            });
                        }
                    });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[iOS PUSH] İzin hatası: {ex.Message}");
            }

            // Killed-state'te push tap ile başlatıldıysa data payload'ı yönlendirme servisine forward et.
            try
            {
                if (launchOptions?[UIApplication.LaunchOptionsRemoteNotificationKey] is NSDictionary push)
                {
                    var veri = PushPayloadunuCevir(push);
                    if (veri.Count > 0 && IPlatformApplication.Current?.Services.GetService(typeof(BildirimYonlendirmeServisi)) is BildirimYonlendirmeServisi yonlendirme)
                    {
                        // Shell hazır olana kadar küçük gecikme — fire & forget
                        _ = Task.Run(async () =>
                        {
                            await Task.Delay(TimeSpan.FromMilliseconds(800));
                            await yonlendirme.IsleAsync(veri);
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[iOS PUSH] Killed-state yönlendirme hatası: {ex.Message}");
            }

            return sonuc;
        }

        [Export("application:didReceiveRemoteNotification:fetchCompletionHandler:")]
        public void DidReceiveRemoteNotification(UIApplication application, NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler)
        {
            // Silent push (content-available=1) forward'u için. Plugin.Firebase iOS Messaging
            // payload'ı kendi event'ine aktarır; biz iOS'a "ben işledim" sinyali veriyoruz.
            completionHandler(UIBackgroundFetchResult.NewData);
        }

        private static Dictionary<string, string> PushPayloadunuCevir(NSDictionary payload)
        {
            var sonuc = new Dictionary<string, string>();
            foreach (var anahtar in payload.Keys)
            {
                if (anahtar is NSString k && payload[k] is NSObject v)
                {
                    var key = k.ToString();
                    if (key == "aps") continue; // sistem alanı
                    sonuc[key] = v.ToString();
                }
            }
            return sonuc;
        }

        [Export("application:didRegisterForRemoteNotificationsWithDeviceToken:")]
        public void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken)
        {
            // APNs token'ı Firebase Messaging SDK'sına aktarmak için Plugin.Firebase
            // FirebaseAppDelegateProxyEnabled=false olduğu için manuel bağlama gerekiyor.
            try
            {
                Firebase.CloudMessaging.Messaging.SharedInstance.ApnsToken = deviceToken;
                Console.WriteLine("[PUSH] APNs token Firebase'e aktarıldı");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PUSH] APNs token aktarım hatası: {ex.Message}");
            }
        }

        [Export("application:didFailToRegisterForRemoteNotificationsWithError:")]
        public void FailedToRegisterForRemoteNotifications(UIApplication application, NSError error)
        {
            Console.WriteLine($"[PUSH] APNs kayıt başarısız: {error.LocalizedDescription}");
        }
    }

    /// <summary>
    /// Foreground'da gelen push'un banner ile gösterilmesini sağlar.
    /// </summary>
    public class BildirimMerkeziDelegesi : UNUserNotificationCenterDelegate
    {
        public override void WillPresentNotification(
            UNUserNotificationCenter center,
            UNNotification notification,
            Action<UNNotificationPresentationOptions> completionHandler)
        {
            completionHandler(
                UNNotificationPresentationOptions.Banner |
                UNNotificationPresentationOptions.List |
                UNNotificationPresentationOptions.Sound);
        }
    }
}