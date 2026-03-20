#region Kütüphane Referansları
using System.Net.Http.Json;
using OgrenciBilgiSistemi.Mobil.Models;
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
            // Demo modunda API çağrısı yapılmaz, sahte öğrenci listesi döndürülür
            if (KullaniciOturum.DemoModuMu)
                return DemoOgrencileriGetir(sinifId);

            try
            {
                // API Ucu: GET api/ogrenciler/class/{sinifId}
                var response = await _httpClient.GetFromJsonAsync<List<Ogrenci>>($"{BaseUrl}ogrenciler/class/{sinifId}");
                var list = response ?? new List<Ogrenci>();
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
        public async Task<Dictionary<string, string>> OgrenciDetayGetirAsync(int ogrenciId)
        {
            // Demo modunda sahte detay bilgisi döndürülür
            if (KullaniciOturum.DemoModuMu)
                return DemoOgrenciDetayGetir(ogrenciId);

            try
            {
                // API Ucu: GET api/ogrenciler/{id}/details
                var response = await _httpClient.GetFromJsonAsync<Dictionary<string, string>>($"{BaseUrl}ogrenciler/{ogrenciId}/details");
                return response ?? new Dictionary<string, string>();
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
            // Demo modunda boş yoklama döndürülür (tüm öğrenciler bekleme durumunda)
            if (KullaniciOturum.DemoModuMu)
                return new Dictionary<int, int>();

            try
            {
                // API Ucu: GET api/ogrenciler/attendance/{sinifId}/{dersNumarasi}
                var response = await _httpClient.GetFromJsonAsync<Dictionary<int, int>>($"{BaseUrl}ogrenciler/attendance/{sinifId}/{dersNumarasi}");
                return response ?? new Dictionary<int, int>();
            }
            catch (Exception ex)
            {
                throw new Exception($"Mevcut yoklama bilgisi alınamadı: {ex.Message}");
            }
        }

        /// <summary>
        /// Çoklu yoklama verisini API'ye göndererek veritabanına kaydeder/günceller.
        /// </summary>
        public async Task TopluYoklamaKaydetAsync(IEnumerable<(int OgrenciId, int DurumId)> yoklamaVerisi, int sinifId, int ogretmenId, int dersNumarasi)
        {
            // Demo modunda API'ye istek gönderilmez, sessizce başarılı sayılır
            if (KullaniciOturum.DemoModuMu)
                return;

            try
            {
                // API tarafındaki TopluYoklamaGuncelleDto yapısına uygun anonim nesne oluşturuluyor
                var model = new
                {
                    SinifId = sinifId,
                    PersonelId = ogretmenId,
                    DersNumarasi = dersNumarasi,
                    Kayitlar = yoklamaVerisi.Select(a => new
                    {
                        OgrenciId = a.OgrenciId,
                        DurumId = a.DurumId
                    }).ToList()
                };

                // API Ucu: POST api/ogrenciler/attendance/save-bulk
                var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}ogrenciler/attendance/save-bulk", model);

                if (!await YanitDurumuIsle(response))
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
            // Demo modunda boş yoklama geçmişi döndürülür
            if (KullaniciOturum.DemoModuMu)
                return new List<SinifYoklama>();

            try
            {
                // API Ucu: GET api/ogrenciler/{id}/weekly-attendance?baslangic=...&bitis=...
                string url = $"{BaseUrl}ogrenciler/{ogrenciId}/weekly-attendance?baslangic={baslangic:yyyy-MM-dd}&bitis={bitis:yyyy-MM-dd}";
                var response = await _httpClient.GetFromJsonAsync<List<SinifYoklama>>(url);
                return response ?? new List<SinifYoklama>();
            }
            catch (Exception ex)
            {
                throw new Exception($"Haftalık yoklama geçmişi yüklenemedi: {ex.Message}");
            }
        }

        #endregion

        #region Demo Modu Verileri

        private List<Ogrenci> DemoOgrencileriGetir(int sinifId)
        {
            int baseId = sinifId == -1 ? -100 : -200;
            string sinifAdi = sinifId == -1 ? "4-A Şubesi" : "5-B Şubesi";

            return new List<Ogrenci>
            {
                new Ogrenci { OgrenciId = baseId - 1, OgrenciAdSoyad = "Ahmet Yılmaz", OgrenciNo = 101, OgrenciDurum = true, BirimId = sinifId, OgrenciCikisDurumu = 0, SinifAdi = sinifAdi, VeliAdSoyad = "Mehmet Yılmaz", VeliTelefon = "0532 111 22 33" },
                new Ogrenci { OgrenciId = baseId - 2, OgrenciAdSoyad = "Ayşe Kaya", OgrenciNo = 102, OgrenciDurum = true, BirimId = sinifId, OgrenciCikisDurumu = 1, SinifAdi = sinifAdi, VeliAdSoyad = "Fatma Kaya", VeliTelefon = "0533 222 33 44" },
                new Ogrenci { OgrenciId = baseId - 3, OgrenciAdSoyad = "Mehmet Demir", OgrenciNo = 103, OgrenciDurum = true, BirimId = sinifId, OgrenciCikisDurumu = 2, SinifAdi = sinifAdi, VeliAdSoyad = "Ali Demir", VeliTelefon = "0534 333 44 55" },
                new Ogrenci { OgrenciId = baseId - 4, OgrenciAdSoyad = "Zeynep Çelik", OgrenciNo = 104, OgrenciDurum = true, BirimId = sinifId, OgrenciCikisDurumu = 0, SinifAdi = sinifAdi, VeliAdSoyad = "Hatice Çelik", VeliTelefon = "0535 444 55 66" },
                new Ogrenci { OgrenciId = baseId - 5, OgrenciAdSoyad = "Can Şahin", OgrenciNo = 105, OgrenciDurum = true, BirimId = sinifId, OgrenciCikisDurumu = 1, SinifAdi = sinifAdi, VeliAdSoyad = "Hüseyin Şahin", VeliTelefon = "0536 555 66 77" }
            };
        }

        private Dictionary<string, string> DemoOgrenciDetayGetir(int ogrenciId)
        {
            return new Dictionary<string, string>
            {
                { "ogrenciAdSoyad", "Demo Öğrenci" },
                { "birimAd", "Demo Sınıf" },
                { "veliAdSoyad", "Demo Veli" },
                { "veliTelefon", "0532 000 00 00" },
                { "servisAdi", "Demo Servis" }
            };
        }

        #endregion
    }
}
