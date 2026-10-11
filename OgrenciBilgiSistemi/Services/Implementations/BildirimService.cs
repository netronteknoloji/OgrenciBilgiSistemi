using OgrenciBilgiSistemi.Data;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Services.Interfaces;

namespace OgrenciBilgiSistemi.Services.Implementations
{
    public class BildirimService : IBildirimService
    {
        private readonly AppDbContext _db;
        private readonly IGonderimKuyruguService _kuyruk;
        private readonly KuyrukTetikleyici _tetikleyici;
        private readonly TimeProvider _timeProvider;

        public BildirimService(
            AppDbContext db,
            IGonderimKuyruguService kuyruk,
            KuyrukTetikleyici tetikleyici,
            TimeProvider timeProvider)
        {
            _db = db;
            _kuyruk = kuyruk;
            _tetikleyici = tetikleyici;
            _timeProvider = timeProvider;
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
                OlusturulmaTarihi = _timeProvider.GetLocalNow().DateTime
            };

            // Bildirim ve push kuyruk satırı tek SaveChanges'te yazılır; push'u GonderimKuyruguIsleyici gönderir
            _db.Bildirimler.Add(bildirim);
            _kuyruk.PushEkle(bildirim);
            await _db.SaveChangesAsync(ct);
            _tetikleyici.Tetikle();
        }

    }
}
