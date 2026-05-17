using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OgrenciBilgiSistemi.Api.Dtos;
using OgrenciBilgiSistemi.Push;

namespace OgrenciBilgiSistemi.Api.Controllers
{
    [Route("api/cihazlar")]
    [ApiController]
    [Authorize]
    public class CihazlarController : ControllerBase
    {
        private readonly IBildirimTokenDeposu _tokenDeposu;
        private readonly IPushBildirimGonderici _pushGonderici;

        public CihazlarController(IBildirimTokenDeposu tokenDeposu, IPushBildirimGonderici pushGonderici)
        {
            _tokenDeposu = tokenDeposu;
            _pushGonderici = pushGonderici;
        }

        private int KullaniciId => int.Parse(User.FindFirst("kullaniciId")!.Value);

        [HttpPost("kaydet")]
        public async Task<IActionResult> Kaydet([FromBody] CihazKayitIstegiDto istek, CancellationToken ct)
        {
            await _tokenDeposu.UpsertAsync(new BildirimCihazKaydi(
                KullaniciId,
                istek.FcmToken,
                istek.Platform,
                istek.UygulamaSurumu,
                istek.CihazModeli), ct);
            return Ok(new { mesaj = "Cihaz kaydedildi." });
        }

        [HttpDelete("kaydi-sil")]
        public async Task<IActionResult> KaydiSil([FromBody] CihazSilIstegiDto istek, CancellationToken ct)
        {
            await _tokenDeposu.IptalAsync(new[] { istek.FcmToken }, ct);
            return Ok(new { mesaj = "Cihaz kaydı silindi." });
        }

        [HttpPost("token-yenile")]
        public async Task<IActionResult> TokenYenile([FromBody] CihazTokenYenileIstegiDto istek, CancellationToken ct)
        {
            if (!string.IsNullOrWhiteSpace(istek.EskiToken) && istek.EskiToken != istek.YeniToken)
                await _tokenDeposu.IptalAsync(new[] { istek.EskiToken }, ct);

            await _tokenDeposu.UpsertAsync(new BildirimCihazKaydi(
                KullaniciId,
                istek.YeniToken,
                istek.Platform,
                istek.UygulamaSurumu,
                istek.CihazModeli), ct);

            return Ok(new { mesaj = "Token yenilendi." });
        }

        [HttpPost("test-push")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> TestPush([FromBody] TestPushIstegiDto istek, CancellationToken ct)
        {
            var sonuc = await _pushGonderici.GonderAsync(istek.AliciKullaniciId, new PushBildirimYuku(
                istek.Baslik,
                istek.Govde,
                new Dictionary<string, string>
                {
                    ["tur"] = "Test",
                    ["bildirimId"] = "0"
                }), ct);

            return Ok(new
            {
                sonuc.BasariliSayisi,
                sonuc.BasarisizSayisi,
                GecersizTokenSayisi = sonuc.GecersizTokenlar.Count
            });
        }
    }
}
