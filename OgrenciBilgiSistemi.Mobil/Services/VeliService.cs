using System.Text.Json;
using OgrenciBilgiSistemi.Mobil.Models;

namespace OgrenciBilgiSistemi.Mobil.Services
{
    public class VeliService : TemelApiService
    {
        /// <summary>
        /// Giriş yapan veliye ait öğrencileri API'den getirir.
        /// </summary>
        public async Task<List<Ogrenci>> CocuklarimiGetir()
        {
            try
            {
                var response = await GetAsync($"{BaseUrl}ogrenciler/benim");

                if (response.IsSuccessStatusCode)
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
    }
}
