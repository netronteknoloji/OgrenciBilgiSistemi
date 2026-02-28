using System.Net.Http.Headers;
using System.Text.Json;

namespace StudentTrackingSystem.Services
{
    public class BaseApiService
    {
        protected readonly HttpClient _httpClient;

        // API URL Yapılandırması - HTTPS zorunlu
        protected readonly string BaseUrl;

        // JSON Serileştirme Seçenekleri
        protected readonly JsonSerializerOptions _jsonOptions;

        public BaseApiService()
        {
            // API base URL'yi yapılandırmadan oku, yoksa varsayılan HTTPS kullan
            BaseUrl = Preferences.Default.Get("ApiBaseUrl", "https://81.214.75.22:5196/api/");

            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);

            // Authorization header'ı varsa ekle
            var token = UserSession.AuthToken;
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
        protected void RefreshAuthHeader()
        {
            var token = UserSession.AuthToken;
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
        /// 401 durumunda oturumu sonlandırır.
        /// </summary>
        protected async Task<bool> HandleResponseStatus(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
                return true;

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                // Token süresi dolmuş veya geçersiz - oturumu temizle
                await UserSession.ClearSessionAsync();
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"[API HATASI] {response.StatusCode}: {errorContent}");
            return false;
        }
    }
}
