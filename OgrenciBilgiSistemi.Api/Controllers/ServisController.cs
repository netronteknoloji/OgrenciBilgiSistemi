using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OgrenciBilgiSistemi.Api.Services;

namespace OgrenciBilgiSistemi.Api.Controllers
{
    [ApiController]
    [Route("api/servisler")]
    [Authorize]
    public class ServisController : ControllerBase
    {
        private readonly ServisService _servisService;

        public ServisController(ServisService servisService)
        {
            _servisService = servisService;
        }

        /// <summary>
        /// Belirtilen servise atanmış öğrencileri getirir.
        /// </summary>
        [HttpGet("{servisId}/ogrenciler")]
        public async Task<IActionResult> ServisOgrencileriGetir(int servisId)
        {
            var ogrenciler = await _servisService.ServisOgrencileriGetir(servisId);
            return Ok(ogrenciler);
        }

        /// <summary>
        /// Belirtilen servisin bilgilerini getirir.
        /// </summary>
        [HttpGet("{servisId}")]
        public async Task<IActionResult> ServisGetir(int servisId)
        {
            var servis = await _servisService.ServisGetir(servisId);
            if (servis == null)
                return NotFound("Servis bulunamadı.");

            return Ok(servis);
        }
    }
}
