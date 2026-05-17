using Plugin.Firebase.CloudMessaging;

namespace OgrenciBilgiSistemi.Mobil.Services
{
    /// <summary>
    /// Push notification tap olayını yakalar ve data payload'a göre uygun View'a yönlendirir.
    /// </summary>
    public class BildirimYonlendirmeServisi
    {
        private bool _bagli;

        public void DinleyiciyiBaglat()
        {
            if (_bagli) return;
            _bagli = true;

            try
            {
                CrossFirebaseCloudMessaging.Current.NotificationTapped += async (s, e) =>
                {
                    await YonlendirAsync(e.Notification?.Data ?? new Dictionary<string, string>());
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[YÖNLENDIRME] Dinleyici hatası: {ex.Message}");
            }
        }

        // Killed/background state'te platform katmanından (örn. Android MainActivity launcher intent)
        // doğrudan çağrılabilmesi için public wrapper.
        public Task IsleAsync(IDictionary<string, string> veri) => YonlendirAsync(veri);

        private async Task YonlendirAsync(IDictionary<string, string> veri)
        {
            try
            {
                if (!KullaniciOturum.GirisYapildiMi)
                    return;

                // Yanlış okuldan gelen push'u sessizce yut
                if (veri.TryGetValue("okulKodu", out var okulKodu)
                    && !string.IsNullOrWhiteSpace(okulKodu)
                    && !string.IsNullOrWhiteSpace(KullaniciOturum.OkulKodu)
                    && !string.Equals(okulKodu, KullaniciOturum.OkulKodu, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                veri.TryGetValue("tur", out var turStr);
                veri.TryGetValue("randevuId", out var randevuIdStr);

                int.TryParse(turStr, out var tur);
                int.TryParse(randevuIdStr, out var randevuId);

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var shell = Shell.Current;
                    if (shell is null) return;

                    // Tur 1-5: Randevu, 6: Duyuru
                    if (tur >= 1 && tur <= 5 && randevuId > 0)
                    {
                        await shell.GoToAsync($"//RandevuDetayView?randevuId={randevuId}");
                    }
                    else
                    {
                        await shell.GoToAsync("//BildirimListeView");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[YÖNLENDIRME] Yönlendirme hatası: {ex.Message}");
            }
        }
    }
}
