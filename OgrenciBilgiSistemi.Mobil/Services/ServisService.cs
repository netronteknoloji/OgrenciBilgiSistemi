using System.Text.Json;
using OgrenciBilgiSistemi.Mobil.Models;

namespace OgrenciBilgiSistemi.Mobil.Services
{
    public class ServisService : TemelApiService
    {
        /// <summary>
        /// Belirtilen servise atanmış öğrencileri API'den getirir.
        /// </summary>
        public async Task<List<Ogrenci>> ServisOgrencileriGetir(int servisId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{BaseUrl}servisler/{servisId}/ogrenciler");

                if (await YanitDurumuIsle(response))
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var list = JsonSerializer.Deserialize<List<Ogrenci>>(json, _jsonOptions) ?? new List<Ogrenci>();
                    foreach (var o in list)
                        o.OgrenciGorsel = Constants.GorselUrl(o.OgrenciGorsel);
                    return list;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[API HATASI]: {ex.Message}");
            }

            return new List<Ogrenci>();
        }

        /// <summary>
        /// Belirtilen servisin bilgilerini API'den getirir.
        /// </summary>
        public async Task<Servis?> ServisGetir(int servisId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{BaseUrl}servisler/{servisId}");

                if (await YanitDurumuIsle(response))
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<Servis>(json, _jsonOptions);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[API HATASI]: {ex.Message}");
            }

            return null;
        }
    }
}
