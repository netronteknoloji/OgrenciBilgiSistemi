using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OgrenciBilgiSistemi.Api.Services;

namespace OgrenciBilgiSistemi.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/gecis-kayit")]
    public class GecisKayitController : ControllerBase
    {
        private readonly GecisKayitService _gecisKayitService;

        public GecisKayitController(GecisKayitService gecisKayitService)
        {
            _gecisKayitService = gecisKayitService;
        }

        // GET: api/gecis?baslangic=2026-01-01&bitis=2026-03-07&arama=ali&sinifId=3
        [HttpGet]
        public async Task<IActionResult> ListeGetir(
            [FromQuery] DateTime? baslangic,
            [FromQuery] DateTime? bitis,
            [FromQuery] string?   arama,
            [FromQuery] int?      sinifId)
        {
            if (baslangic.HasValue && bitis.HasValue && baslangic > bitis)
                return BadRequest(new { error = "Başlangıç tarihi bitiş tarihinden sonra olamaz." });

            try
            {
                var kayitlar = await _gecisKayitService.GetListAsync(baslangic, bitis, arama, sinifId);
                return Ok(kayitlar);
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "Giriş/çıkış kayıtları alınırken bir hata oluştu." });
            }
        }

        // GET: api/gecis/{ogrenciId}
        [HttpGet("{ogrenciId}")]
        public async Task<IActionResult> OgrenciyeGoreGetir(int ogrenciId)
        {
            try
            {
                var kayitlar = await _gecisKayitService.GetByOgrenciIdAsync(ogrenciId);
                return Ok(kayitlar);
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "Öğrenci giriş/çıkış kayıtları alınırken bir hata oluştu." });
            }
        }
    }
}
