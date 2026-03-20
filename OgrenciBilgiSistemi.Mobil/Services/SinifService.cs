using System.Net.Http.Json;
using OgrenciBilgiSistemi.Mobil.Models;
using OgrenciBilgiSistemi.Mobil.ViewModels;

namespace OgrenciBilgiSistemi.Mobil.Services
{
    public class SinifService : TemelApiService
    {
        public async Task<List<SinifGorunumModel>> TumSiniflariOgrenciSayisiIleGetirAsync()
        {
            // Demo modunda API çağrısı yapılmaz, sahte sınıf listesi döndürülür
            if (KullaniciOturum.DemoModuMu)
                return DemoSiniflariGetir();

            try
            {
                // API'deki 'api/siniflar/all-with-count' endpoint'ine istek atıyoruz
                var response = await _httpClient.GetFromJsonAsync<List<BirimOgrenciSayisiDto>>($"{BaseUrl}siniflar/all-with-count");

                if (response != null)
                {
                    // API'den gelen DTO listesini MAUI'nin beklediği ViewModel listesine dönüştürüyoruz
                    return response.Select(dto => new SinifGorunumModel
                    {
                        SinifVerisi = dto.Birim,
                        OgrenciSayisi = dto.OgrenciSayisi
                    }).ToList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API HATASI]: Sınıf listesi çekilemedi: {ex.Message}");
            }

            return new List<SinifGorunumModel>();
        }

        /// <summary>
        /// Apple App Store incelemesi için sahte sınıf listesi döndürür.
        /// </summary>
        private List<SinifGorunumModel> DemoSiniflariGetir()
        {
            return new List<SinifGorunumModel>
            {
                new SinifGorunumModel
                {
                    SinifVerisi = new Birim { BirimId = -1, BirimAd = "4-A Şubesi", BirimDurum = true, BirimSinifMi = true },
                    OgrenciSayisi = 5
                },
                new SinifGorunumModel
                {
                    SinifVerisi = new Birim { BirimId = -2, BirimAd = "5-B Şubesi", BirimDurum = true, BirimSinifMi = true },
                    OgrenciSayisi = 5
                }
            };
        }
    }

    // API'nin BirimOgrenciSayisiModel yapısını karşılayan DTO
    public class BirimOgrenciSayisiDto
    {
        public Birim Birim { get; set; }
        public int OgrenciSayisi { get; set; }
    }
}
