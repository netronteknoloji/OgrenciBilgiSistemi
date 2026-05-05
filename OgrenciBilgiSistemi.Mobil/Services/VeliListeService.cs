using System.Text.Json;
using OgrenciBilgiSistemi.Mobil.Models;

namespace OgrenciBilgiSistemi.Mobil.Services
{
    public class VeliListeService : TemelApiService
    {
        public async Task<List<Veli>> AktifVelileriGetir()
        {
            try
            {
                var response = await GetAsync($"{BaseUrl}yonetici/veliler");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<Veli>>(json, _jsonOptions) ?? new();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[VELI LISTE HATASI]: {ex.Message}");
            }
            return new();
        }

        public async Task<VeliDetay?> VeliDetayGetir(int kullaniciId)
        {
            try
            {
                var response = await GetAsync($"{BaseUrl}yonetici/veliler/{kullaniciId}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<VeliDetay>(json, _jsonOptions);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[VELI DETAY HATASI]: {ex.Message}");
            }
            return null;
        }
    }
}
