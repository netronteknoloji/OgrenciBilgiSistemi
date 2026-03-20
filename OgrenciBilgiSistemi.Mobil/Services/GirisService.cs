using System.Text;
using System.Text.Json;
using OgrenciBilgiSistemi.Mobil.Models;

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

                    // Token varsa al
                    if (root.TryGetProperty("token", out var tokenElement))
                    {
                        token = tokenElement.GetString();
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

                        await KullaniciOturum.OturumAyarlaAsync(
                            kullaniciId: kullanici.KullaniciId,
                            adSoyad: gorunenAd,
                            birimId: kullanici.BirimId,
                            rol: kullanici.Rol,
                            servisId: kullanici.ServisId,
                            yetkiToken: token
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
    }
}
