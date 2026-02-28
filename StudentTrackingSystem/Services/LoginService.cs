using System.Text;
using System.Text.Json;
using StudentTrackingSystem.Models;

namespace StudentTrackingSystem.Services
{
    public class LoginService : BaseApiService
    {
        public async Task<bool> LoginAsUserAsync(string username, string password)
        {
            try
            {
                var loginData = new { Username = username, Password = password };
                var json = JsonSerializer.Serialize(loginData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{BaseUrl}auth/login", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();

                    // API'den dönen JSON: { user: {...}, token: "..." }
                    // Eğer API henüz token döndürmüyorsa, User nesnesini de destekler
                    using var doc = JsonDocument.Parse(responseString);
                    var root = doc.RootElement;

                    string token = null;
                    User user = null;

                    // Token varsa al
                    if (root.TryGetProperty("token", out var tokenElement))
                    {
                        token = tokenElement.GetString();
                    }

                    // User nesnesini çöz (iç içe "user" alanı veya doğrudan root)
                    if (root.TryGetProperty("user", out var userElement))
                    {
                        user = JsonSerializer.Deserialize<User>(userElement.GetRawText(), _jsonOptions);
                    }
                    else
                    {
                        // API doğrudan User nesnesi dönüyorsa (geriye uyumluluk)
                        user = JsonSerializer.Deserialize<User>(responseString, _jsonOptions);
                    }

                    if (user != null)
                    {
                        // Oturum bilgilerini SecureStorage'a kaydet
                        await UserSession.SetSessionAsync(
                            userId: user.Id,
                            fullName: user.Username,
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
