using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OgrenciBilgiSistemi.Api.Dtos;
using OgrenciBilgiSistemi.Api.Services;

namespace OgrenciBilgiSistemi.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
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

        // 2. ID'ye göre tek öğrenci getirir
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var ogrenci = await _studentService.GetStudentByIdAsync(id);
                if (ogrenci is null)
                    return NotFound(new { message = $"{id} numaralı öğrenci bulunamadı." });
                return Ok(ogrenci);
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "Öğrenci bilgisi alınırken bir hata oluştu." });
            }
        }

        // 3. Öğrencinin tüm detaylarını (Veli, Servis vb.) getirir
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

        #region Öğrenci CRUD (Admin Only)

        // 4. Yeni öğrenci ekler — yalnızca admin
        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Ekle([FromBody] OgrenciKaydetDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.OgrenciAdSoyad))
                return BadRequest(new { error = "OgrenciAdSoyad boş olamaz." });

            if (dto.OgrenciNo <= 0)
                return BadRequest(new { error = "OgrenciNo sıfırdan büyük olmalıdır." });

            try
            {
                int yeniId = await _studentService.EkleAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = yeniId }, new { ogrenciId = yeniId });
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "Öğrenci eklenirken bir hata oluştu." });
            }
        }

        // 5. Mevcut öğrenciyi günceller — yalnızca admin
        [HttpPut("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Guncelle(int id, [FromBody] OgrenciKaydetDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.OgrenciAdSoyad))
                return BadRequest(new { error = "OgrenciAdSoyad boş olamaz." });

            if (dto.OgrenciNo <= 0)
                return BadRequest(new { error = "OgrenciNo sıfırdan büyük olmalıdır." });

            try
            {
                bool basarili = await _studentService.GuncelleAsync(id, dto);
                if (!basarili)
                    return NotFound(new { message = $"{id} numaralı öğrenci bulunamadı." });
                return Ok(new { message = "Öğrenci başarıyla güncellendi." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "Öğrenci güncellenirken bir hata oluştu." });
            }
        }

        // 6. Öğrenciyi pasife alır (soft-delete) — yalnızca admin
        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Sil(int id)
        {
            try
            {
                bool basarili = await _studentService.SilAsync(id);
                if (!basarili)
                    return NotFound(new { message = $"{id} numaralı öğrenci bulunamadı." });
                return Ok(new { message = "Öğrenci pasife alındı." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "Öğrenci silinirken bir hata oluştu." });
            }
        }

        #endregion

        #region Yoklama Metotları

        // 7. Mevcut yoklama durumunu getirir (Dictionary döner)
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

        // 8. Haftalık yoklama geçmişini getirir
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

        // 9. Toplu yoklama kaydetme (POST)
        [HttpPost("attendance/save-bulk")]
        public async Task<IActionResult> SaveBulkAttendance([FromBody] TopluYoklamaGuncelleDto model)
        {
            if (model.Kayitlar == null || model.Kayitlar.Count == 0)
                return BadRequest(new { error = "Yoklama kaydı listesi boş olamaz." });

            try
            {
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
