using System.Text;
using System.Text.Json;
using OgrenciBilgiSistemi.Mobil.Models;
using OgrenciBilgiSistemi.Shared.Enums;

namespace OgrenciBilgiSistemi.Mobil.Services
{
    public class GirisService : TemelApiService
    {
        public async Task<bool> KullaniciGirisYapAsync(string kullaniciAdi, string sifre)
        {
            try
            {
                // API GirisIstegiDto: KullaniciAdi ve Sifre alanlarını bekler
                var loginData = new { KullaniciAdi = kullaniciAdi, Sifre = sifre };
                var json = JsonSerializer.Serialize(loginData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{BaseUrl}kimlik-dogrulama/login", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();

                    // API'den dönen JSON: { token: "...", expiresIn: 28800, kullanici: {...} }
                    using var doc = JsonDocument.Parse(responseString);
                    var root = doc.RootElement;

                    string token = null;
                    Kullanici kullanici = null;

                    string refreshToken = null;

                    // Token varsa al
                    if (root.TryGetProperty("token", out var tokenElement))
                    {
                        token = tokenElement.GetString();
                    }

                    // Refresh token varsa al
                    if (root.TryGetProperty("refreshToken", out var refreshTokenElement))
                    {
                        refreshToken = refreshTokenElement.GetString();
                    }

                    // Kullanici nesnesini "kullanici" alanından çöz
                    if (root.TryGetProperty("kullanici", out var userElement))
                    {
                        kullanici = JsonSerializer.Deserialize<Kullanici>(userElement.GetRawText(), _jsonOptions);
                    }

                    if (kullanici != null)
                    {
                        // Oturum bilgilerini SecureStorage'a kaydet
                        string gorunenAd = !string.IsNullOrWhiteSpace(kullanici.AdSoyad)
                            ? kullanici.AdSoyad
                            : kullanici.KullaniciAdi;

                        // Veli rolünde veliId = KullaniciId (1:1 ilişki)
                        int? veliId = kullanici.Rol == KullaniciRolu.Veli ? kullanici.KullaniciId : null;

                        // Servis rolünde servisId = KullaniciId (1:1 ilişki)
                        int? servisId = kullanici.Rol == KullaniciRolu.Servis ? kullanici.KullaniciId : null;

                        await KullaniciOturum.OturumAyarlaAsync(
                            kullaniciId: kullanici.KullaniciId,
                            adSoyad: gorunenAd,
                            birimId: null,
                            rol: kullanici.Rol,
                            servisId: servisId,
                            veliId: veliId,
                            yetkiToken: token,
                            refreshToken: refreshToken
                        );

                        // HttpClient header'ını güncelle
                        YetkiBasliginiYenile();

                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[API HATASI]: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// Giriş yapmış kullanıcının şifresini değiştirir.
        /// </summary>
        public async Task<bool> SifreDegistirAsync(string yeniSifre)
        {
            try
            {
                var model = new { YeniSifre = yeniSifre };
                var response = await PostAsJsonAsync($"{BaseUrl}kimlik-dogrulama/sifre-degistir", model);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ŞİFRE DEĞİŞTİRME HATASI]: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Kullanıcı adının ilk harflerine göre eşleşen kullanıcı adlarını getirir.
        /// JWT gerektirmez (anonim endpoint).
        /// </summary>
        public async Task<List<string>> KullaniciAdiAraAsync(string aranan)
        {
            try
            {
                var response = await _httpClient.GetAsync(
                    $"{BaseUrl}kimlik-dogrulama/kullanici-ara?q={Uri.EscapeDataString(aranan)}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<string>>(json, _jsonOptions) ?? new List<string>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[KullaniciAdiAra HATASI]: {ex.Message}");
            }

            return new List<string>();
        }
    }
}
