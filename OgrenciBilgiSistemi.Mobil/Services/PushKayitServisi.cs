using System.Net.Http.Json;
using Plugin.Firebase.CloudMessaging;
using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models;

namespace OgrenciBilgiSistemi.Mobil.Services
{
    /// <summary>
    /// FCM push token yaşam döngüsünü yönetir: izin → token al → API'ye kaydet,
    /// logout → API'den sil. TokenChanged ve NotificationReceived olaylarını dinler.
    /// </summary>
    public class PushKayitServisi : TemelApiService
    {
        private const string AnahtarFcmToken = "fcm_token";

        /// <summary>Son teşhis durumu — sorun bildiriminde cihazdan okunabilir.</summary>
        public const string AnahtarTaniDurumu = "push_tani_durumu";

        private bool _dinleyicilerBagli;

        /// <summary>
        /// Teşhis izi: Debug.WriteLine Release build'de derlenmediği için Console'a yazar
        /// (iOS Console.app / adb logcat'te görünür) ve son durumu Preferences'a kaydeder.
        /// Token değeri asla yazılmaz (yalnız uzunluk).
        /// </summary>
        private static void Tani(string mesaj)
        {
            Console.WriteLine($"[PUSH] {mesaj}");
            try
            {
                Preferences.Default.Set(AnahtarTaniDurumu, $"{DateTime.Now:HH:mm:ss} {mesaj}");
            }
            catch { /* Preferences erişilemezse teşhis akışı kırılmasın */ }
        }

        public async Task DinleyicileriBaslatAsync()
        {
            if (_dinleyicilerBagli) return;
            _dinleyicilerBagli = true;

            try
            {
                CrossFirebaseCloudMessaging.Current.TokenChanged += async (s, e) =>
                {
                    await TokenYeniledigindeAsync(e.Token);
                };

                CrossFirebaseCloudMessaging.Current.NotificationReceived += (s, e) =>
                {
                    BildirimAlindigindaIsle(e);
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PUSH] Dinleyici başlatma hatası: {ex.Message}");
            }
        }

        public async Task LoginSonrasiKaydetAsync()
        {
            Tani($"LoginSonrasiKaydet başladı, GirisYapildiMi={KullaniciOturum.GirisYapildiMi}");

            if (!KullaniciOturum.GirisYapildiMi)
            {
                Tani("GirisYapildiMi=false → skip");
                return;
            }

            try
            {
                // Bildirim izni iste (iOS'ta AppDelegate.FinishedLaunching zaten UNUserNotificationCenter
                // üzerinden istiyor; çift dialog'u önlemek için yalnız Android'de)
#if ANDROID
                var izin = await LocalNotificationCenter.Current.RequestNotificationPermission();
                Tani($"Android bildirim izni={izin}");
#endif
                await CrossFirebaseCloudMessaging.Current.CheckIfValidAsync();
                Tani("CheckIfValidAsync OK");

                var token = await CrossFirebaseCloudMessaging.Current.GetTokenAsync();
                Tani($"GetTokenAsync 1. deneme len={token?.Length ?? 0}");

                // iOS'ta APNs registration tamamlanmadan FCM token null/empty dönebilir.
                // Tek seferlik kısa retry ile race condition'ı tolere et.
                if (string.IsNullOrWhiteSpace(token))
                {
                    await Task.Delay(TimeSpan.FromSeconds(2));
                    token = await CrossFirebaseCloudMessaging.Current.GetTokenAsync();
                    Tani($"GetTokenAsync 2. deneme len={token?.Length ?? 0}");
                }

                if (string.IsNullOrWhiteSpace(token))
                {
                    Tani("token null/empty → API çağrılmadı (APNs kaydı tamamlanmamış olabilir)");
                    return;
                }

                await SecureStorage.Default.SetAsync(AnahtarFcmToken, token);
                Tani("ApiyeKaydet çağrılıyor");
                await ApiyeKaydetAsync(token);
                Tani("ApiyeKaydet tamamlandı");
            }
            catch (Exception ex)
            {
                Tani($"LoginSonrasiKaydet exception type={ex.GetType().Name} msg={ex.Message}");
            }
        }

        public async Task LogoutOncesiAsync()
        {
            try
            {
                var token = await SecureStorage.Default.GetAsync(AnahtarFcmToken);
                if (!string.IsNullOrWhiteSpace(token) && KullaniciOturum.GirisYapildiMi)
                {
                    await ApidenSilAsync(token);
                }

                // Plugin.Firebase.CloudMessaging 3.x'te DeleteToken yok; FCM tarafı token cihazda
                // kalır. Sunucuda IsDeleted=1 ile kayıt iptal edildiği için pratikte etkisi yok.
                SecureStorage.Default.Remove(AnahtarFcmToken);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PUSH] Logout öncesi temizlik hatası: {ex.Message}");
            }
        }

        private async Task TokenYeniledigindeAsync(string yeniToken)
        {
            if (!KullaniciOturum.GirisYapildiMi || string.IsNullOrWhiteSpace(yeniToken))
                return;

            try
            {
                var eskiToken = await SecureStorage.Default.GetAsync(AnahtarFcmToken);
                if (!string.IsNullOrWhiteSpace(eskiToken) && eskiToken != yeniToken)
                {
                    await ApiyeTokenYenileAsync(eskiToken, yeniToken);
                }
                else
                {
                    await ApiyeKaydetAsync(yeniToken);
                }

                await SecureStorage.Default.SetAsync(AnahtarFcmToken, yeniToken);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PUSH] Token yenileme hatası: {ex.Message}");
            }
        }

        private void BildirimAlindigindaIsle(Plugin.Firebase.CloudMessaging.EventArgs.FCMNotificationReceivedEventArgs e)
        {
            // Foreground'da Plugin.Firebase varsayılan banner göstermez; biz Plugin.LocalNotification ile gösteriyoruz.
            try
            {
                var baslik = e.Notification?.Title ?? "Bildirim";
                var govde = e.Notification?.Body ?? string.Empty;
                var veri = e.Notification?.Data ?? new Dictionary<string, string>();

                LocalNotificationCenter.Current.Show(new NotificationRequest
                {
                    NotificationId = Random.Shared.Next(1, int.MaxValue),
                    Title = baslik,
                    Description = govde,
                    ReturningData = System.Text.Json.JsonSerializer.Serialize(veri),
                    Android = new Plugin.LocalNotification.Core.Models.AndroidOption.AndroidOptions
                    {
                        ChannelId = "obs_default",
                        Priority = Plugin.LocalNotification.Core.Models.AndroidOption.AndroidPriority.High
                    }
                });

                CommunityToolkit.Mvvm.Messaging.IMessengerExtensions.Send<BildirimGeldiMesaji>(
                    CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger.Default,
                    new BildirimGeldiMesaji(veri));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PUSH] Bildirim işleme hatası: {ex.Message}");
            }
        }

        private async Task ApiyeKaydetAsync(string token)
        {
            var govde = new
            {
                FcmToken = token,
                Platform = AlgilanaPlatform(),
                UygulamaSurumu = AppInfo.Current.VersionString,
                CihazModeli = DeviceInfo.Current.Model
            };

            var response = await PostAsJsonAsync($"{BaseUrl}cihazlar/kaydet", govde);
            if (!response.IsSuccessStatusCode)
                Tani($"Cihaz kayıt hatası: {(int)response.StatusCode} {response.StatusCode}");
            else
                Tani("Cihaz API'ye kaydedildi (2xx)");
        }

        private async Task ApiyeTokenYenileAsync(string eskiToken, string yeniToken)
        {
            var govde = new
            {
                EskiToken = eskiToken,
                YeniToken = yeniToken,
                Platform = AlgilanaPlatform(),
                UygulamaSurumu = AppInfo.Current.VersionString,
                CihazModeli = DeviceInfo.Current.Model
            };

            var response = await PostAsJsonAsync($"{BaseUrl}cihazlar/token-yenile", govde);
            if (!response.IsSuccessStatusCode)
                System.Diagnostics.Debug.WriteLine($"[PUSH] Token yenileme hatası: {response.StatusCode}");
        }

        private async Task ApidenSilAsync(string token)
        {
            try
            {
                var response = await DeleteAsJsonAsync($"{BaseUrl}cihazlar/kaydi-sil", new { FcmToken = token });
                if (!response.IsSuccessStatusCode)
                    System.Diagnostics.Debug.WriteLine($"[PUSH] Cihaz silme hatası: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PUSH] Cihaz silme istisnası: {ex.Message}");
            }
        }

        private static int AlgilanaPlatform()
        {
            // PushPlatformu enum: Android=1, iOS=2
#if ANDROID
            return 1;
#elif IOS
            return 2;
#else
            return 1;
#endif
        }
    }

    public sealed record BildirimGeldiMesaji(IDictionary<string, string> Veri);
}
