using System.Text.Json;
using OgrenciBilgiSistemi.Mobil.Models;

namespace OgrenciBilgiSistemi.Mobil.Services
{
    /// <summary>
    /// Merkezi kayıt sunucusundan okul listesini çeker.
    /// TemelApiService'den bağımsızdır — kendi HttpClient'ını kullanır.
    /// Offline durumda son başarılı yanıtı cache'den döner.
    /// </summary>
    public class OkulKayitServisi
    {
        private readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(15) };

        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private const string CacheAnahtari = "CachedOkulListesi";

        /// <summary>
        /// Merkezi JSON'dan okul listesini getirir. Başarısız olursa cache'den döner.
        /// </summary>
        public async Task<List<OkulBilgi>> OkullariGetirAsync()
        {
            try
            {
                var url = Preferences.Default.Get("KayitSunucuUrl", Constants.KayitSunucuUrl);
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    Preferences.Default.Set(CacheAnahtari, json);

                    var kayit = JsonSerializer.Deserialize<OkulKayitYanit>(json, _jsonOptions);
                    return kayit?.Okullar ?? new List<OkulBilgi>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OkulKayit HATASI]: {ex.Message}");
            }

            // Ağ hatası — cache'den dön
            return CachedOkullariGetir();
        }

        private List<OkulBilgi> CachedOkullariGetir()
        {
            try
            {
                var cached = Preferences.Default.Get(CacheAnahtari, "");
                if (!string.IsNullOrEmpty(cached))
                {
                    var kayit = JsonSerializer.Deserialize<OkulKayitYanit>(cached, _jsonOptions);
                    return kayit?.Okullar ?? new List<OkulBilgi>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OkulKayit CACHE HATASI]: {ex.Message}");
            }

            return new List<OkulBilgi>();
        }
    }

    /// <summary>
    /// Merkezi okul kayıt JSON'unun deserialization modeli.
    /// </summary>
    public class OkulKayitYanit
    {
        public int Surum { get; set; }
        public List<OkulBilgi> Okullar { get; set; } = new();
    }
}
