using Microsoft.EntityFrameworkCore;
using OgrenciBilgiSistemi.Data;

namespace OgrenciBilgiSistemi.Helpers
{
    /// <summary>
    /// Ogrenciler.OgretmenId türetilmiş bir alandır: öğrencinin birimindeki ilk aktif öğretmen.
    /// Öğretmenin birimi, rolü veya durumu değiştiğinde ilgili öğrencilerde yeniden hesaplanır.
    /// SaveChanges'ten SONRA çağrılmalıdır (DB'deki güncel profil durumunu okur).
    /// </summary>
    public static class OgretmenAtamaHelper
    {
        public static async Task OgretmenDegistiAsync(AppDbContext db, int ogretmenKullaniciId, IEnumerable<int?> etkilenenBirimler, CancellationToken ct = default)
        {
            // Bu öğretmene bağlı görünen öğrencilerin birimleri de yeniden hesaplanır (bayat kayıtlar)
            var bagliBirimler = await db.Ogrenciler
                .IgnoreQueryFilters()
                .Where(o => o.OgretmenId == ogretmenKullaniciId && o.BirimId != null)
                .Select(o => o.BirimId)
                .Distinct()
                .ToListAsync(ct);

            foreach (var birimId in etkilenenBirimler.Concat(bagliBirimler).Where(b => b.HasValue).Distinct())
                await BiriminOgretmeniniGuncelleAsync(db, birimId!.Value, ct);

            // Birimi olmayan öğrencide öğretmen olamaz
            await db.Ogrenciler
                .IgnoreQueryFilters()
                .Where(o => o.OgretmenId == ogretmenKullaniciId && o.BirimId == null)
                .ExecuteUpdateAsync(s => s.SetProperty(o => o.OgretmenId, (int?)null), ct);
        }

        private static async Task BiriminOgretmeniniGuncelleAsync(AppDbContext db, int birimId, CancellationToken ct)
        {
            var ogretmenId = await db.OgretmenProfiller
                .IgnoreQueryFilters()
                .Where(op => op.BirimId == birimId
                             && !op.IsDeleted
                             && !op.Kullanici.IsDeleted
                             && op.Kullanici.Rol == KullaniciRolu.Ogretmen)
                .OrderBy(op => op.KullaniciId)
                .Select(op => (int?)op.KullaniciId)
                .FirstOrDefaultAsync(ct);

            await db.Ogrenciler
                .IgnoreQueryFilters()
                .Where(o => o.BirimId == birimId && o.OgretmenId != ogretmenId)
                .ExecuteUpdateAsync(s => s.SetProperty(o => o.OgretmenId, ogretmenId), ct);
        }
    }
}
