using Microsoft.AspNetCore.Mvc;
using OgrenciBilgiSistemi.Api.Models;
using OgrenciBilgiSistemi.Api.Services;

namespace OgrenciBilgiSistemi.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UnitsController : ControllerBase
    {
        private readonly UnitService _unitService;

        // Dependency Injection ile UnitService'i alıyoruz
        public UnitsController(UnitService unitService)
        {
            _unitService = unitService;
        }

        // Birim ID'sine göre detayları getiren endpoint
        // GET: api/units/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUnit(int id)
        {
            try
            {
                var unit = await _unitService.GetUnitByIdAsync(id);

                if (unit == null)
                {
                    return NotFound(new { message = $"{id} numaralı birim bulunamadı." });
                }

                return Ok(unit);
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "Birim bilgisi alınırken bir hata oluştu." });
            }
        }
    }
}