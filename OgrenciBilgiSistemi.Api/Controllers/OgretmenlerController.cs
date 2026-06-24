using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OgrenciBilgiSistemi.Api.Services.Interfaces;

namespace OgrenciBilgiSistemi.Api.Controllers
{
    [Route("api/ogretmenler")]
    [ApiController]
    [Authorize]
    public class OgretmenlerController : ControllerBase
    {
        private readonly IOgretmenListeService _ogretmenListeService;

        public OgretmenlerController(IOgretmenListeService ogretmenListeService)
        {
            _ogretmenListeService = ogretmenListeService;
        }

        [HttpGet("aktif")]
        public async Task<IActionResult> AktifOgretmenler()
        {
            var liste = await _ogretmenListeService.AktifOgretmenleriGetir();
            return Ok(liste);
        }

        [HttpGet("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> OgretmenDetay(int id)
        {
            var detay = await _ogretmenListeService.OgretmenDetayGetirAsync(id);
            if (detay is null)
                return NotFound(new { message = $"{id} numaralı öğretmen bulunamadı." });
            return Ok(detay);
        }
    }
}
