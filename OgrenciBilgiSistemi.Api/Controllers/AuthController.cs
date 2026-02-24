using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OgrenciBilgiSistemi.Api.Models;
using OgrenciBilgiSistemi.Api.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace OgrenciBilgiSistemi.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly LoginService  _loginService;
        private readonly IConfiguration _config;

        public AuthController(LoginService loginService, IConfiguration config)
        {
            _loginService = loginService;
            _config       = config;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Kullanıcı adı veya şifre boş olamaz.");

            var user = await _loginService.AuthenticateAsync(request.Username, request.Password);

            if (user is null)
                return Unauthorized("Kullanıcı adı veya şifre hatalı.");

            var token = GenerateJwtToken(user);

            return Ok(new
            {
                token,
                expiresIn = _config.GetValue<int>("JwtSettings:ExpiresInMinutes", 480) * 60,
                user = new
                {
                    user.Id,
                    user.Username,
                    user.UnitId,
                    user.IsActive,
                    user.IsAdmin
                }
            });
        }

        private string GenerateJwtToken(User user)
        {
            var secret   = _config["JwtSettings:SecretKey"]!;
            var issuer   = _config["JwtSettings:Issuer"]   ?? "OgrenciBilgiSistemiApi";
            var audience = _config["JwtSettings:Audience"] ?? "OgrenciBilgiSistemiClient";
            var minutes  = _config.GetValue<int>("JwtSettings:ExpiresInMinutes", 480);

            var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub,  user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Name, user.Username),
                new Claim(JwtRegisteredClaimNames.Jti,  Guid.NewGuid().ToString()),
                new Claim("userid", user.Id.ToString())
            };

            if (user.UnitId.HasValue)
                claims.Add(new Claim("unitid", user.UnitId.Value.ToString()));

            if (user.IsAdmin)
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));

            var token = new JwtSecurityToken(
                issuer:             issuer,
                audience:           audience,
                claims:             claims,
                notBefore:          DateTime.UtcNow,
                expires:            DateTime.UtcNow.AddMinutes(minutes),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    public record LoginRequest(string Username, string Password);
}
