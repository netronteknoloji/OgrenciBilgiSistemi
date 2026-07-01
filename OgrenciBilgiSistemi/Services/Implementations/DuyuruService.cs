using Microsoft.EntityFrameworkCore;
using OgrenciBilgiSistemi.Data;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Services.Interfaces;

namespace OgrenciBilgiSistemi.Services.Implementations
{
    public class DuyuruService : IDuyuruService
    {
        private readonly AppDbContext _db;
        private readonly IBildirimService _bildirimService;
        private readonly ILogger<DuyuruService> _logger;
        private readonly TimeProvider _timeProvider;

        public DuyuruService(AppDbContext db, IBildirimService bildirimService, ILogger<DuyuruService> logger, TimeProvider timeProvider)
        {
            _db = db;
            _bildirimService = bildirimService;
            _logger = logger;
            _timeProvider = timeProvider;
        }

        public async Task<int> Olustur(int olusturanId, DuyuruHedefi hedef, string baslik, string icerik, CancellationToken ct = default)
        {
            var duyuru = new DuyuruModel
            {
                OlusturanKullaniciId = olusturanId,
                Hedef = hedef,
                Baslik = baslik.Trim(),
                Icerik = icerik.Trim(),
                OlusturulmaTarihi = _timeProvider.GetLocalNow().DateTime
            };

            _db.Duyurular.Add(duyuru);
            await _db.SaveChangesAsync(ct);

            var hedefVeliler = await HedefVelileriBul(olusturanId, hedef, ct);
            foreach (var veliId in hedefVeliler)
            {
                await _bildirimService.Olustur(veliId, BildirimTuru.DuyuruYayinlandi,
                    $"Yeni duyuru: {duyuru.Baslik}", randevuId: null, ct);
            }

            return duyuru.DuyuruId;
        }

        public async Task<SayfalanmisListeModel<DuyuruModel>> Listele(int sayfaNo, int sayfaBoyutu = 20, CancellationToken ct = default)
        {
            var query = _db.Duyurular
                .AsNoTracking()
                .Include(d => d.Olusturan)
                .OrderByDescending(d => d.OlusturulmaTarihi);

            return await SayfalanmisListeModel<DuyuruModel>.CreateAsync(query, sayfaNo, sayfaBoyutu, ct);
        }

        public async Task<DuyuruModel?> IdIleGetir(int duyuruId, CancellationToken ct = default)
        {
            return await _db.Duyurular
                .AsNoTracking()
                .Include(d => d.Olusturan)
                .FirstOrDefaultAsync(d => d.DuyuruId == duyuruId, ct);
        }

        public async Task Sil(int duyuruId, CancellationToken ct = default)
        {
            var duyuru = await _db.Duyurular.FindAsync(new object[] { duyuruId }, ct)
                         ?? throw new KeyNotFoundException("Duyuru bulunamadı.");

            duyuru.IsDeleted = true;
            await _db.SaveChangesAsync(ct);
        }

        private async Task<List<int>> HedefVelileriBul(int olusturanId, DuyuruHedefi hedef, CancellationToken ct)
        {
            if (hedef == DuyuruHedefi.OgretmenKendiOgrencileri)
            {
                // Öğretmen-öğrenci eşlemesi OgretmenProfiller.BirimId = Ogrenciler.BirimId üzerinden.
                return await (from o in _db.Ogrenciler
                              join op in _db.OgretmenProfiller on o.BirimId equals op.BirimId
                              where op.KullaniciId == olusturanId
                                    && !op.IsDeleted
                                    && !o.IsDeleted
                                    && o.VeliId != null
                                    && o.Veli != null
                                    && !o.Veli.IsDeleted
                              select o.VeliId!.Value)
                              .Distinct()
                              .ToListAsync(ct);
            }

            return await _db.Kullanicilar
                .Where(k => k.Rol == KullaniciRolu.Veli && !k.IsDeleted)
                .Select(k => k.KullaniciId)
                .ToListAsync(ct);
        }
    }
}
