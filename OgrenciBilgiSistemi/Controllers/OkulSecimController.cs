using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OgrenciBilgiSistemi.Services.Interfaces;
using OgrenciBilgiSistemi.Shared.Services;
using OgrenciBilgiSistemi.ViewModels;

namespace OgrenciBilgiSistemi.Controllers
{
    [Authorize(Roles = "GenelAdmin")]
    public class OkulSecimController : Controller
    {
        private readonly OkulYapilandirmaServisi _okulServisi;
        private readonly IKimlikDogrulamaService _kimlikServisi;

        public OkulSecimController(OkulYapilandirmaServisi okulServisi, IKimlikDogrulamaService kimlikServisi)
        {
            _okulServisi = okulServisi;
            _kimlikServisi = kimlikServisi;
        }

        public IActionResult Index()
        {
            return View(new OkulSecimVm
            {
                Okullar = _okulServisi.TumOkullariGetir(),
                SeciliOkulKodu = HttpContext.Session.GetString("SeciliOkulKodu")
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OkulSec(string okulKodu, CancellationToken ct = default)
        {
            if (!_okulServisi.OkulVarMi(okulKodu))
                return BadRequest("Geçersiz okul kodu.");

            HttpContext.Session.SetString("SeciliOkulKodu", okulKodu);

            var okul = _okulServisi.OkulGetir(okulKodu);
            await _kimlikServisi.GenelAdminOlusturAsync(okul!.ConnectionString, ct);

            return RedirectToAction("Index", "Home");
        }
    }
}
