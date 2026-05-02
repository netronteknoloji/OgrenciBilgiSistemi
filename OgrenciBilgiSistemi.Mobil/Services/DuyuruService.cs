using System.Text;
using System.Text.Json;
using OgrenciBilgiSistemi.Mobil.Models;

namespace OgrenciBilgiSistemi.Mobil.Services
{
    public class DuyuruService : TemelApiService
    {
        public async Task<List<Duyuru>> BenimDuyurular(int sayfaNo = 1)
        {
            var response = await GetAsync($"{BaseUrl}duyurular/benim?sayfaNo={sayfaNo}");
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                System.Diagnostics.Debug.WriteLine($"[DUYURU HATASI]: {(int)response.StatusCode} {response.StatusCode} — {body}");
                return new();
            }

            return JsonSerializer.Deserialize<List<Duyuru>>(body, _jsonOptions) ?? new();
        }

        public async Task<(bool Basarili, string? HataMesaji)> Olustur(string baslik, string icerik)
        {
            try
            {
                var body = new { baslik, icerik };
                var content = new StringContent(JsonSerializer.Serialize(body, _jsonOptions), Encoding.UTF8, "application/json");
                var response = await PostAsync($"{BaseUrl}duyurular", content);

                if (response.IsSuccessStatusCode)
                    return (true, null);

                var responseBody = await response.Content.ReadAsStringAsync();
                string? mesaj = null;
                try
                {
                    using var doc = JsonDocument.Parse(responseBody);
                    if (doc.RootElement.TryGetProperty("mesaj", out var m))
                        mesaj = m.GetString();
                }
                catch { }

                return (false, mesaj);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DUYURU HATASI]: {ex.Message}");
                return (false, null);
            }
        }
    }
}
