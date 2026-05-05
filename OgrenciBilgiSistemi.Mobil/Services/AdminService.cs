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

        /// <summary>
        /// Yönetici ana sayfası için tüm servis kullanıcılarını öğrenci sayılarıyla birlikte getirir.
        /// </summary>
        public async Task<List<ServisListeOgesi>> ServisListesiGetir()
        {
            try
            {
                var response = await GetAsync($"{BaseUrl}yonetici/servisler");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<ServisListeOgesi>>(json, _jsonOptions) ?? new();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AdminService.ServisListesiGetir HATASI]: {ex.Message}");
            }

            return new();
        }

        /// <summary>
        /// Bugün yemekhaneye giriş yapan öğrencilerin listesini getirir.
        /// </summary>
        public async Task<List<YemekhaneBugunOgesi>> YemekhaneBugunGetir()
        {
            try
            {
                var response = await GetAsync($"{BaseUrl}yonetici/yemekhane-bugun");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<YemekhaneBugunOgesi>>(json, _jsonOptions) ?? new();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AdminService.YemekhaneBugunGetir HATASI]: {ex.Message}");
            }

            return new();
        }

        /// <summary>
        /// Bugün ana kapıdan çıkış yapan öğrencilerin listesini getirir.
        /// </summary>
        public async Task<List<AnakapiCikisBugunOgesi>> AnakapiCikisBugunGetir()
        {
            try
            {
                var response = await GetAsync($"{BaseUrl}yonetici/anakapi-cikis-bugun");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<AnakapiCikisBugunOgesi>>(json, _jsonOptions) ?? new();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AdminService.AnakapiCikisBugunGetir HATASI]: {ex.Message}");
            }

            return new();
        }
    }
}
