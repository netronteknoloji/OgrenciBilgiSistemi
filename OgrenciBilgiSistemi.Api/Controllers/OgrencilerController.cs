using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OgrenciBilgiSistemi.Api.Dtos;
using OgrenciBilgiSistemi.Api.Services.Interfaces;
using OgrenciBilgiSistemi.Shared.Services;

namespace OgrenciBilgiSistemi.Api.Controllers
{
    [Route("api/ogrenciler")]
    [ApiController]
    [Authorize]
    public class OgrencilerController : ControllerBase
    {
        private readonly IOgrenciService _ogrenciService;
        private readonly IServisService _servisService;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly TenantBaglami _tenantBaglami;
        private readonly ILogger<OgrencilerController> _logger;

        public OgrencilerController(
            IOgrenciService ogrenciService,
            IServisService servisService,
            IServiceScopeFactory scopeFactory,
            TenantBaglami tenantBaglami,
            ILogger<OgrencilerController> logger)
        {
            _ogrenciService = ogrenciService;
            _servisService = servisService;
            _scopeFactory = scopeFactory;
            _tenantBaglami = tenantBaglami;
            _logger = logger;
        }

        #region Rol Bazlı Öğrenci Metotları

        // Rol bazlı: Giriş yapan kullanıcıya ait öğrencileri getirir
        // Öğretmen → tüm sınıflar, Servis → kendi servisi, Veli → kendi çocukları
        [HttpGet("benim")]
        public async Task<IActionResult> BenimOgrencilerim()
        {
            try
            {
                var rol = User.FindFirst("rol")?.Value;
                var kullaniciId = User.FindFirst("kullaniciId")?.Value;

                if (string.IsNullOrEmpty(rol) || string.IsNullOrEmpty(kullaniciId))
                    return Unauthorized("Oturum bilgileri eksik.");

                switch (rol)
                {
                    case "Veli":
                        var veliIdStr = User.FindFirst("veliId")?.Value;
                        if (string.IsNullOrEmpty(veliIdStr) || !int.TryParse(veliIdStr, out var veliId))
                            return BadRequest("Veli bilgisi bulunamadı.");
                        var cocuklar = await _ogrenciService.VeliyeGoreOgrencileriGetirAsync(veliId);
                        return Ok(cocuklar);

                    case "Servis":
                        var servisIdStr = User.FindFirst("servisId")?.Value;
                        if (string.IsNullOrEmpty(servisIdStr) || !int.TryParse(servisIdStr, out var servisId))
                            return BadRequest("Servis bilgisi bulunamadı.");
                        var servisOgrencileri = await _servisService.ServisOgrencileriGetir(servisId);
                        return Ok(servisOgrencileri);

                    default:
                        // Öğretmen: tüm sınıfları görebilir, bu endpoint sınıf listesine yönlendirir
                        return Ok(new { mesaj = "Öğretmenler sınıf listesinden öğrenci görüntüler.", rol });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Öğrenci listesi alınırken hata.");
                return StatusCode(500, new { error = "Öğrenci listesi alınırken bir hata oluştu." });
            }
        }

        #endregion

        #region Öğrenci Bilgi Metotları

        [HttpGet("tumu")]
        public async Task<IActionResult> TumOgrencileriGetir()
        {
            var rol = User.FindFirst("rol")?.Value;
            if (rol != "Ogretmen" && rol != "Admin" && rol != "GenelAdmin")
                return Forbid();

            try
            {
                var ogrenciler = await _ogrenciService.TumAktifOgrencileriGetirAsync();
                return Ok(ogrenciler);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Öğrenci listesi alınırken hata.");
                return StatusCode(500, new { error = "Öğrenci listesi alınırken bir hata oluştu." });
            }
        }

        /// <summary>
        /// Bir sınıfın belirli bir tarihteki tüm öğrenci yoklama özetini döner.
        /// Her öğrenci için 8 ders durumu ve yoklamayı yapan öğretmen bilgisi.
        /// </summary>
        [HttpGet("sinif-yoklama/{sinifId}")]
        public async Task<IActionResult> SinifYoklamaOzeti(int sinifId, [FromQuery] DateTime? tarih)
        {
            var rol = User.FindFirst("rol")?.Value;
            if (rol != "Ogretmen" && rol != "Admin" && rol != "GenelAdmin")
                return Forbid();

            try
            {
                var seciliTarih = tarih?.Date ?? DateTime.Today;
                var ozet = await _ogrenciService.SinifYoklamaOzetiGetirAsync(sinifId, seciliTarih);
                return Ok(ozet);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sınıf yoklama özeti alınırken hata. SinifId: {SinifId}", sinifId);
                return StatusCode(500, new { error = "Sınıf yoklama özeti alınırken bir hata oluştu." });
            }
        }

        // 1. Sınıf ID'sine göre öğrenci listesini getirir — öğretmen veya admin
        [HttpGet("class/{sinifId}")]
        public async Task<IActionResult> SinifaGoreGetir(int sinifId)
        {
            var rol = User.FindFirst("rol")?.Value;
            if (rol != "Ogretmen" && rol != "Admin" && rol != "GenelAdmin")
                return Forbid();

            try
            {
                var ogrenciler = await _ogrenciService.SinifaGoreOgrencileriGetirAsync(sinifId);
                return Ok(ogrenciler);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Öğrenci listesi alınırken hata.");
                return StatusCode(500, new { error = "Öğrenci listesi alınırken bir hata oluştu." });
            }
        }

        // 2. ID'ye göre tek öğrenci getirir
        [HttpGet("{id}")]
        public async Task<IActionResult> IdIleGetir(int id)
        {
            try
            {
                var ogrenci = await _ogrenciService.OgrenciGetirAsync(id);
                if (ogrenci is null)
                    return NotFound(new { message = $"{id} numaralı öğrenci bulunamadı." });

                var yetkiSonucu = OgrenciyeErisimKontrol(ogrenci.VeliId, ogrenci.ServisId);
                if (yetkiSonucu != null) return yetkiSonucu;

                return Ok(ogrenci);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Öğrenci bilgisi alınırken hata. Id: {Id}", id);
                return StatusCode(500, new { error = "Öğrenci bilgisi alınırken bir hata oluştu." });
            }
        }

        // 3. Öğrencinin tüm detaylarını (Veli, Servis vb.) getirir
        [HttpGet("{id}/details")]
        public async Task<IActionResult> DetayGetir(int id)
        {
            try
            {
                var detaylar = await _ogrenciService.OgrenciDetayGetirAsync(id);
                if (detaylar is null)
                    return NotFound(new { message = $"{id} numaralı öğrenci bulunamadı." });

                var yetkiSonucu = OgrenciyeErisimKontrol(detaylar.VeliId, detaylar.ServisId);
                if (yetkiSonucu != null) return yetkiSonucu;

                return Ok(detaylar);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Öğrenci detayları alınırken hata. Id: {Id}", id);
                return StatusCode(500, new { error = "Öğrenci detayları alınırken bir hata oluştu." });
            }
        }

        #endregion

        #region Öğrenci CRUD (Admin Only)

        // 4. Yeni öğrenci ekler — yalnızca admin
        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Ekle([FromBody] OgrenciKaydetDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.OgrenciAdSoyad))
                return BadRequest(new { error = "OgrenciAdSoyad boş olamaz." });

            if (dto.OgrenciNo <= 0)
                return BadRequest(new { error = "OgrenciNo sıfırdan büyük olmalıdır." });

            try
            {
                int yeniId = await _ogrenciService.EkleAsync(dto);
                return CreatedAtAction(nameof(IdIleGetir), new { id = yeniId }, new { ogrenciId = yeniId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Öğrenci eklenirken hata.");
                return StatusCode(500, new { error = "Öğrenci eklenirken bir hata oluştu." });
            }
        }

        // 5. Mevcut öğrenciyi günceller — yalnızca admin
        [HttpPut("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Guncelle(int id, [FromBody] OgrenciKaydetDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.OgrenciAdSoyad))
                return BadRequest(new { error = "OgrenciAdSoyad boş olamaz." });

            if (dto.OgrenciNo <= 0)
                return BadRequest(new { error = "OgrenciNo sıfırdan büyük olmalıdır." });

            try
            {
                bool basarili = await _ogrenciService.GuncelleAsync(id, dto);
                if (!basarili)
                    return NotFound(new { message = $"{id} numaralı öğrenci bulunamadı." });
                return Ok(new { message = "Öğrenci başarıyla güncellendi." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Öğrenci güncellenirken hata. Id: {Id}", id);
                return StatusCode(500, new { error = "Öğrenci güncellenirken bir hata oluştu." });
            }
        }

        // 6. Öğrenciyi pasife alır (soft-delete) — yalnızca admin
        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Sil(int id)
        {
            try
            {
                bool basarili = await _ogrenciService.SilAsync(id);
                if (!basarili)
                    return NotFound(new { message = $"{id} numaralı öğrenci bulunamadı." });
                return Ok(new { message = "Öğrenci pasife alındı." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Öğrenci silinirken hata. Id: {Id}", id);
                return StatusCode(500, new { error = "Öğrenci silinirken bir hata oluştu." });
            }
        }

        #endregion

        #region Yoklama Metotları

        // 7. Öğrencinin haftalık yoklama geçmişini getirir
        [HttpGet("{id}/haftalik-yoklama")]
        public async Task<IActionResult> HaftalikYoklamaGetir(int id, [FromQuery] DateTime baslangic, [FromQuery] DateTime bitis)
        {
            try
            {
                var ogrenci = await _ogrenciService.OgrenciGetirAsync(id);
                if (ogrenci is null)
                    return NotFound(new { message = $"{id} numaralı öğrenci bulunamadı." });

                var yetkiSonucu = OgrenciyeErisimKontrol(ogrenci.VeliId, ogrenci.ServisId);
                if (yetkiSonucu != null) return yetkiSonucu;

                var yoklamalar = await _ogrenciService.HaftalikYoklamaGetirAsync(id, baslangic, bitis);
                return Ok(yoklamalar);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Haftalık yoklama alınırken hata. Id: {Id}", id);
                return StatusCode(500, new { error = "Haftalık yoklama bilgisi alınırken bir hata oluştu." });
            }
        }

        // 8. Mevcut yoklama durumunu getirir (Dictionary döner) — yalnızca öğretmen
        [HttpGet("attendance/{sinifId}/{dersNumarasi}")]
        public async Task<IActionResult> YoklamaGetir(int sinifId, int dersNumarasi)
        {
            var rol = User.FindFirst("rol")?.Value;
            if (rol != "Ogretmen")
                return Forbid();

            try
            {
                var yoklama = await _ogrenciService.MevcutYoklamaGetirAsync(sinifId, dersNumarasi);
                return Ok(yoklama);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Yoklama bilgisi alınırken hata. SinifId: {SinifId}, Ders: {Ders}", sinifId, dersNumarasi);
                return StatusCode(500, new { error = "Yoklama bilgisi alınırken bir hata oluştu." });
            }
        }

        // 8. Toplu yoklama kaydetme (POST) — yalnızca öğretmen
        [HttpPost("attendance/save-bulk")]
        public async Task<IActionResult> TopluYoklamaKaydet([FromBody] TopluYoklamaGuncelleDto model)
        {
            var rol = User.FindFirst("rol")?.Value;
            if (rol != "Ogretmen")
                return Forbid();

            var kullaniciIdStr = User.FindFirst("kullaniciId")?.Value;
            if (!int.TryParse(kullaniciIdStr, out var tokenKullaniciId))
                return Unauthorized("Oturum bilgileri eksik.");

            if (model.Kayitlar == null || model.Kayitlar.Count == 0)
                return BadRequest(new { error = "Yoklama kaydı listesi boş olamaz." });

            try
            {
                var kayitlar = model.Kayitlar.Select(k => (k.OgrenciId, k.DurumId)).ToList();

                await _ogrenciService.TopluYoklamaKaydetAsync(
                    kayitlar,
                    model.SinifId,
                    tokenKullaniciId,
                    model.DersNumarasi
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
                        await smsBildirim.SinifYoklamaBildir(kayitlar, model.DersNumarasi);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Sınıf yoklama SMS gönderim hatası.");
                    }
                });

                return Ok(new { message = "Yoklama başarıyla kaydedildi." });
            }
            catch (ArgumentOutOfRangeException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Toplu yoklama kaydedilirken hata. SinifId: {SinifId}", model.SinifId);
                return StatusCode(500, new { error = "Yoklama kaydedilirken bir hata oluştu." });
            }
        }

        #endregion

        #region Yardımcı Metotlar

        /// <summary>
        /// Rol bazlı öğrenci erişim kontrolü.
        /// Veli sadece kendi çocuğunu, servis sadece kendi servisindeki öğrenciyi görebilir.
        /// Öğretmen tüm öğrencilere erişebilir.
        /// </summary>
        private IActionResult? OgrenciyeErisimKontrol(int? ogrenciVeliId, int? ogrenciServisId)
        {
            var rol = User.FindFirst("rol")?.Value;

            switch (rol)
            {
                case "Veli":
                    var veliIdStr = User.FindFirst("veliId")?.Value;
                    if (!int.TryParse(veliIdStr, out var veliId) || ogrenciVeliId != veliId)
                        return Forbid();
                    break;

                case "Servis":
                    var servisIdStr = User.FindFirst("servisId")?.Value;
                    if (!int.TryParse(servisIdStr, out var servisId) || ogrenciServisId != servisId)
                        return Forbid();
                    break;

                case "Ogretmen":
                case "Admin":
                case "GenelAdmin":
                    break; // Tüm öğrencilere erişebilir

                default:
                    return Forbid();
            }

            return null; // Erişim onaylandı
        }

        #endregion
    }
}
