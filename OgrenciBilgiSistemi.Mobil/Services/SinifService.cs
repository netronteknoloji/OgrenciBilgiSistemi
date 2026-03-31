using System.Net.Http.Json;
using OgrenciBilgiSistemi.Mobil.Models;
using OgrenciBilgiSistemi.Mobil.ViewModels;

namespace OgrenciBilgiSistemi.Mobil.Services
{
    public class SinifService : TemelApiService
    {
        public async Task<List<SinifGorunumModel>> TumSiniflariOgrenciSayisiIleGetirAsync()
        {
            try
            {
                // API'deki 'api/siniflar/all-with-count' endpoint'ine istek atıyoruz
                var response = await GetAsync($"{BaseUrl}siniflar/all-with-count");
                if (!response.IsSuccessStatusCode)
                    return new List<SinifGorunumModel>();

                var data = await response.Content.ReadFromJsonAsync<List<BirimOgrenciSayisiDto>>(_jsonOptions);

                if (data != null)
                {
                    // API'den gelen DTO listesini MAUI'nin beklediği ViewModel listesine dönüştürüyoruz
                    return data.Select(dto => new SinifGorunumModel
                    {
                        SinifVerisi = dto.Birim,
                        OgrenciSayisi = dto.OgrenciSayisi
                    }).ToList();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[API HATASI]: Sınıf listesi çekilemedi: {ex.Message}");
            }

            return new List<SinifGorunumModel>();
        }

    }

    // API'nin BirimOgrenciSayisiModel yapısını karşılayan DTO
    public class BirimOgrenciSayisiDto
    {
        public Birim Birim { get; set; }
        public int OgrenciSayisi { get; set; }
    }
}
