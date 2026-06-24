using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OgrenciBilgiSistemi.Api.Services.Interfaces;

namespace OgrenciBilgiSistemi.Api.Controllers
{
    [Route("api/bildirimler")]
    [ApiController]
    [Authorize]
    public class BildirimlerController : ControllerBase
    {
        private readonly IBildirimService _bildirimService;

        public BildirimlerController(IBildirimService bildirimService)
        {
            _bildirimService = bildirimService;
        }

        private int KullaniciId => int.Parse(User.FindFirst("kullaniciId")!.Value);

        [HttpGet]
        public async Task<IActionResult> Listele([FromQuery] int sayfaNo = 1)
        {
            var liste = await _bildirimService.KullanicininBildirimleriniGetir(KullaniciId, sayfaNo);
            return Ok(liste);
        }

        [HttpGet("okunmamis-sayisi")]
        public async Task<IActionResult> OkunmamisSayisi()
        {
            var sayi = await _bildirimService.OkunmamisSayisi(KullaniciId);
            return Ok(new { sayi });
        }

        [HttpPut("{id}/okundu")]
        public async Task<IActionResult> OkunduIsaretle(int id)
        {
            var basarili = await _bildirimService.OkunduIsaretle(id, KullaniciId);
            if (!basarili) return NotFound();
            return Ok(new { mesaj = "Bildirim okundu olarak işaretlendi." });
        }

        [HttpPut("tumunu-okundu")]
        public async Task<IActionResult> TumunuOkundu()
        {
            await _bildirimService.TumunuOkunduIsaretle(KullaniciId);
            return Ok(new { mesaj = "Tüm bildirimler okundu olarak işaretlendi." });
        }
    }
}
