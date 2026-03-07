using Microsoft.AspNetCore.Mvc;
using OgrenciBilgiSistemi.Api.Dtos;
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
        [HttpGet("class/{sinifId}")]
        public async Task<IActionResult> GetByClass(int sinifId)
        {
            try
            {
                var ogrenciler = await _studentService.GetStudentsByClassIdAsync(sinifId);
                return Ok(ogrenciler);
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
                var detaylar = await _studentService.GetStudentFullDetailsAsync(id);
                if (detaylar.Count == 0)
                    return NotFound(new { message = $"{id} numaralı öğrenci bulunamadı." });
                return Ok(detaylar);
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "Öğrenci detayları alınırken bir hata oluştu." });
            }
        }

        #endregion

        #region Yoklama Metotları

        // 3. Mevcut yoklama durumunu getirir (Dictionary döner)
        [HttpGet("attendance/{sinifId}/{dersNumarasi}")]
        public async Task<IActionResult> GetAttendance(int sinifId, int dersNumarasi)
        {
            try
            {
                var yoklama = await _studentService.GetExistingAttendanceAsync(sinifId, dersNumarasi);
                return Ok(yoklama);
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
        public async Task<IActionResult> GetWeekly(int id, [FromQuery] DateTime baslangic, [FromQuery] DateTime bitis)
        {
            if (baslangic > bitis)
                return BadRequest(new { error = "Başlangıç tarihi bitiş tarihinden sonra olamaz." });

            try
            {
                var gecmis = await _studentService.GetStudentWeeklyAttendanceAsync(id, baslangic, bitis);
                return Ok(gecmis);
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "Haftalık yoklama alınırken bir hata oluştu." });
            }
        }

        // 5. Toplu yoklama kaydetme (POST)
        [HttpPost("attendance/save-bulk")]
        public async Task<IActionResult> SaveBulkAttendance([FromBody] TopluYoklamaGuncelleDto model)
        {
            if (model.Kayitlar == null || model.Kayitlar.Count == 0)
                return BadRequest(new { error = "Yoklama kaydı listesi boş olamaz." });

            try
            {
                // API üzerinden gelen veriyi servisin beklediği Tuple formatına dönüştürüyoruz
                var formatliVeri = model.Kayitlar.Select(k => (k.OgrenciId, k.DurumId));

                await _studentService.SaveBulkAttendanceAsync(
                    formatliVeri,
                    model.SinifId,
                    model.OgretmenId,
                    model.DersNumarasi
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
}
