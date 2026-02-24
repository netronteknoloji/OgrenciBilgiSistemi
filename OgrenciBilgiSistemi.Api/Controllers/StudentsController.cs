using Microsoft.AspNetCore.Mvc;
using OgrenciBilgiSistemi.Api.Models;
using OgrenciBilgiSistemi.Api.Services;

namespace OgrenciBilgiSistemi.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StudentsController : ControllerBase
    {
        private readonly StudentService _studentService;

        public StudentsController(StudentService studentService)
        {
            _studentService = studentService;
        }

        #region Öğrenci Bilgi Metotları

        // 1. Sınıf ID'sine göre öğrenci listesini getirir
        [HttpGet("class/{classId}")]
        public async Task<IActionResult> GetByClass(int classId)
        {
            try
            {
                var students = await _studentService.GetStudentsByClassIdAsync(classId);
                return Ok(students);
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "Öğrenci listesi alınırken bir hata oluştu." });
            }
        }

        // 2. Öğrencinin tüm detaylarını (Veli, Servis vb.) getirir
        [HttpGet("{id}/details")]
        public async Task<IActionResult> GetDetails(int id)
        {
            try
            {
                var details = await _studentService.GetStudentFullDetailsAsync(id);
                if (details.Count == 0)
                    return NotFound(new { message = $"{id} numaralı öğrenci bulunamadı." });
                return Ok(details);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Öğrenci detayları alınırken bir hata oluştu." });
            }
        }

        #endregion

        #region Yoklama Metotları

        // 3. Mevcut yoklama durumunu getirir (Dictionary döner)
        [HttpGet("attendance/{classId}/{lessonNumber}")]
        public async Task<IActionResult> GetAttendance(int classId, int lessonNumber)
        {
            try
            {
                var attendance = await _studentService.GetExistingAttendanceAsync(classId, lessonNumber);
                return Ok(attendance);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "Yoklama bilgisi alınırken bir hata oluştu." });
            }
        }

        // 4. Haftalık yoklama geçmişini getirir
        [HttpGet("{id}/weekly-attendance")]
        public async Task<IActionResult> GetWeekly(int id, [FromQuery] DateTime start, [FromQuery] DateTime end)
        {
            if (start > end)
                return BadRequest(new { error = "Başlangıç tarihi bitiş tarihinden sonra olamaz." });

            try
            {
                var history = await _studentService.GetStudentWeeklyAttendanceAsync(id, start, end);
                return Ok(history);
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "Haftalık yoklama alınırken bir hata oluştu." });
            }
        }

        // 5. Toplu yoklama kaydetme (POST)
        [HttpPost("attendance/save-bulk")]
        public async Task<IActionResult> SaveBulkAttendance([FromBody] AttendanceUpdateModel model)
        {
            if (model.Records == null || model.Records.Count == 0)
                return BadRequest(new { error = "Yoklama kaydı listesi boş olamaz." });

            try
            {
                // API üzerinden gelen veriyi servisin beklediği Tuple formatına dönüştürüyoruz
                var formattedData = model.Records.Select(r => (r.StudentId, r.StatusId));

                await _studentService.SaveBulkAttendanceAsync(
                    formattedData,
                    model.ClassId,
                    model.TeacherId,
                    model.LessonNumber
                );

                return Ok(new { message = "Yoklama başarıyla kaydedildi." });
            }
            catch (ArgumentOutOfRangeException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "Yoklama kaydedilirken bir hata oluştu." });
            }
        }

        #endregion
    }

    #region Transfer Modelleri

    // Toplu yoklama verisini karşılamak için kullanılan model
    public class AttendanceUpdateModel
    {
        public int ClassId { get; set; }
        public int TeacherId { get; set; }
        public int LessonNumber { get; set; }
        public List<AttendanceRecordItem> Records { get; set; } = new();
    }

    // Her bir öğrencinin yoklama bilgisini temsil eden model
    public class AttendanceRecordItem
    {
        public int StudentId { get; set; }
        public int StatusId { get; set; }
    }

    #endregion
}