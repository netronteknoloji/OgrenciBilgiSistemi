using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using OgrenciBilgiSistemi.Api.Dtos;
using OgrenciBilgiSistemi.Api.Models;
using OgrenciBilgiSistemi.Api.Services;
using OgrenciBilgiSistemi.Api.Services.Interfaces;
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
        private readonly IGirisService _girisService;
        private readonly RefreshTokenService _refreshTokenService;
        private readonly IConfiguration _configuration;
        private readonly OkulYapilandirmaServisi _okulServisi;
        private readonly ILogger<KimlikDogrulamaController> _logger;
        private readonly TimeProvider _timeProvider;
        private readonly GirisDenemeSayaci _denemeSayaci;

        // Access token süresi (dakika)
        private const int AccessTokenDakika = 30;

        public KimlikDogrulamaController(
            IGirisService girisService,
            RefreshTokenService refreshTokenService,
            IConfiguration configuration,
            OkulYapilandirmaServisi okulServisi,
            ILogger<KimlikDogrulamaController> logger,
            TimeProvider timeProvider,
            GirisDenemeSayaci denemeSayaci)
        {
            _denemeSayaci = denemeSayaci;
            _girisService = girisService;
            _refreshTokenService = refreshTokenService;
            _configuration = configuration;
            _okulServisi = okulServisi;
            _logger = logger;
            _timeProvider = timeProvider;
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

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> GirisYap([FromBody] ApiGirisIstegiDto istek)
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

            // Art arda hatalı denemeler hesabı geçici kilitler (Genel Admin okuldan bağımsız sayılır)
            var denemeAnahtari = GirisDenemeSayaci.Anahtar(
                istek.KullaniciAdi == genelAdminKullaniciAdi ? null : okul.OkulKodu, istek.KullaniciAdi);
            if (_denemeSayaci.KilitliMi(denemeAnahtari, out var kalan))
                return StatusCode(StatusCodes.Status429TooManyRequests, GirisDenemeSayaci.KilitMesaji(kalan));

            if (!string.IsNullOrWhiteSpace(genelAdminKullaniciAdi) &&
                !string.IsNullOrWhiteSpace(genelAdminSifreHash) &&
                istek.KullaniciAdi == genelAdminKullaniciAdi)
            {
                var genelHasher = new Microsoft.AspNetCore.Identity.PasswordHasher<object>();
                var dogrulama = genelHasher.VerifyHashedPassword(null!, genelAdminSifreHash, istek.Sifre);
                if (dogrulama == Microsoft.AspNetCore.Identity.PasswordVerificationResult.Failed)
                {
                    _denemeSayaci.HataliKaydet(denemeAnahtari);
                    return Unauthorized("Kullanıcı adı veya şifre hatalı.");
                }
                _denemeSayaci.Sifirla(denemeAnahtari);

                var genelAdminKullanici = GenelAdminKullanicisi(genelAdminKullaniciAdi);

                var (genelRefresh, genelOturum) = await _refreshTokenService.OlusturAsync(okul, genelAdminKullanici.KullaniciId);
                var genelToken = GenerateJwtToken(genelAdminKullanici, okul.OkulKodu, genelOturum);

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
                        genelAdminKullanici.IsDeleted,
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
            {
                _denemeSayaci.HataliKaydet(denemeAnahtari);
                return Unauthorized("Kullanıcı adı veya şifre hatalı.");
            }
            _denemeSayaci.Sifirla(denemeAnahtari);

            // Refresh token (yeni oturum, tek kullanımlık) + JWT access token (kısa süreli, oturumId claim'li)
            var (refreshToken, oturumId) = await _refreshTokenService.OlusturAsync(okul, kullanici.KullaniciId);
            var accessToken = GenerateJwtToken(kullanici, okul.OkulKodu, oturumId);

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
                    kullanici.IsDeleted,
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

            // Refresh token'ı doğrula ve tüket (okul, token'ın içinden çözülür)
            var sonuc = await _refreshTokenService.TuketAsync(istek.RefreshToken);
            if (sonuc is null)
                return Unauthorized("Refresh token geçersiz veya süresi dolmuş.");

            var okul = sonuc.Okul;
            KullaniciModel? kullanici;
            if (sonuc.KullaniciId == 0)
            {
                // Genel Admin: DB'de satırı yok; yapılandırmada hâlâ tanımlıysa yenilenir
                var genelAdminKullaniciAdi = _configuration["GenelAdmin:KullaniciAdi"];
                kullanici = string.IsNullOrWhiteSpace(genelAdminKullaniciAdi) ? null : GenelAdminKullanicisi(genelAdminKullaniciAdi);
            }
            else
            {
                // Kullanıcının (ve profilinin) hâlâ aktif olduğunu doğrula
                kullanici = await _girisService.KimlikDogrulaAsync_IdIle(sonuc.KullaniciId, okul.ConnectionString);
            }

            if (kullanici is null || kullanici.IsDeleted)
                return Unauthorized("Kullanıcı hesabı aktif değil.");

            // Aynı oturumda yeni token çifti (token rotation)
            var (yeniRefreshToken, _) = await _refreshTokenService.OlusturAsync(okul, kullanici.KullaniciId, sonuc.OturumId);
            var yeniAccessToken = GenerateJwtToken(kullanici, okul.OkulKodu, sonuc.OturumId);

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

                // Şifre değişince tüm cihazlardaki oturumlar kapanır (bu cihaz access token süresince çalışır)
                var okul = _okulServisi.OkulGetir(User.FindFirst("okulKodu")?.Value ?? string.Empty);
                if (okul is not null)
                    await _refreshTokenService.KullanicininTumunuIptalEtAsync(okul, kullaniciId);

                return Ok(new { mesaj = "Şifre başarıyla değiştirildi." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Şifre değiştirilirken hata. KullaniciId: {Id}", kullaniciId);
                return StatusCode(500, new { error = "Şifre değiştirilirken bir hata oluştu." });
            }
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> CikisYap()
        {
            var kullaniciIdStr = User.FindFirst("kullaniciId")?.Value;
            var okul = _okulServisi.OkulGetir(User.FindFirst("okulKodu")?.Value ?? string.Empty);
            if (!int.TryParse(kullaniciIdStr, out var kullaniciId) || okul is null)
                return Unauthorized("Oturum bilgileri eksik.");

            // Yalnızca bu cihazın oturumu kapanır; oturumId'siz (eski) token'da kullanıcının tümü
            if (Guid.TryParse(User.FindFirst("oturumId")?.Value, out var oturumId))
                await _refreshTokenService.OturumuIptalEtAsync(okul, oturumId);
            else
                await _refreshTokenService.KullanicininTumunuIptalEtAsync(okul, kullaniciId);

            return Ok(new { mesaj = "Oturum sonlandırıldı." });
        }

        /// <summary>
        /// Kullanıcı bilgilerini claim olarak içeren imzalı JWT token üretir.
        /// </summary>
        private static KullaniciModel GenelAdminKullanicisi(string kullaniciAdi) => new()
        {
            KullaniciId = 0,
            KullaniciAdi = kullaniciAdi,
            AdSoyad = "Genel Yönetici",
            Rol = KullaniciRolu.GenelAdmin,
            IsDeleted = false
        };

        private string GenerateJwtToken(KullaniciModel kullanici, string okulKodu, Guid oturumId)
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
                new("okulKodu",    okulKodu),
                new("oturumId",    oturumId.ToString())
            };

            if (kullanici.Rol == KullaniciRolu.Servis)
                claims.Add(new Claim("servisId", kullanici.KullaniciId.ToString()));

            if (kullanici.Rol == KullaniciRolu.Veli)
                claims.Add(new Claim("veliId", kullanici.KullaniciId.ToString()));

            var token = new JwtSecurityToken(
                issuer:             _configuration["Jwt:Issuer"],
                audience:           _configuration["Jwt:Audience"],
                claims:             claims,
                expires:            _timeProvider.GetUtcNow().UtcDateTime.AddMinutes(AccessTokenDakika),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
