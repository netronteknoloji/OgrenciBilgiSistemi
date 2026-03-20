using System.Net.Http.Headers;
using System.Text.Json;

namespace OgrenciBilgiSistemi.Mobil.Services
{
    public class TemelApiService
    {
        // 401 alındığında tüm aboneleri uyarır (App.xaml.cs login'e yönlendirir)
        public static event Action OturumSuresiDoldu;

        protected readonly HttpClient _httpClient;

        // API URL Yapılandırması - HTTPS zorunlu
        protected readonly string BaseUrl;

        // JSON Serileştirme Seçenekleri
        protected readonly JsonSerializerOptions _jsonOptions;

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
        /// 401 durumunda oturumu sonlandırır.
        /// </summary>
        protected async Task<bool> YanitDurumuIsle(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
                return true;

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                // Token süresi dolmuş veya geçersiz - oturumu temizle ve login'e yönlendir
                await KullaniciOturum.OturumTemizleAsync();
                OturumSuresiDoldu?.Invoke();
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"[API HATASI] {response.StatusCode}: {errorContent}");
            return false;
        }
    }
}
