using System.Net.Http.Json;
using System.Text;
using Plugin.Firebase.CloudMessaging;
using Plugin.LocalNotification;

namespace OgrenciBilgiSistemi.Mobil.Services
{
    /// <summary>
    /// FCM push token yaşam döngüsünü yönetir: izin → token al → API'ye kaydet,
    /// logout → API'den sil. TokenChanged ve NotificationReceived olaylarını dinler.
    /// </summary>
    public class PushKayitServisi : TemelApiService
    {
        private const string AnahtarFcmToken = "fcm_token";

        private bool _dinleyicilerBagli;

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
            if (!KullaniciOturum.GirisYapildiMi)
                return;

            try
            {
                // Bildirim izni iste (iOS'ta AppDelegate.FinishedLaunching zaten UNUserNotificationCenter
                // üzerinden istiyor; çift dialog'u önlemek için yalnız Android'de)
#if ANDROID
                await LocalNotificationCenter.Current.RequestNotificationPermission();
#endif
                await CrossFirebaseCloudMessaging.Current.CheckIfValidAsync();

                var token = await CrossFirebaseCloudMessaging.Current.GetTokenAsync();

                // iOS'ta APNs registration tamamlanmadan FCM token null/empty dönebilir.
                // Tek seferlik kısa retry ile race condition'ı tolere et.
                if (string.IsNullOrWhiteSpace(token))
                {
                    await Task.Delay(TimeSpan.FromSeconds(2));
                    token = await CrossFirebaseCloudMessaging.Current.GetTokenAsync();
                }

                if (string.IsNullOrWhiteSpace(token))
                    return;

                await SecureStorage.Default.SetAsync(AnahtarFcmToken, token);
                await ApiyeKaydetAsync(token);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PUSH] Login sonrası kayıt hatası: {ex.Message}");
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
                    Android = new Plugin.LocalNotification.AndroidOption.AndroidOptions
                    {
                        ChannelId = "obs_default",
                        Priority = Plugin.LocalNotification.AndroidOption.AndroidPriority.High
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
                System.Diagnostics.Debug.WriteLine($"[PUSH] Cihaz kayıt hatası: {response.StatusCode}");
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
                YetkiBasliginiYenile();
                var govde = new { FcmToken = token };
                var json = System.Text.Json.JsonSerializer.Serialize(govde);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                using var istek = new HttpRequestMessage(HttpMethod.Delete, $"{BaseUrl}cihazlar/kaydi-sil")
                {
                    Content = content
                };
                using var response = await _httpClient.SendAsync(istek);
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
