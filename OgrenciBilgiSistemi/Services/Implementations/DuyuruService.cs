using Microsoft.EntityFrameworkCore;
using OgrenciBilgiSistemi.Data;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Services.Interfaces;
using OgrenciBilgiSistemi.Shared.Enums;

namespace OgrenciBilgiSistemi.Services.Implementations
{
    public class DuyuruService : IDuyuruService
    {
        private readonly AppDbContext _db;
        private readonly IBildirimService _bildirimService;

        public DuyuruService(AppDbContext db, IBildirimService bildirimService)
        {
            _db = db;
            _bildirimService = bildirimService;
        }

        public async Task<int> Olustur(int olusturanId, DuyuruHedefi hedef, string baslik, string icerik, CancellationToken ct = default)
        {
            var duyuru = new DuyuruModel
            {
                OlusturanKullaniciId = olusturanId,
                Hedef = hedef,
                Baslik = baslik.Trim(),
                Icerik = icerik.Trim(),
                OlusturulmaTarihi = DateTime.Now
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
                return await _db.Ogrenciler
                    .Where(o => o.OgretmenId == olusturanId
                                && o.OgrenciDurum
                                && o.VeliId != null
                                && o.Veli != null
                                && o.Veli.KullaniciDurum)
                    .Select(o => o.VeliId!.Value)
                    .Distinct()
                    .ToListAsync(ct);
            }

            return await _db.Kullanicilar
                .Where(k => k.Rol == KullaniciRolu.Veli && k.KullaniciDurum)
                .Select(k => k.KullaniciId)
                .ToListAsync(ct);
        }
    }
}
