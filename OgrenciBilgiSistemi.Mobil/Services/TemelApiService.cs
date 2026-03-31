using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace OgrenciBilgiSistemi.Mobil.Services
{
    public class TemelApiService
    {
        // 401 alındığında ve refresh da başarısız olduğunda tüm aboneleri uyarır
        public static event Action OturumSuresiDoldu;

        protected readonly HttpClient _httpClient;

        // API URL Yapılandırması - HTTPS zorunlu
        protected readonly string BaseUrl;

        // JSON Serileştirme Seçenekleri
        protected readonly JsonSerializerOptions _jsonOptions;

        // Eş zamanlı refresh isteklerini önlemek için kilit
        private static readonly SemaphoreSlim _refreshKilidi = new(1, 1);
        private static bool _refreshDevamEdiyor;

        public TemelApiService()
        {
            // API base URL'yi yapılandırmadan oku, yoksa Constants.cs'deki değeri kullan
            BaseUrl = Preferences.Default.Get("ApiBaseUrl", Constants.ApiBaseUrl);

            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);

            // Authorization header'ı varsa ekle
            var token = KullaniciOturum.YetkiToken;
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        /// <summary>
        /// Token değiştiğinde HttpClient header'ını günceller.
        /// </summary>
        protected void YetkiBasliginiYenile()
        {
            var token = KullaniciOturum.YetkiToken;
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }
            else
            {
                _httpClient.DefaultRequestHeaders.Authorization = null;
            }
        }

        /// <summary>
        /// API haberleşmesi sırasında oluşan hataları yönetmek için ortak metot.
        /// 401 durumunda önce token yenileme dener, başarısızsa oturumu sonlandırır.
        /// </summary>
        protected async Task<bool> YanitDurumuIsle(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
                return true;

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                // Token yenilemeyi dene
                var yenilendi = await TokenYenilemeAsync();
                if (yenilendi)
                {
                    // Token yenilendi, çağıran taraf isteği tekrar denemeli
                    return false;
                }

                // Yenileme başarısız — oturumu sonlandır
                await KullaniciOturum.OturumTemizleAsync();
                OturumSuresiDoldu?.Invoke();
            }
            // 403 Forbidden: Token geçerli ama yetki yok — oturum temizlenmez,
            // çağıran servis hata fırlatarak kullanıcıya bilgi verir.

            var errorContent = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"[API HATASI] {response.StatusCode}: {errorContent}");
            return false;
        }

        /// <summary>
        /// GET isteği gönderir. Her istekten önce token'ı günceller.
        /// 401 alınırsa token yeniler ve isteği tekrar dener.
        /// </summary>
        protected async Task<HttpResponseMessage> GetAsync(string url)
        {
            YetkiBasliginiYenile();
            var response = await _httpClient.GetAsync(url);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                var yenilendi = await TokenYenilemeAsync();
                if (yenilendi)
                {
                    YetkiBasliginiYenile();
                    response = await _httpClient.GetAsync(url);
                }
                else
                {
                    await KullaniciOturum.OturumTemizleAsync();
                    OturumSuresiDoldu?.Invoke();
                }
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[API HATASI] {response.StatusCode}: {errorContent}");
            }

            return response;
        }

        /// <summary>
        /// POST isteği gönderir. Her istekten önce token'ı günceller.
        /// 401 alınırsa token yeniler ve isteği tekrar dener.
        /// </summary>
        protected async Task<HttpResponseMessage> PostAsJsonAsync<T>(string url, T data)
        {
            YetkiBasliginiYenile();
            var response = await _httpClient.PostAsJsonAsync(url, data);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                var yenilendi = await TokenYenilemeAsync();
                if (yenilendi)
                {
                    YetkiBasliginiYenile();
                    response = await _httpClient.PostAsJsonAsync(url, data);
                }
                else
                {
                    await KullaniciOturum.OturumTemizleAsync();
                    OturumSuresiDoldu?.Invoke();
                }
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[API HATASI] {response.StatusCode}: {errorContent}");
            }

            return response;
        }

        /// <summary>
        /// Refresh token kullanarak yeni access token alır.
        /// Eş zamanlı birden fazla refresh isteğini önler.
        /// </summary>
        private async Task<bool> TokenYenilemeAsync()
        {
            var refreshToken = KullaniciOturum.RefreshToken;
            if (string.IsNullOrEmpty(refreshToken))
                return false;

            await _refreshKilidi.WaitAsync();
            try
            {
                // Başka bir thread zaten refresh yaptıysa sonucu kullan
                if (_refreshDevamEdiyor)
                    return false;

                _refreshDevamEdiyor = true;

                var requestBody = new { RefreshToken = refreshToken };
                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                using var tempClient = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
                var response = await tempClient.PostAsync($"{BaseUrl}kimlik-dogrulama/refresh", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(responseString);
                    var root = doc.RootElement;

                    string yeniToken = null;
                    string yeniRefreshToken = null;

                    if (root.TryGetProperty("token", out var tokenEl))
                        yeniToken = tokenEl.GetString();

                    if (root.TryGetProperty("refreshToken", out var refreshEl))
                        yeniRefreshToken = refreshEl.GetString();

                    if (!string.IsNullOrEmpty(yeniToken) && !string.IsNullOrEmpty(yeniRefreshToken))
                    {
                        await KullaniciOturum.TokenlariGuncelleAsync(yeniToken, yeniRefreshToken);
                        YetkiBasliginiYenile();
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TOKEN YENİLEME HATASI]: {ex.Message}");
                return false;
            }
            finally
            {
                _refreshDevamEdiyor = false;
                _refreshKilidi.Release();
            }
        }
    }
}
