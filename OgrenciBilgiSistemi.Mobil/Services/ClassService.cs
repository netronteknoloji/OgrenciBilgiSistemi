using System.Net.Http.Json;
using OgrenciBilgiSistemi.Mobil.Models;
using OgrenciBilgiSistemi.Mobil.ViewModels;

namespace OgrenciBilgiSistemi.Mobil.Services
{
    public class ClassService : BaseApiService
    {
        public async Task<List<ClassroomViewModel>> GetAllClassesWithStudentCountAsync()
        {
            // Demo modunda API çağrısı yapılmaz, sahte sınıf listesi döndürülür
            if (UserSession.IsDemoMode)
                return GetDemoClasses();

            try
            {
                // API'deki 'api/Class/all-with-count' endpoint'ine istek atıyoruz
                var response = await _httpClient.GetFromJsonAsync<List<UnitWithCountDto>>($"{BaseUrl}Class/all-with-count");

                if (response != null)
                {
                    // API'den gelen DTO listesini MAUI'nin beklediği ViewModel listesine dönüştürüyoruz
                    return response.Select(dto => new ClassroomViewModel
                    {
                        ClassroomData = dto.UnitData,
                        StudentCount = dto.StudentCount
                    }).ToList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API HATASI]: Sınıf listesi çekilemedi: {ex.Message}");
            }

            return new List<ClassroomViewModel>();
        }

        /// <summary>
        /// Apple App Store incelemesi için sahte sınıf listesi döndürür.
        /// </summary>
        private List<ClassroomViewModel> GetDemoClasses()
        {
            return new List<ClassroomViewModel>
            {
                new ClassroomViewModel
                {
                    ClassroomData = new Birim { Id = -1, Name = "4-A Şubesi", IsActive = true, IsClass = true },
                    StudentCount = 5
                },
                new ClassroomViewModel
                {
                    ClassroomData = new Birim { Id = -2, Name = "5-B Şubesi", IsActive = true, IsClass = true },
                    StudentCount = 5
                }
            };
        }
    }

    // API'nin BirimOgrenciSayisiModel yapısını karşılayan DTO
    public class UnitWithCountDto
    {
        [System.Text.Json.Serialization.JsonPropertyName("birim")]
        public Birim UnitData { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("ogrenciSayisi")]
        public int StudentCount { get; set; }
    }
}
