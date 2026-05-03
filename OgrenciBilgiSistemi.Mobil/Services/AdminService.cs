using System.Text.Json;
using OgrenciBilgiSistemi.Mobil.Models;

namespace OgrenciBilgiSistemi.Mobil.Services
{
    public class AdminService : TemelApiService
    {
        /// <summary>
        /// Yönetici ana sayfası için okulun toplam sayım ve bugünkü geçiş özetini getirir.
        /// </summary>
        public async Task<OkulOzet?> OkulOzetGetir()
        {
            try
            {
                var response = await GetAsync($"{BaseUrl}yonetici/ozet");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<OkulOzet>(json, _jsonOptions);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AdminService HATASI]: {ex.Message}");
            }

            return null;
        }
    }
}
