#region Kütüphane Referansları
using System.Net.Http.Json;
using StudentTrackingSystem.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
#endregion

namespace StudentTrackingSystem.Services
{
    /// <summary>
    /// API üzerinden öğrenci ve yoklama işlemlerini yöneten servis.
    /// BaseApiService üzerinden HttpClient ve BaseUrl yapılandırmasını devralır.
    /// </summary>
    public class StudentService : BaseApiService
    {
        #region Yapıcı Metot
        public StudentService() : base()
        {
            // BaseApiService içerisindeki HttpClient ve Timeout ayarları burada devralınır.
        }
        #endregion

        #region Öğrenci Listeleme ve Detay Metotları

        /// <summary>
        /// Belirli bir sınıfa (BirimId) ait aktif öğrencileri getirir.
        /// </summary>
        public async Task<List<Ogrenci>> GetStudentsByClassIdAsync(int classId)
        {
            try
            {
                // API Ucu: GET api/students/class/{classId}
                var response = await _httpClient.GetFromJsonAsync<List<Ogrenci>>($"{BaseUrl}students/class/{classId}");
                return response ?? new List<Ogrenci>();
            }
            catch (Exception ex)
            {
                throw new Exception($"Öğrenci listesi çekilirken hata oluştu: {ex.Message}");
            }
        }

        /// <summary>
        /// Öğrencinin veli, servis ve sınıf gibi tüm detaylı bilgilerini getirir.
        /// </summary>
        public async Task<Dictionary<string, string>> GetStudentFullDetailsAsync(int studentId)
        {
            try
            {
                // API Ucu: GET api/students/{id}/details
                var response = await _httpClient.GetFromJsonAsync<Dictionary<string, string>>($"{BaseUrl}students/{studentId}/details");
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
        public async Task<Dictionary<int, int>> GetExistingAttendanceAsync(int classId, int lessonNumber)
        {
            try
            {
                // API Ucu: GET api/students/attendance/{classId}/{lessonNumber}
                var response = await _httpClient.GetFromJsonAsync<Dictionary<int, int>>($"{BaseUrl}students/attendance/{classId}/{lessonNumber}");
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
        public async Task SaveBulkAttendanceAsync(IEnumerable<(int StudentId, int StatusId)> attendanceData, int classId, int teacherId, int lessonNumber)
        {
            try
            {
                // API tarafındaki TopluYoklamaGuncelleDto yapısına uygun anonim nesne oluşturuluyor
                var model = new
                {
                    SinifId = classId,
                    OgretmenId = teacherId,
                    DersNumarasi = lessonNumber,
                    Kayitlar = attendanceData.Select(a => new
                    {
                        OgrenciId = a.StudentId,
                        DurumId = a.StatusId
                    }).ToList()
                };

                // API Ucu: POST api/students/attendance/save-bulk
                var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}students/attendance/save-bulk", model);

                if (!await HandleResponseStatus(response))
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
        public async Task<List<SinifYoklama>> GetStudentWeeklyAttendanceAsync(int studentId, DateTime start, DateTime end)
        {
            try
            {
                // Query String parametreleri ile istek atılıyor
                // API Ucu: GET api/students/{id}/weekly-attendance?baslangic=...&bitis=...
                string url = $"{BaseUrl}students/{studentId}/weekly-attendance?baslangic={start:yyyy-MM-dd}&bitis={end:yyyy-MM-dd}";
                var response = await _httpClient.GetFromJsonAsync<List<SinifYoklama>>(url);
                return response ?? new List<SinifYoklama>();
            }
            catch (Exception ex)
            {
                throw new Exception($"Haftalık yoklama geçmişi yüklenemedi: {ex.Message}");
            }
        }

        #endregion
    }
}