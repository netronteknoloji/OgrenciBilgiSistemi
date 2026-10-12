using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using OgrenciBilgiSistemi.Dtos;
using OgrenciBilgiSistemi.Infrastructure;
using OgrenciBilgiSistemi.Services.Interfaces;
using OgrenciBilgiSistemi.Shared.Kurallar;
using OgrenciBilgiSistemi.Shared.Services;
using System.Security.Claims;

namespace OgrenciBilgiSistemi.Controllers
{
    public class HesaplarController : Controller
    {
        private readonly IKimlikDogrulamaService _kimlikServisi;
        private readonly OkulYapilandirmaServisi _okulServisi;
        private readonly IConfiguration _configuration;
        private readonly GirisDenemeSayaci _denemeSayaci;
        private readonly IMemoryCache _cache;

        public HesaplarController(
            IKimlikDogrulamaService kimlikServisi,
            OkulYapilandirmaServisi okulServisi,
            IConfiguration configuration,
            GirisDenemeSayaci denemeSayaci,
            IMemoryCache cache)
        {
            _kimlikServisi = kimlikServisi;
            _okulServisi = okulServisi;
            _configuration = configuration;
            _denemeSayaci = denemeSayaci;
            _cache = cache;
        }

        // Art arda hatalı denemelerde hesap geçici kilitlenir; kilitliyse formu hata ile döndürür
        private IActionResult? KilitliyseReddet(string denemeAnahtari, GirisIstegiDto model)
        {
            if (!_denemeSayaci.KilitliMi(denemeAnahtari, out var kalan))
                return null;

            ModelState.AddModelError(string.Empty, GirisDenemeSayaci.KilitMesaji(kalan));
            model.Okullar = _okulServisi.TumOkullariGetir();
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Giris()
        {
            return View(new GirisIstegiDto { Okullar = _okulServisi.TumOkullariGetir() });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Giris(GirisIstegiDto model)
        {
            // --- Genel Admin kontrolü (okul seçimi gerekmez, DB erişimi yok) ---
            var genelAdminKullaniciAdi = _configuration["GenelAdmin:KullaniciAdi"];
            var genelAdminSifreHash = _configuration["GenelAdmin:SifreHash"];

            if (!string.IsNullOrEmpty(genelAdminKullaniciAdi) &&
                !string.IsNullOrWhiteSpace(model.KullaniciAdi) &&
                !string.IsNullOrWhiteSpace(model.Sifre) &&
                model.KullaniciAdi == genelAdminKullaniciAdi)
            {
                if (string.IsNullOrEmpty(genelAdminSifreHash))
                {
                    ModelState.AddModelError(string.Empty, "Genel admin yapılandırması eksik.");
                    model.Okullar = _okulServisi.TumOkullariGetir();
                    return View(model);
                }

                var genelAnahtar = GirisDenemeSayaci.Anahtar(null, model.KullaniciAdi);
                if (KilitliyseReddet(genelAnahtar, model) is { } genelKilit)
                    return genelKilit;

                var genelHasher = new PasswordHasher<object>();
                var genelResult = genelHasher.VerifyHashedPassword(null!, genelAdminSifreHash, model.Sifre);
                if (genelResult == PasswordVerificationResult.Failed)
                {
                    _denemeSayaci.HataliKaydet(genelAnahtar);
                    ModelState.AddModelError(string.Empty, "Geçersiz kullanıcı adı veya şifre.");
                    model.Okullar = _okulServisi.TumOkullariGetir();
                    return View(model);
                }

                _denemeSayaci.Sifirla(genelAnahtar);

                var genelClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, genelAdminKullaniciAdi),
                    new Claim(ClaimTypes.NameIdentifier, "0"),
                    new Claim(ClaimTypes.Role, "GenelAdmin")
                };

                var genelIdentity = new ClaimsIdentity(genelClaims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(genelIdentity),
                    new AuthenticationProperties { IsPersistent = model.BeniHatirla });

                return RedirectToAction("Index", "OkulSecim");
            }

            // --- Normal kullanıcı (okul bazlı) login ---
            if (!ModelState.IsValid)
            {
                model.Okullar = _okulServisi.TumOkullariGetir();
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(model.OkulKodu))
            {
                ModelState.AddModelError(string.Empty, "Okul seçimi gereklidir.");
                model.Okullar = _okulServisi.TumOkullariGetir();
                return View(model);
            }

            var okul = _okulServisi.OkulGetir(model.OkulKodu);
            if (okul is null)
            {
                ModelState.AddModelError(string.Empty, "Geçersiz okul kodu.");
                model.Okullar = _okulServisi.TumOkullariGetir();
                return View(model);
            }

            var denemeAnahtari = GirisDenemeSayaci.Anahtar(okul.OkulKodu, model.KullaniciAdi);
            if (KilitliyseReddet(denemeAnahtari, model) is { } kilit)
                return kilit;

            var user = await _kimlikServisi.DogrulaAsync(okul.ConnectionString, model.KullaniciAdi, model.Sifre);

            if (user is null)
            {
                _denemeSayaci.HataliKaydet(denemeAnahtari);
                ModelState.AddModelError(string.Empty, "Geçersiz kullanıcı adı veya şifre.");
                model.Okullar = _okulServisi.TumOkullariGetir();
                return View(model);
            }

            _denemeSayaci.Sifirla(denemeAnahtari);

            // Veli ve Servis hesapları yalnızca mobil uygulama içindir; web paneline giremez
            if (!PanelOturumKurallari.PaneleGirebilirMi(user.Rol))
            {
                ModelState.AddModelError(string.Empty, "Bu hesap mobil uygulama içindir; web paneline giriş yapılamaz.");
                model.Okullar = _okulServisi.TumOkullariGetir();
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.KullaniciAdi),
                new Claim(ClaimTypes.NameIdentifier, user.KullaniciId.ToString()),
                new Claim("userid", user.KullaniciId.ToString()),
                new Claim("KullaniciId", user.KullaniciId.ToString()),
                new Claim("sub", user.KullaniciId.ToString()),
                new Claim(ClaimTypes.Role, user.Rol.ToString()),
                new Claim("okulKodu", okul.OkulKodu),
                new Claim("okulAdi", okul.OkulAdi)
            };

            // Önceki oturumdan kalan karar (eski rol / pasif) yeni oturumu reddetmesin
            OturumDogrulayici.OnbellegiTemizle(_cache, okul.OkulKodu, user.KullaniciId);

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties { IsPersistent = model.BeniHatirla });

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Cikis()
        {
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Giris", "Hesaplar");
        }

        [AllowAnonymous]
        public IActionResult YetkisizGiris() => View();
    }
}
