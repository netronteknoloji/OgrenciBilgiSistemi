using System.Text;
using System.Text.Json;
using OgrenciBilgiSistemi.Mobil.Models;

namespace OgrenciBilgiSistemi.Mobil.Services
{
    public class LoginService : BaseApiService
    {
        public async Task<bool> LoginAsUserAsync(string username, string password)
        {
            try
            {
                // API GirisIstegiDto: KullaniciAdi ve Sifre alanlarını bekler
                var loginData = new { KullaniciAdi = username, Sifre = password };
                var json = JsonSerializer.Serialize(loginData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{BaseUrl}auth/login", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();

                    // API'den dönen JSON: { token: "...", expiresIn: 28800, kullanici: {...} }
                    using var doc = JsonDocument.Parse(responseString);
                    var root = doc.RootElement;

                    string token = null;
                    Kullanici user = null;

                    // Token varsa al
                    if (root.TryGetProperty("token", out var tokenElement))
                    {
                        token = tokenElement.GetString();
                    }

                    // Kullanici nesnesini "kullanici" alanından çöz
                    if (root.TryGetProperty("kullanici", out var userElement))
                    {
                        user = JsonSerializer.Deserialize<Kullanici>(userElement.GetRawText(), _jsonOptions);
                    }

                    if (user != null)
                    {
                        // Oturum bilgilerini SecureStorage'a kaydet
                        // API FullName dönüyorsa onu, yoksa Username'i kullan
                        string displayName = !string.IsNullOrWhiteSpace(user.FullName)
                            ? user.FullName
                            : user.Username;

                        await UserSession.SetSessionAsync(
                            userId: user.Id,
                            fullName: displayName,
                            unitId: user.UnitId,
                            authToken: token
                        );

                        // HttpClient header'ını güncelle
                        RefreshAuthHeader();

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
