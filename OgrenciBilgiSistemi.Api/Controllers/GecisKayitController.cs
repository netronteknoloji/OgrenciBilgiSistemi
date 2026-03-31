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
        private readonly OgrenciService _ogrenciService;

        public GecisKayitController(GecisKayitService gecisKayitService, OgrenciService ogrenciService)
        {
            _gecisKayitService = gecisKayitService;
            _ogrenciService = ogrenciService;
        }

        // GET: api/gecis-kayit?baslangic=2026-01-01&bitis=2026-03-07&arama=ali&sinifId=3
        // Rol bazlı: Veli sadece kendi çocuklarını, Servis kendi servis öğrencilerini görür
        [HttpGet]
        public async Task<IActionResult> ListeGetir(
            [FromQuery] DateTime? baslangic,
            [FromQuery] DateTime? bitis,
            [FromQuery] string?   arama,
            [FromQuery] int?      sinifId)
        {
            if (baslangic.HasValue && bitis.HasValue && baslangic > bitis)
                return BadRequest(new { error = "Başlangıç tarihi bitiş tarihinden sonra olamaz." });

            var rol = User.FindFirst("rol")?.Value;
            int? veliId = null;
            int? servisId = null;

            if (rol == "Veli")
            {
                if (!int.TryParse(User.FindFirst("veliId")?.Value, out var vid))
                    return Unauthorized("Oturum bilgileri eksik.");
                veliId = vid;
            }
            else if (rol == "Servis")
            {
                if (!int.TryParse(User.FindFirst("servisId")?.Value, out var sid))
                    return Unauthorized("Oturum bilgileri eksik.");
                servisId = sid;
            }

            try
            {
                var kayitlar = await _gecisKayitService.GetListAsync(baslangic, bitis, arama, sinifId, veliId, servisId);
                return Ok(kayitlar);
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "Giriş/çıkış kayıtları alınırken bir hata oluştu." });
            }
        }

        // GET: api/gecis-kayit/{ogrenciId}
        // Rol bazlı: Veli sadece kendi çocuğunun, Servis kendi servis öğrencisinin kayıtlarını görür
        [HttpGet("{ogrenciId}")]
        public async Task<IActionResult> OgrenciyeGoreGetir(int ogrenciId)
        {
            try
            {
                // Öğrenciyi getirip rol bazlı erişim kontrolü yap
                var ogrenci = await _ogrenciService.OgrenciGetirAsync(ogrenciId);
                if (ogrenci is null)
                    return NotFound(new { message = $"{ogrenciId} numaralı öğrenci bulunamadı." });

                var rol = User.FindFirst("rol")?.Value;
                if (rol == "Veli")
                {
                    if (!int.TryParse(User.FindFirst("veliId")?.Value, out var veliId) || ogrenci.VeliId != veliId)
                        return Forbid();
                }
                else if (rol == "Servis")
                {
                    if (!int.TryParse(User.FindFirst("servisId")?.Value, out var servisId) || ogrenci.ServisId != servisId)
                        return Forbid();
                }

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
