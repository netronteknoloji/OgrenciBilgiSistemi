using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OgrenciBilgiSistemi.Data;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Push;
using OgrenciBilgiSistemi.Services.Interfaces;
using OgrenciBilgiSistemi.Shared.Enums;
using OgrenciBilgiSistemi.Shared.Services;

namespace OgrenciBilgiSistemi.Services.Implementations
{
    public class BildirimService : IBildirimService
    {
        private readonly AppDbContext _db;
        private readonly IPushBildirimGonderici _pushGonderici;
        private readonly TenantBaglami _tenantBaglami;
        private readonly ILogger<BildirimService> _logger;

        public BildirimService(
            AppDbContext db,
            IPushBildirimGonderici pushGonderici,
            TenantBaglami tenantBaglami,
            ILogger<BildirimService> logger)
        {
            _db = db;
            _pushGonderici = pushGonderici;
            _tenantBaglami = tenantBaglami;
            _logger = logger;
        }

        public async Task Olustur(int aliciKullaniciId, BildirimTuru tur, string mesaj, int? randevuId = null, CancellationToken ct = default)
        {
            var bildirim = new BildirimModel
            {
                AliciKullaniciId = aliciKullaniciId,
                Tur = tur,
                Mesaj = mesaj,
                RandevuId = randevuId,
                Okundu = false,
                OlusturulmaTarihi = DateTime.Now
            };

            _db.Bildirimler.Add(bildirim);
            await _db.SaveChangesAsync(ct);

            try
            {
                var yuk = new PushBildirimYuku(
                    BildirimTuruBaslikHelper.BasligaCevir((int)tur),
                    mesaj,
                    new Dictionary<string, string>
                    {
                        ["bildirimId"] = bildirim.BildirimId.ToString(),
                        ["tur"] = ((int)tur).ToString(),
                        ["randevuId"] = randevuId?.ToString() ?? string.Empty,
                        ["okulKodu"] = _tenantBaglami.OkulKodu
                    });

                await _pushGonderici.GonderAsync(aliciKullaniciId, yuk, ct);
            }
            catch (Exception ex)
            {
                // Push hatası bildirimin DB kaydını invalide etmemeli
                _logger.LogError(ex, "Push gönderimi başarısız. BildirimId: {id}", bildirim.BildirimId);
            }
        }

        public async Task<int> OkunmamisSayisi(int kullaniciId, CancellationToken ct = default)
        {
            return await _db.Bildirimler
                .AsNoTracking()
                .Where(b => b.AliciKullaniciId == kullaniciId && !b.Okundu)
                .CountAsync(ct);
        }
    }
}
