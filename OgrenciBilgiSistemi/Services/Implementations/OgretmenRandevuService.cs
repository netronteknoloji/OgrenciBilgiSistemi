using Microsoft.EntityFrameworkCore;
using OgrenciBilgiSistemi.Data;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Services.Interfaces;

namespace OgrenciBilgiSistemi.Services.Implementations
{
    public class OgretmenRandevuService : IOgretmenRandevuService
    {
        private readonly AppDbContext _db;

        public OgretmenRandevuService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<OgretmenRandevuModel>> OgretmeneGoreListele(int ogretmenKullaniciId, CancellationToken ct = default)
        {
            return await _db.OgretmenRandevular
                .AsNoTracking()
                .Include(m => m.Ogretmen)
                .Where(m => m.OgretmenKullaniciId == ogretmenKullaniciId)
                .OrderBy(m => m.Tarih)
                .ThenBy(m => m.BaslangicSaati)
                .ToListAsync(ct);
        }

        public async Task<OgretmenRandevuModel?> Getir(int ogretmenRandevuId, CancellationToken ct = default)
        {
            return await _db.OgretmenRandevular
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.OgretmenRandevuId == ogretmenRandevuId, ct);
        }

        public async Task Ekle(OgretmenRandevuModel model, CancellationToken ct = default)
        {
            bool cakisma = await _db.OgretmenRandevular.AnyAsync(m =>
                m.OgretmenKullaniciId == model.OgretmenKullaniciId &&
                m.Tarih == model.Tarih.Date &&
                model.BaslangicSaati < m.BitisSaati &&
                model.BitisSaati > m.BaslangicSaati, ct);

            if (cakisma)
                throw new InvalidOperationException("Bu zaman aralığında zaten bir slot tanımlanmış.");

            _db.OgretmenRandevular.Add(model);
            await _db.SaveChangesAsync(ct);
        }

        public async Task Guncelle(OgretmenRandevuModel model, CancellationToken ct = default)
        {
            var ent = await _db.OgretmenRandevular.FindAsync(new object[] { model.OgretmenRandevuId }, ct)
                      ?? throw new KeyNotFoundException("Randevu takvimi bulunamadı.");

            bool cakisma = await _db.OgretmenRandevular.AnyAsync(m =>
                m.OgretmenRandevuId != model.OgretmenRandevuId &&
                m.OgretmenKullaniciId == model.OgretmenKullaniciId &&
                m.Tarih == model.Tarih.Date &&
                model.BaslangicSaati < m.BitisSaati &&
                model.BitisSaati > m.BaslangicSaati, ct);

            if (cakisma)
                throw new InvalidOperationException("Bu zaman aralığında zaten bir slot tanımlanmış.");

            ent.OgretmenKullaniciId = model.OgretmenKullaniciId;
            ent.Tarih               = model.Tarih;
            ent.BaslangicSaati      = model.BaslangicSaati;
            ent.BitisSaati          = model.BitisSaati;
            await _db.SaveChangesAsync(ct);
        }

        public async Task Sil(int ogretmenRandevuId, CancellationToken ct = default)
        {
            var ent = await _db.OgretmenRandevular.FindAsync(new object[] { ogretmenRandevuId }, ct)
                      ?? throw new KeyNotFoundException("Randevu takvimi bulunamadı.");

            ent.IsDeleted = true;
            await _db.SaveChangesAsync(ct);
        }
    }
}
