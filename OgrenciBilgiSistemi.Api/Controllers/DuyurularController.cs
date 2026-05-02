using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OgrenciBilgiSistemi.Api.Dtos;
using OgrenciBilgiSistemi.Api.Services;

namespace OgrenciBilgiSistemi.Api.Controllers
{
    [Route("api/duyurular")]
    [ApiController]
    [Authorize]
    public class DuyurularController : ControllerBase
    {
        private readonly DuyuruService _duyuruService;

        public DuyurularController(DuyuruService duyuruService)
        {
            _duyuruService = duyuruService;
        }

        private int KullaniciId => int.Parse(User.FindFirst("kullaniciId")!.Value);
        private string Rol => User.FindFirst("rol")!.Value;

        [HttpPost]
        public async Task<IActionResult> Olustur([FromBody] DuyuruOlusturDto dto)
        {
            if (Rol != "Ogretmen") return Forbid();
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var duyuruId = await _duyuruService.OgretmenDuyuruOlustur(KullaniciId, dto.Baslik, dto.Icerik);
                return CreatedAtAction(nameof(Getir), new { id = duyuruId }, new { duyuruId });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { mesaj = ex.Message });
            }
        }

        [HttpGet("benim")]
        public async Task<IActionResult> Benim([FromQuery] int sayfaNo = 1)
        {
            if (Rol != "Veli") return Forbid();
            var liste = await _duyuruService.VeliDuyurulariGetir(KullaniciId, sayfaNo);
            return Ok(liste);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Getir(int id)
        {
            var duyuru = await _duyuruService.DuyuruGetir(id);
            if (duyuru is null) return NotFound();
            return Ok(duyuru);
        }
    }
}
