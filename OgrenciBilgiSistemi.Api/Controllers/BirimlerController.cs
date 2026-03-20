using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OgrenciBilgiSistemi.Api.Models;
using OgrenciBilgiSistemi.Api.Services;

namespace OgrenciBilgiSistemi.Api.Controllers
{
    [Route("api/birimler")]
    [ApiController]
    [Authorize]
    public class BirimlerController : ControllerBase
    {
        private readonly BirimService _birimService;

        public BirimlerController(BirimService birimService)
        {
            _birimService = birimService;
        }

        // GET: api/birimler/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> BirimGetir(int id)
        {
            try
            {
                var birim = await _birimService.BirimGetirAsync(id);

                if (birim == null)
                {
                    return NotFound(new { message = $"{id} numaralı birim bulunamadı." });
                }

                return Ok(birim);
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "Birim bilgisi alınırken bir hata oluştu." });
            }
        }
    }
}
