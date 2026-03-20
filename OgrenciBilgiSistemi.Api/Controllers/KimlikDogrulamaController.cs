using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OgrenciBilgiSistemi.Api.Dtos;
using OgrenciBilgiSistemi.Api.Models;
using OgrenciBilgiSistemi.Api.Services;
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
        private readonly IConfiguration _configuration;

        public KimlikDogrulamaController(GirisService girisService, IConfiguration configuration)
        {
            _girisService = girisService;
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

            // JWT token üret — geçerlilik süresi 8 saat (mobil uygulama ile eşleşir)
            var token = GenerateJwtToken(kullanici);

            return Ok(new
            {
                token,
                expiresIn = 8 * 3600, // saniye cinsinden
                kullanici = new
                {
                    kullanici.KullaniciId,
                    kullanici.KullaniciAdi,
                    kullanici.BirimId,
                    kullanici.KullaniciDurum,
                    kullanici.AdminMi
                }
            });
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

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub,        kullanici.KullaniciId.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, kullanici.KullaniciAdi),
                new Claim(JwtRegisteredClaimNames.Jti,        Guid.NewGuid().ToString()),
                new Claim("kullaniciId", kullanici.KullaniciId.ToString()),
                new Claim("adminMi",     kullanici.AdminMi.ToString().ToLower())
            };

            var token = new JwtSecurityToken(
                issuer:             _configuration["Jwt:Issuer"],
                audience:           _configuration["Jwt:Audience"],
                claims:             claims,
                expires:            DateTime.UtcNow.AddHours(8),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
