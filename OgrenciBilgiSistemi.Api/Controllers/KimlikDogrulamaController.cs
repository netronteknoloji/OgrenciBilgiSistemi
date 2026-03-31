using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OgrenciBilgiSistemi.Api.Dtos;
using OgrenciBilgiSistemi.Api.Models;
using OgrenciBilgiSistemi.Api.Services;
using OgrenciBilgiSistemi.Shared.Enums;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace OgrenciBilgiSistemi.Api.Controllers
{
    [ApiController]
    [Route("api/kimlik-dogrulama")]
    public class KimlikDogrulamaController : ControllerBase
    {
        private readonly GirisService _girisService;
        private readonly RefreshTokenService _refreshTokenService;
        private readonly IConfiguration _configuration;

        // Access token süresi (dakika)
        private const int AccessTokenDakika = 30;

        public KimlikDogrulamaController(
            GirisService girisService,
            RefreshTokenService refreshTokenService,
            IConfiguration configuration)
        {
            _girisService = girisService;
            _refreshTokenService = refreshTokenService;
            _configuration = configuration;
        }

        [HttpPost("login")]
        public async Task<IActionResult> GirisYap([FromBody] GirisIstegiDto istek)
        {
            if (string.IsNullOrWhiteSpace(istek.KullaniciAdi) ||
                string.IsNullOrWhiteSpace(istek.Sifre))
                return BadRequest("Kullanıcı adı veya şifre boş olamaz.");

            var kullanici = await _girisService.KimlikDogrulaAsync(istek.KullaniciAdi, istek.Sifre);

            if (kullanici is null)
                return Unauthorized("Kullanıcı adı veya şifre hatalı.");

            // Mobil uygulamada admin girişi desteklenmez
            if (kullanici.Rol == KullaniciRolu.Admin)
                return Forbid("Bu uygulama yönetici girişi desteklememektedir.");

            // JWT access token üret (kısa süreli)
            var accessToken = GenerateJwtToken(kullanici);

            // Refresh token üret (uzun süreli, tek kullanımlık)
            var refreshToken = _refreshTokenService.TokenOlustur(kullanici.KullaniciId);

            return Ok(new
            {
                token = accessToken,
                refreshToken,
                expiresIn = AccessTokenDakika * 60, // saniye cinsinden
                kullanici = new
                {
                    kullanici.KullaniciId,
                    kullanici.KullaniciAdi,
                    kullanici.AdSoyad,
                    kullanici.KullaniciDurum,
                    kullanici.Rol,
                    kullanici.VeliProfilVar,
                    kullanici.ServisProfilVar
                }
            });
        }

        /// <summary>
        /// Süresi dolmuş access token yerine yeni bir token çifti üretir.
        /// Refresh token tek kullanımlıktır — her yenilemede yeni bir çift döner.
        /// </summary>
        [HttpPost("refresh")]
        public async Task<IActionResult> TokenYenile([FromBody] TokenYenilemeIstegiDto istek)
        {
            if (string.IsNullOrWhiteSpace(istek.RefreshToken))
                return BadRequest("Refresh token boş olamaz.");

            // Refresh token'ı doğrula ve tüket
            var kullaniciId = _refreshTokenService.TokenDogrula(istek.RefreshToken);
            if (kullaniciId is null)
                return Unauthorized("Refresh token geçersiz veya süresi dolmuş.");

            // Kullanıcının hâlâ aktif olduğunu doğrula
            var kullanici = await _girisService.KullaniciIdIleGetirAsync(kullaniciId.Value);
            if (kullanici is null || !kullanici.KullaniciDurum)
                return Unauthorized("Kullanıcı hesabı aktif değil.");

            // Yeni access token + yeni refresh token üret (token rotation)
            var yeniAccessToken = GenerateJwtToken(kullanici);
            var yeniRefreshToken = _refreshTokenService.TokenOlustur(kullanici.KullaniciId);

            return Ok(new
            {
                token = yeniAccessToken,
                refreshToken = yeniRefreshToken,
                expiresIn = AccessTokenDakika * 60
            });
        }

        /// <summary>
        /// Kullanıcının tüm refresh tokenlarını geçersiz kılarak oturumu sonlandırır.
        /// </summary>
        /// <summary>
        /// Kullanıcı adının ilk harflerine göre eşleşen kullanıcıları döner.
        /// Minimum 3 karakter gerektirir. JWT gerektirmez.
        /// </summary>
        [AllowAnonymous]
        [HttpGet("kullanici-ara")]
        public async Task<IActionResult> KullaniciAra([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 3)
                return BadRequest("Arama metni en az 3 karakter olmalıdır.");

            var sonuclar = await _girisService.KullaniciAdiAraAsync(q.Trim());
            return Ok(sonuclar);
        }

        /// <summary>
        /// Giriş yapmış kullanıcının şifresini değiştirir. Eski şifre sorgulanmaz.
        /// </summary>
        [Authorize]
        [HttpPost("sifre-degistir")]
        public async Task<IActionResult> SifreDegistir([FromBody] SifreDegistirIstegiDto istek)
        {
            var kullaniciIdStr = User.FindFirst("kullaniciId")?.Value;
            if (!int.TryParse(kullaniciIdStr, out var kullaniciId))
                return Unauthorized("Oturum bilgileri eksik.");

            try
            {
                var sonuc = await _girisService.SifreDegistirAsync(kullaniciId, istek.YeniSifre);
                if (!sonuc)
                    return NotFound(new { error = "Kullanıcı bulunamadı." });

                return Ok(new { mesaj = "Şifre başarıyla değiştirildi." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "Şifre değiştirilirken bir hata oluştu." });
            }
        }

        [Authorize]
        [HttpPost("logout")]
        public IActionResult CikisYap()
        {
            var kullaniciIdStr = User.FindFirst("kullaniciId")?.Value;
            if (!int.TryParse(kullaniciIdStr, out var kullaniciId))
                return Unauthorized("Oturum bilgileri eksik.");

            _refreshTokenService.KullaniciTokenlariniSil(kullaniciId);

            return Ok(new { mesaj = "Oturum sonlandırıldı." });
        }

        /// <summary>
        /// Kullanıcı bilgilerini claim olarak içeren imzalı JWT token üretir.
        /// </summary>
        private string GenerateJwtToken(KullaniciModel kullanici)
        {
            var secretKey = _configuration["Jwt:SecretKey"]
                ?? throw new InvalidOperationException("Jwt:SecretKey yapılandırılmamış.");

            var key         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var rolAdi = kullanici.Rol switch
            {
                KullaniciRolu.Ogretmen => "Ogretmen",
                KullaniciRolu.Servis    => "Servis",
                KullaniciRolu.Veli     => "Veli",
                _ => throw new InvalidOperationException($"Desteklenmeyen rol: {kullanici.Rol}")
            };

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub,        kullanici.KullaniciId.ToString()),
                new(JwtRegisteredClaimNames.UniqueName, kullanici.KullaniciAdi),
                new(JwtRegisteredClaimNames.Jti,        Guid.NewGuid().ToString()),
                new("kullaniciId", kullanici.KullaniciId.ToString()),
                new("rol",         rolAdi)
            };

            if (kullanici.Rol == KullaniciRolu.Servis)
                claims.Add(new Claim("servisId", kullanici.KullaniciId.ToString()));

            if (kullanici.Rol == KullaniciRolu.Veli)
                claims.Add(new Claim("veliId", kullanici.KullaniciId.ToString()));

            var token = new JwtSecurityToken(
                issuer:             _configuration["Jwt:Issuer"],
                audience:           _configuration["Jwt:Audience"],
                claims:             claims,
                expires:            DateTime.UtcNow.AddMinutes(AccessTokenDakika),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
