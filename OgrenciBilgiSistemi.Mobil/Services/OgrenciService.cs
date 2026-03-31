#region Kütüphane Referansları
using System.Net.Http.Json;
using OgrenciBilgiSistemi.Mobil.Models;
using OgrenciBilgiSistemi.Shared.Enums;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
#endregion

namespace OgrenciBilgiSistemi.Mobil.Services
{
    /// <summary>
    /// API üzerinden öğrenci ve yoklama işlemlerini yöneten servis.
    /// TemelApiService üzerinden HttpClient ve BaseUrl yapılandırmasını devralır.
    /// </summary>
    public class OgrenciService : TemelApiService
    {
        #region Yapıcı Metot
        public OgrenciService() : base()
        {
        }
        #endregion

        #region Öğrenci Listeleme ve Detay Metotları

        /// <summary>
        /// Belirli bir sınıfa (BirimId) ait aktif öğrencileri getirir.
        /// </summary>
        public async Task<List<Ogrenci>> SinifaGoreOgrencileriGetirAsync(int sinifId)
        {
            try
            {
                // API Ucu: GET api/ogrenciler/class/{sinifId}
                var response = await GetAsync($"{BaseUrl}ogrenciler/class/{sinifId}");
                if (!response.IsSuccessStatusCode)
                    return new List<Ogrenci>();

                var list = await response.Content.ReadFromJsonAsync<List<Ogrenci>>(_jsonOptions) ?? new List<Ogrenci>();
                foreach (var o in list)
                    o.OgrenciGorsel = Constants.GorselUrl(o.OgrenciGorsel);
                return list;
            }
            catch (Exception ex)
            {
                throw new Exception($"Öğrenci listesi çekilirken hata oluştu: {ex.Message}");
            }
        }

        /// <summary>
        /// Öğrencinin veli, servis ve sınıf gibi tüm detaylı bilgilerini getirir.
        /// </summary>
        public async Task<OgrenciDetay?> OgrenciDetayGetirAsync(int ogrenciId)
        {
            try
            {
                // API Ucu: GET api/ogrenciler/{id}/details
                var response = await GetAsync($"{BaseUrl}ogrenciler/{ogrenciId}/details");
                if (!response.IsSuccessStatusCode)
                    return null;

                return await response.Content.ReadFromJsonAsync<OgrenciDetay>(_jsonOptions);
            }
            catch (Exception ex)
            {
                throw new Exception($"Öğrenci detayları alınamadı: {ex.Message}");
            }
        }

        #endregion

        #region Yoklama İşlemleri Metotları

        /// <summary>
        /// Sınıfın ve ilgili ders saatinin bugünlük mevcut yoklama durumunu getirir.
        /// </summary>
        public async Task<Dictionary<int, int>> MevcutYoklamaGetirAsync(int sinifId, int dersNumarasi)
        {
            try
            {
                // API Ucu: GET api/ogrenciler/attendance/{sinifId}/{dersNumarasi}
                var response = await GetAsync($"{BaseUrl}ogrenciler/attendance/{sinifId}/{dersNumarasi}");
                if (!response.IsSuccessStatusCode)
                    return new Dictionary<int, int>();

                return await response.Content.ReadFromJsonAsync<Dictionary<int, int>>(_jsonOptions) ?? new Dictionary<int, int>();
            }
            catch (Exception ex)
            {
                throw new Exception($"Mevcut yoklama bilgisi alınamadı: {ex.Message}");
            }
        }

        /// <summary>
        /// Çoklu yoklama verisini API'ye göndererek veritabanına kaydeder/günceller.
        /// </summary>
        public async Task TopluYoklamaKaydetAsync(IEnumerable<(int OgrenciId, int DurumId)> yoklamaVerisi, int sinifId, int dersNumarasi)
        {
            try
            {
                // API tarafındaki TopluYoklamaGuncelleDto yapısına uygun anonim nesne oluşturuluyor
                var model = new
                {
                    SinifId = sinifId,
                    DersNumarasi = dersNumarasi,
                    Kayitlar = yoklamaVerisi.Select(a => new
                    {
                        OgrenciId = a.OgrenciId,
                        DurumId = a.DurumId
                    }).ToList()
                };

                // API Ucu: POST api/ogrenciler/attendance/save-bulk
                var response = await PostAsJsonAsync($"{BaseUrl}ogrenciler/attendance/save-bulk", model);

                if (!response.IsSuccessStatusCode)
                    throw new Exception("Yoklama kaydedilemedi.");
            }
            catch (Exception ex)
            {
                throw new Exception($"Yoklama kaydı sırasında hata oluştu: {ex.Message}");
            }
        }

        /// <summary>
        /// Belirli bir tarih aralığında öğrencinin haftalık yoklama geçmişini getirir.
        /// </summary>
        public async Task<List<SinifYoklama>> HaftalikYoklamaGetirAsync(int ogrenciId, DateTime baslangic, DateTime bitis)
        {
            try
            {
                // API Ucu: GET api/ogrenciler/{id}/weekly-attendance?baslangic=...&bitis=...
                string url = $"{BaseUrl}ogrenciler/{ogrenciId}/weekly-attendance?baslangic={baslangic:yyyy-MM-dd}&bitis={bitis:yyyy-MM-dd}";
                var response = await GetAsync(url);
                if (!response.IsSuccessStatusCode)
                    return new List<SinifYoklama>();

                return await response.Content.ReadFromJsonAsync<List<SinifYoklama>>(_jsonOptions) ?? new List<SinifYoklama>();
            }
            catch (Exception ex)
            {
                throw new Exception($"Haftalık yoklama geçmişi yüklenemedi: {ex.Message}");
            }
        }

        #endregion

    }
}
