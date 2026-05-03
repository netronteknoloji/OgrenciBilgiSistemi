using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using OgrenciBilgiSistemi.Api.Dtos;
using OgrenciBilgiSistemi.Api.Models;
using OgrenciBilgiSistemi.Api.Services;
using OgrenciBilgiSistemi.Shared.Enums;
using OgrenciBilgiSistemi.Shared.Services;
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
        private readonly OkulYapilandirmaServisi _okulServisi;

        // Access token süresi (dakika)
        private const int AccessTokenDakika = 30;

        public KimlikDogrulamaController(
            GirisService girisService,
            RefreshTokenService refreshTokenService,
            IConfiguration configuration,
            OkulYapilandirmaServisi okulServisi)
        {
            _girisService = girisService;
            _refreshTokenService = refreshTokenService;
            _configuration = configuration;
            _okulServisi = okulServisi;
        }

        /// <summary>
        /// Yapılandırılmış okul listesini döner. Connection string bilgisi dönmez.
        /// </summary>
        [AllowAnonymous]
        [HttpGet("okullar")]
        public IActionResult OkullariListele()
        {
            var okullar = _okulServisi.TumOkullariGetir()
                .Select(o => new { o.OkulKodu, o.OkulAdi })
                .ToList();
            return Ok(okullar);
        }

        /// <summary>
        /// Genel Admin şifre hash'i üretmek için geçici endpoint.
        /// Hash alındıktan sonra bu endpoint kaldırılabilir.
        /// </summary>
        [Authorize(Policy = "AdminOnly")]
        [HttpGet("hash-uret")]
        public IActionResult HashUret([FromQuery] string sifre)
        {
            if (string.IsNullOrWhiteSpace(sifre))
                return BadRequest("Şifre boş olamaz.");

            var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<object>();
            var hash = hasher.HashPassword(null!, sifre);
            return Ok(new { hash });
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> GirisYap([FromBody] GirisIstegiDto istek)
        {
            if (string.IsNullOrWhiteSpace(istek.KullaniciAdi) ||
                string.IsNullOrWhiteSpace(istek.Sifre))
                return BadRequest("Kullanıcı adı veya şifre boş olamaz.");

            if (string.IsNullOrWhiteSpace(istek.OkulKodu))
                return BadRequest("Okul kodu boş olamaz.");

            var okul = _okulServisi.OkulGetir(istek.OkulKodu);
            if (okul is null)
                return BadRequest("Geçersiz okul kodu.");

            // GenelAdmin (multi-tenant): kullanıcı adı/şifre appsettings'te tanımlıdır,
            // DB'de aranmaz. Seçilen okulun bağlamında token üretilir.
            var genelAdminKullaniciAdi = _configuration["GenelAdmin:KullaniciAdi"];
            var genelAdminSifreHash = _configuration["GenelAdmin:SifreHash"];
            if (!string.IsNullOrWhiteSpace(genelAdminKullaniciAdi) &&
                !string.IsNullOrWhiteSpace(genelAdminSifreHash) &&
                istek.KullaniciAdi == genelAdminKullaniciAdi)
            {
                var genelHasher = new Microsoft.AspNetCore.Identity.PasswordHasher<object>();
                var dogrulama = genelHasher.VerifyHashedPassword(null!, genelAdminSifreHash, istek.Sifre);
                if (dogrulama == Microsoft.AspNetCore.Identity.PasswordVerificationResult.Failed)
                    return Unauthorized("Kullanıcı adı veya şifre hatalı.");

                var genelAdminKullanici = new KullaniciModel
                {
                    KullaniciId = 0,
                    KullaniciAdi = genelAdminKullaniciAdi,
                    AdSoyad = "Genel Yönetici",
                    Rol = KullaniciRolu.GenelAdmin,
                    KullaniciDurum = true
                };

                var genelToken = GenerateJwtToken(genelAdminKullanici, okul.OkulKodu);
                var genelRefresh = _refreshTokenService.TokenOlustur(genelAdminKullanici.KullaniciId, okul.OkulKodu);

                return Ok(new
                {
                    token = genelToken,
                    refreshToken = genelRefresh,
                    expiresIn = AccessTokenDakika * 60,
                    kullanici = new
                    {
                        genelAdminKullanici.KullaniciId,
                        genelAdminKullanici.KullaniciAdi,
                        genelAdminKullanici.AdSoyad,
                        genelAdminKullanici.KullaniciDurum,
                        genelAdminKullanici.Rol,
                        genelAdminKullanici.BirimId,
                        genelAdminKullanici.VeliProfilVar,
                        genelAdminKullanici.ServisProfilVar
                    }
                });
            }

            var kullanici = await _girisService.KimlikDogrulaAsync(
                istek.KullaniciAdi, istek.Sifre, okul.ConnectionString);

            if (kullanici is null)
                return Unauthorized("Kullanıcı adı veya şifre hatalı.");

            // JWT access token üret (kısa süreli)
            var accessToken = GenerateJwtToken(kullanici, okul.OkulKodu);

            // Refresh token üret (uzun süreli, tek kullanımlık)
            var refreshToken = _refreshTokenService.TokenOlustur(kullanici.KullaniciId, okul.OkulKodu);

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
                    kullanici.BirimId,
                    kullanici.VeliProfilVar,
                    kullanici.ServisProfilVar
                }
            });
        }

        /// <summary>
        /// Süresi dolmuş access token yerine yeni bir token çifti üretir.
        /// Refresh token tek kullanımlıktır — her yenilemede yeni bir çift döner.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("refresh")]
        public async Task<IActionResult> TokenYenile([FromBody] TokenYenilemeIstegiDto istek)
        {
            if (string.IsNullOrWhiteSpace(istek.RefreshToken))
                return BadRequest("Refresh token boş olamaz.");

            // Refresh token'ı doğrula ve tüket
            var sonuc = _refreshTokenService.TokenDogrula(istek.RefreshToken);
            if (sonuc is null)
                return Unauthorized("Refresh token geçersiz veya süresi dolmuş.");

            var (kullaniciId, okulKodu) = sonuc.Value;

            // Okul bilgisini doğrula
            var okul = _okulServisi.OkulGetir(okulKodu);
            if (okul is null)
                return Unauthorized("Geçersiz okul kodu.");

            // Kullanıcının hâlâ aktif olduğunu doğrula
            var kullanici = await _girisService.KimlikDogrulaAsync_IdIle(kullaniciId, okul.ConnectionString);
            if (kullanici is null || !kullanici.KullaniciDurum)
                return Unauthorized("Kullanıcı hesabı aktif değil.");

            // Yeni access token + yeni refresh token üret (token rotation)
            var yeniAccessToken = GenerateJwtToken(kullanici, okulKodu);
            var yeniRefreshToken = _refreshTokenService.TokenOlustur(kullanici.KullaniciId, okulKodu);

            return Ok(new
            {
                token = yeniAccessToken,
                refreshToken = yeniRefreshToken,
                expiresIn = AccessTokenDakika * 60
            });
        }

        /// <summary>
        /// Kullanıcı adının ilk harflerine göre eşleşen kullanıcıları döner.
        /// Minimum 3 karakter gerektirir. JWT gerektirmez.
        /// </summary>
        [AllowAnonymous]
        [EnableRateLimiting("arama")]
        [HttpGet("kullanici-ara")]
        public async Task<IActionResult> KullaniciAra([FromQuery] string q, [FromQuery] string okulKodu)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 3)
                return BadRequest("Arama metni en az 3 karakter olmalıdır.");

            if (string.IsNullOrWhiteSpace(okulKodu))
                return BadRequest("Okul kodu boş olamaz.");

            var okul = _okulServisi.OkulGetir(okulKodu);
            if (okul is null)
                return BadRequest("Geçersiz okul kodu.");

            var sonuclar = await _girisService.KullaniciAdiAraAsync(q.Trim(), okul.ConnectionString);
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
        private string GenerateJwtToken(KullaniciModel kullanici, string okulKodu)
        {
            var secretKey = _configuration["Jwt:SecretKey"]
                ?? throw new InvalidOperationException("Jwt:SecretKey yapılandırılmamış.");

            var key         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var rolAdi = kullanici.Rol switch
            {
                KullaniciRolu.Admin     => "Admin",
                KullaniciRolu.Ogretmen  => "Ogretmen",
                KullaniciRolu.Servis    => "Servis",
                KullaniciRolu.Veli      => "Veli",
                KullaniciRolu.GenelAdmin => "GenelAdmin",
                _ => throw new InvalidOperationException($"Desteklenmeyen rol: {kullanici.Rol}")
            };

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub,        kullanici.KullaniciId.ToString()),
                new(JwtRegisteredClaimNames.UniqueName, kullanici.KullaniciAdi),
                new(JwtRegisteredClaimNames.Jti,        Guid.NewGuid().ToString()),
                new("kullaniciId", kullanici.KullaniciId.ToString()),
                new("rol",         rolAdi),
                new("okulKodu",    okulKodu)
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
