using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OgrenciBilgiSistemi.Api.Services;

namespace OgrenciBilgiSistemi.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/siniflar")]
    public class SiniflarController : ControllerBase
    {
        private readonly SinifService _sinifService;

        public SiniflarController(SinifService sinifService)
        {
            _sinifService = sinifService;
        }

        [HttpGet("all-with-count")]
        public async Task<IActionResult> SiniflariSayiIleGetir()
        {
            try
            {
                var data = await _sinifService.TumSiniflariOgrenciSayisiIleGetirAsync();
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
