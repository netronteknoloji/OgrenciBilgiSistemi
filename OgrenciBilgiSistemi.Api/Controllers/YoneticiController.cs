using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OgrenciBilgiSistemi.Api.Services;
using OgrenciBilgiSistemi.Shared.Services;

namespace OgrenciBilgiSistemi.Api.Controllers
{
    [ApiController]
    [Route("api/yonetici")]
    [Authorize(Policy = "AdminOnly")]
    public class YoneticiController : ControllerBase
    {
        private readonly YoneticiService _yoneticiService;
        private readonly OkulYapilandirmaServisi _okulServisi;

        public YoneticiController(
            YoneticiService yoneticiService,
            OkulYapilandirmaServisi okulServisi)
        {
            _yoneticiService = yoneticiService;
            _okulServisi = okulServisi;
        }

        /// <summary>
        /// Yönetici ana sayfası için okulun toplam sayım ve bugünkü geçiş özetini döner.
        /// </summary>
        [HttpGet("ozet")]
        public async Task<IActionResult> Ozet()
        {
            var ozet = await _yoneticiService.OkulOzetGetirAsync();

            // Token'daki okulKodu üzerinden okul adını ekle
            var okulKodu = User.FindFirst("okulKodu")?.Value;
            if (!string.IsNullOrWhiteSpace(okulKodu))
            {
                var okul = _okulServisi.OkulGetir(okulKodu);
                if (okul is not null)
                    ozet.OkulAdi = okul.OkulAdi;
            }

            return Ok(ozet);
        }

        /// <summary>
        /// Yönetici ana sayfası için tüm servis kullanıcılarını öğrenci sayılarıyla birlikte döner.
        /// </summary>
        [HttpGet("servisler")]
        public async Task<IActionResult> Servisler()
            => Ok(await _yoneticiService.TumServisleriGetirAsync());

        /// <summary>
        /// Bugün yemekhaneye giriş yapan öğrencilerin listesini döner.
        /// </summary>
        [HttpGet("yemekhane-bugun")]
        public async Task<IActionResult> YemekhaneBugun()
            => Ok(await _yoneticiService.BugunYemekhaneGirislerinAsync());

        /// <summary>
        /// Bugün ana kapıdan çıkış yapan öğrencilerin listesini döner.
        /// </summary>
        [HttpGet("anakapi-cikis-bugun")]
        public async Task<IActionResult> AnakapiCikisBugun()
            => Ok(await _yoneticiService.BugunAnakapiCikislariAsync());
    }
}
