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
        /// Başarısızlık (HTTP hata, deserialize hatası) durumunda <c>null</c> döner;
        /// boş liste döndüğünde gerçekten kayıt yoktur.
        /// </summary>
        public async Task<List<ServisListeOgesi>?> ServisListesiGetir()
        {
            try
            {
                var response = await GetAsync($"{BaseUrl}yonetici/servisler");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<ServisListeOgesi>>(json, _jsonOptions) ?? new();
                }

                System.Diagnostics.Debug.WriteLine($"[AdminService.ServisListesiGetir] HTTP {(int)response.StatusCode} {response.StatusCode}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AdminService.ServisListesiGetir HATASI]: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Belirtilen servise atanmış aktif öğrencileri admin akışı için getirir.
        /// Başarısızlık durumunda boş liste döner; görsel URL'leri tam adrese çevrilir.
        /// </summary>
        public async Task<List<Ogrenci>> ServisOgrencileriGetir(int servisId)
        {
            try
            {
                var response = await GetAsync($"{BaseUrl}yonetici/servisler/{servisId}/ogrenciler");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var list = JsonSerializer.Deserialize<List<Ogrenci>>(json, _jsonOptions) ?? new List<Ogrenci>();
                    foreach (var o in list)
                        o.OgrenciGorsel = Constants.GorselUrl(o.OgrenciGorsel);
                    return list;
                }

                System.Diagnostics.Debug.WriteLine($"[AdminService.ServisOgrencileriGetir] HTTP {(int)response.StatusCode} {response.StatusCode}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AdminService.ServisOgrencileriGetir HATASI]: {ex.Message}");
            }

            return new List<Ogrenci>();
        }

        /// <summary>
        /// Bugün yemekhaneye giriş yapan öğrencilerin listesini getirir.
        /// Başarısızlık (HTTP hata, deserialize hatası) durumunda <c>null</c> döner;
        /// boş liste döndüğünde gerçekten kayıt yoktur.
        /// </summary>
        public async Task<List<YemekhaneBugunOgesi>?> YemekhaneBugunGetir()
        {
            try
            {
                var response = await GetAsync($"{BaseUrl}yonetici/yemekhane-bugun");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<YemekhaneBugunOgesi>>(json, _jsonOptions) ?? new();
                }

                System.Diagnostics.Debug.WriteLine($"[AdminService.YemekhaneBugunGetir] HTTP {(int)response.StatusCode} {response.StatusCode}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AdminService.YemekhaneBugunGetir HATASI]: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Bugün ana kapıdan çıkış yapan öğrencilerin listesini getirir.
        /// Başarısızlık (HTTP hata, deserialize hatası) durumunda <c>null</c> döner;
        /// boş liste döndüğünde gerçekten kayıt yoktur.
        /// </summary>
        public async Task<List<AnakapiCikisBugunOgesi>?> AnakapiCikisBugunGetir()
        {
            try
            {
                var response = await GetAsync($"{BaseUrl}yonetici/anakapi-cikis-bugun");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<AnakapiCikisBugunOgesi>>(json, _jsonOptions) ?? new();
                }

                System.Diagnostics.Debug.WriteLine($"[AdminService.AnakapiCikisBugunGetir] HTTP {(int)response.StatusCode} {response.StatusCode}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AdminService.AnakapiCikisBugunGetir HATASI]: {ex.Message}");
            }

            return null;
        }
    }
}
