using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OgrenciBilgiSistemi.Api.Dtos;
using OgrenciBilgiSistemi.Api.Services.Interfaces;
using OgrenciBilgiSistemi.Shared.Services;

namespace OgrenciBilgiSistemi.Api.Controllers
{
    [ApiController]
    [Route("api/servisler")]
    [Authorize]
    public class ServisController : ControllerBase
    {
        private readonly IServisService _servisService;
        private readonly IOgrenciService _ogrenciService;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly TenantBaglami _tenantBaglami;
        private readonly ILogger<ServisController> _logger;

        public ServisController(IServisService servisService, IOgrenciService ogrenciService, IServiceScopeFactory scopeFactory, TenantBaglami tenantBaglami, ILogger<ServisController> logger)
        {
            _servisService = servisService;
            _ogrenciService = ogrenciService;
            _scopeFactory = scopeFactory;
            _tenantBaglami = tenantBaglami;
            _logger = logger;
        }

        /// <summary>
        /// Belirtilen servise (KullaniciId) atanmış öğrencileri getirir.
        /// Yalnızca servis kendi servis ID'si ile erişebilir.
        /// </summary>
        [HttpGet("{servisId}/ogrenciler")]
        public async Task<IActionResult> ServisOgrencileriGetir(int servisId)
        {
            var yetkiSonucu = ServisErisimKontrol(servisId);
            if (yetkiSonucu != null) return yetkiSonucu;

            var ogrenciler = await _servisService.ServisOgrencileriGetir(servisId);
            return Ok(ogrenciler);
        }

        /// <summary>
        /// Belirtilen servisün servis profil bilgilerini getirir.
        /// Yalnızca servis kendi profilini görüntüleyebilir.
        /// </summary>
        [HttpGet("{servisId}")]
        public async Task<IActionResult> ServisProfilGetir(int servisId)
        {
            var yetkiSonucu = ServisErisimKontrol(servisId);
            if (yetkiSonucu != null) return yetkiSonucu;

            var profil = await _servisService.ServisProfilGetir(servisId);
            if (profil == null)
                return NotFound("Servis profili bulunamadı.");

            return Ok(profil);
        }

        /// <summary>
        /// Belirtilen servisün bugünkü yoklamasını periyoda göre getirir.
        /// Yalnızca servis kendi yoklamasını görüntüleyebilir.
        /// </summary>
        [HttpGet("{servisId}/yoklama/{periyot}")]
        public async Task<IActionResult> ServisYoklamaGetir(int servisId, int periyot)
        {
            var yetkiSonucu = ServisErisimKontrol(servisId);
            if (yetkiSonucu != null) return yetkiSonucu;

            try
            {
                var yoklama = await _servisService.MevcutServisYoklamaGetir(servisId, periyot);
                return Ok(yoklama);
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "Servis yoklama bilgisi alınırken bir hata oluştu." });
            }
        }

        /// <summary>
        /// Servis yoklamasını toplu olarak kaydeder.
        /// KullaniciId body'den değil, JWT token'dan alınır.
        /// </summary>
        [HttpPost("yoklama-kaydet")]
        public async Task<IActionResult> ServisYoklamaKaydet([FromBody] ServisYoklamaKaydetDto model)
        {
            var rol = User.FindFirst("rol")?.Value;
            if (rol != "Servis")
                return Forbid();

            var servisIdStr = User.FindFirst("servisId")?.Value;
            if (!int.TryParse(servisIdStr, out var tokenServisId))
                return Unauthorized("Oturum bilgileri eksik.");

            if (model.Kayitlar == null || model.Kayitlar.Count == 0)
                return BadRequest(new { error = "Yoklama kaydı listesi boş olamaz." });

            try
            {
                var kayitlar = model.Kayitlar.Select(k => (k.OgrenciId, k.DurumId)).ToList();

                await _servisService.ServisYoklamaKaydet(
                    kayitlar,
                    tokenServisId,
                    model.Periyot
                );

                // SMS bildirimini arka planda gönder
                // Tenant bilgileri istek scope'undan snapshot alınır; arka plan scope'unda
                // TenantBaglami otomatik doldurulmadığı için manuel set edilir.
                var tenantSnapshot = (
                    OkulKodu: _tenantBaglami.OkulKodu,
                    ConnectionString: _tenantBaglami.ConnectionString,
                    OkulAdi: _tenantBaglami.OkulAdi
                );

                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var tenant = scope.ServiceProvider.GetRequiredService<TenantBaglami>();
                        tenant.OkulKodu = tenantSnapshot.OkulKodu;
                        tenant.ConnectionString = tenantSnapshot.ConnectionString;
                        tenant.OkulAdi = tenantSnapshot.OkulAdi;

                        var smsBildirim = scope.ServiceProvider.GetRequiredService<IYoklamaSmsBildirimService>();
                        await smsBildirim.ServisYoklamaBildir(kayitlar, model.Periyot);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Servis yoklama SMS gönderim hatası.");
                    }
                });

                return Ok(new { message = "Servis yoklaması başarıyla kaydedildi." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "Servis yoklaması kaydedilirken bir hata oluştu." });
            }
        }

        /// <summary>
        /// Bir öğrencinin servis yoklama geçmişini tarih aralığına göre getirir.
        /// Veli sadece kendi çocuğunu, Servis sadece kendi servis öğrencisini görebilir;
        /// Admin/GenelAdmin/Öğretmen tüm öğrencileri görebilir.
        /// </summary>
        [HttpGet("ogrenci/{ogrenciId}/yoklama-gecmisi")]
        public async Task<IActionResult> OgrenciYoklamaGecmisi(
            int ogrenciId,
            [FromQuery] DateTime? baslangic,
            [FromQuery] DateTime? bitis)
        {
            if (baslangic.HasValue && bitis.HasValue && baslangic > bitis)
                return BadRequest(new { error = "Başlangıç tarihi bitiş tarihinden sonra olamaz." });

            try
            {
                var ogrenci = await _ogrenciService.OgrenciGetirAsync(ogrenciId);
                if (ogrenci is null)
                    return NotFound(new { message = $"{ogrenciId} numaralı öğrenci bulunamadı." });

                var rol = User.FindFirst("rol")?.Value;
                if (rol == "Veli")
                {
                    if (!int.TryParse(User.FindFirst("veliId")?.Value, out var veliId) || ogrenci.VeliId != veliId)
                        return Forbid();
                }
                else if (rol == "Servis")
                {
                    if (!int.TryParse(User.FindFirst("servisId")?.Value, out var servisId) || ogrenci.ServisId != servisId)
                        return Forbid();
                }

                var kayitlar = await _servisService.OgrenciYoklamaGecmisiGetir(ogrenciId, baslangic, bitis);
                return Ok(kayitlar);
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "Servis yoklama geçmişi alınırken bir hata oluştu." });
            }
        }

        /// <summary>
        /// Servis rolü ve servisId eşleşmesini kontrol eder.
        /// </summary>
        private IActionResult? ServisErisimKontrol(int servisId)
        {
            var rol = User.FindFirst("rol")?.Value;
            if (rol != "Servis")
                return Forbid();

            var servisIdStr = User.FindFirst("servisId")?.Value;
            if (!int.TryParse(servisIdStr, out var tokenServisId) || tokenServisId != servisId)
                return Forbid();

            return null;
        }
    }
}
