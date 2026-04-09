using System.Net.Http.Json;
using OgrenciBilgiSistemi.Mobil.Models;

namespace OgrenciBilgiSistemi.Mobil.Services
{
    /// <summary>
    /// Backend'ten en güncel mobil sürüm bilgisini çekip, mevcut sürümle karşılaştırır.
    /// Yeni sürüm varsa kullanıcıya opsiyonel güncelleme uyarısı gösterir.
    /// </summary>
    public class GuncellemeKontrolService : TemelApiService
    {
        public async Task KontrolEt()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{BaseUrl}uygulama-versiyon");
                if (!response.IsSuccessStatusCode) return;

                var bilgi = await response.Content.ReadFromJsonAsync<UygulamaVersiyonBilgi>(_jsonOptions);
                if (bilgi == null) return;

                var mevcutStr = AppInfo.Current.VersionString;
                var isIos = DeviceInfo.Current.Platform == DevicePlatform.iOS;
                var enYeniStr = isIos ? bilgi.IosLatestVersion : bilgi.AndroidLatestVersion;
                var storeUrl = isIos ? bilgi.IosStoreUrl : bilgi.AndroidStoreUrl;

                if (string.IsNullOrWhiteSpace(enYeniStr) || string.IsNullOrWhiteSpace(storeUrl))
                    return;

                if (!Version.TryParse(mevcutStr, out var mevcut)) return;
                if (!Version.TryParse(enYeniStr, out var enYeni)) return;
                if (enYeni <= mevcut) return;

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var page = Application.Current?.MainPage;
                    if (page == null) return;

                    bool guncelle = await page.DisplayAlert(
                        "Güncelleme Var",
                        $"Yeni sürüm ({enYeniStr}) yayınlandı. Şimdi güncellemek ister misiniz?",
                        "Güncelle",
                        "Sonra");

                    if (guncelle)
                    {
                        try { await Launcher.Default.OpenAsync(new Uri(storeUrl)); }
                        catch { /* store açılamazsa sessizce geç */ }
                    }
                });
            }
            catch
            {
                // Ağ yoksa veya backend yanıt vermezse kontrol sessizce atlanır
            }
        }
    }
}
