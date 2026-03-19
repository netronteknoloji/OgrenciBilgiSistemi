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
            // Demo modunda API çağrısı yapılmaz, sahte öğrenci listesi döndürülür
            if (UserSession.IsDemoMode)
                return GetDemoStudents(classId);

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
            // Demo modunda sahte detay bilgisi döndürülür
            if (UserSession.IsDemoMode)
                return GetDemoStudentDetails(studentId);

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
            // Demo modunda boş yoklama döndürülür (tüm öğrenciler bekleme durumunda)
            if (UserSession.IsDemoMode)
                return new Dictionary<int, int>();

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
            // Demo modunda API'ye istek gönderilmez, sessizce başarılı sayılır
            if (UserSession.IsDemoMode)
                return;

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
            // Demo modunda boş yoklama geçmişi döndürülür
            if (UserSession.IsDemoMode)
                return new List<SinifYoklama>();

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

        #region Demo Modu Verileri

        /// <summary>
        /// Apple App Store incelemesi için sahte öğrenci listesi döndürür.
        /// </summary>
        private List<Ogrenci> GetDemoStudents(int classId)
        {
            int baseId = classId == -1 ? -100 : -200;
            string sinifAdi = classId == -1 ? "4-A Şubesi" : "5-B Şubesi";

            return new List<Ogrenci>
            {
                new Ogrenci { Id = baseId - 1, FullName = "Ahmet Yılmaz", StudentNumber = 101, IsActive = true, UnitId = classId, ExitStatus = 0, ClassName = sinifAdi, ParentFullName = "Mehmet Yılmaz", ParentPhoneNumber = "0532 111 22 33" },
                new Ogrenci { Id = baseId - 2, FullName = "Ayşe Kaya", StudentNumber = 102, IsActive = true, UnitId = classId, ExitStatus = 1, ClassName = sinifAdi, ParentFullName = "Fatma Kaya", ParentPhoneNumber = "0533 222 33 44" },
                new Ogrenci { Id = baseId - 3, FullName = "Mehmet Demir", StudentNumber = 103, IsActive = true, UnitId = classId, ExitStatus = 2, ClassName = sinifAdi, ParentFullName = "Ali Demir", ParentPhoneNumber = "0534 333 44 55" },
                new Ogrenci { Id = baseId - 4, FullName = "Zeynep Çelik", StudentNumber = 104, IsActive = true, UnitId = classId, ExitStatus = 0, ClassName = sinifAdi, ParentFullName = "Hatice Çelik", ParentPhoneNumber = "0535 444 55 66" },
                new Ogrenci { Id = baseId - 5, FullName = "Can Şahin", StudentNumber = 105, IsActive = true, UnitId = classId, ExitStatus = 1, ClassName = sinifAdi, ParentFullName = "Hüseyin Şahin", ParentPhoneNumber = "0536 555 66 77" }
            };
        }

        /// <summary>
        /// Apple App Store incelemesi için sahte öğrenci detay bilgisi döndürür.
        /// </summary>
        private Dictionary<string, string> GetDemoStudentDetails(int studentId)
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