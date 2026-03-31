using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OgrenciBilgiSistemi.Data;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Services.Interfaces;

namespace OgrenciBilgiSistemi.Services.Implementations
{
    public sealed class KitapDetayService : IKitapDetayService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<KitapDetayService> _logger;

        public KitapDetayService(AppDbContext db, ILogger<KitapDetayService> logger)
        {
            _db = db;
            _logger = logger;
        }

        // ======================================
        // PAGINATED SEARCH
        // ======================================
        public async Task<SayfalanmisListeModel<KitapDetayModel>> SearchPagedAsync(
            string? sortOrder,
            string? searchString,
            string? durumFilter,
            int pageIndex,
            int pageSize,
            CancellationToken ct = default)
        {
            if (pageIndex < 1) pageIndex = 1;

            var q = _db.KitapDetaylar
                .AsNoTracking()
                .Include(kd => kd.Kitap)
                .Include(kd => kd.Ogrenci)
                .AsQueryable();

            // Arama
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var s = searchString.Trim();
                q = q.Where(kd =>
                    EF.Functions.Like(kd.Kitap!.KitapAd, $"%{s}%") ||
                    EF.Functions.Like(kd.Ogrenci!.OgrenciAdSoyad, $"%{s}%")
                );
            }

            // Durum
            if (!string.IsNullOrEmpty(durumFilter))
            {
                if (durumFilter == "Alındı")
                    q = q.Where(kd => kd.KitapDurum == KitapDurumu.Alındı);
                else if (durumFilter == "TeslimEdildi")
                    q = q.Where(kd => kd.KitapDurum == KitapDurumu.TeslimEdildi);
            }

            // Sıralama
            q = sortOrder switch
            {
                "kitap_desc" => q.OrderByDescending(kd => kd.Kitap!.KitapAd),
                "ogrenci" => q.OrderBy(kd => kd.Ogrenci!.OgrenciAdSoyad),
                "ogrenci_desc" => q.OrderByDescending(kd => kd.Ogrenci!.OgrenciAdSoyad),
                "tarih" => q.OrderBy(kd => kd.KitapAlTarih),
                "tarih_desc" => q.OrderByDescending(kd => kd.KitapAlTarih),
                _ => q.OrderBy(kd => kd.Kitap!.KitapAd)
                                   .ThenBy(kd => kd.Ogrenci!.OgrenciAdSoyad)
            };

            return await SayfalanmisListeModel<KitapDetayModel>.CreateAsync(q, pageIndex, pageSize, ct);
        }

        // ======================================
        // FILTERED FULL LIST (Excel, rapor)
        // ======================================
        public async Task<IReadOnlyList<KitapDetayModel>> GetFilteredListAsync(
            string? sortOrder,
            string? searchString,
            string? durumFilter,
            CancellationToken ct = default)
        {
            var q = _db.KitapDetaylar
                .Include(kd => kd.Kitap)
                .Include(kd => kd.Ogrenci)
                .AsQueryable();

            // Arama
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var s = searchString.Trim();
                q = q.Where(kd =>
                    EF.Functions.Like(kd.Kitap!.KitapAd, $"%{s}%") ||
                    EF.Functions.Like(kd.Ogrenci!.OgrenciAdSoyad, $"%{s}%")
                );
            }

            // Durum
            if (!string.IsNullOrEmpty(durumFilter))
            {
                if (durumFilter == "Alındı")
                    q = q.Where(kd => kd.KitapDurum == KitapDurumu.Alındı);
                else if (durumFilter == "TeslimEdildi")
                    q = q.Where(kd => kd.KitapDurum == KitapDurumu.TeslimEdildi);
            }

            // Sıralama
            q = sortOrder switch
            {
                "kitap_desc" => q.OrderByDescending(kd => kd.Kitap!.KitapAd),
                "ogrenci" => q.OrderBy(kd => kd.Ogrenci!.OgrenciAdSoyad),
                "ogrenci_desc" => q.OrderByDescending(kd => kd.Ogrenci!.OgrenciAdSoyad),
                "tarih" => q.OrderBy(kd => kd.KitapAlTarih),
                "tarih_desc" => q.OrderByDescending(kd => kd.KitapAlTarih),
                _ => q.OrderBy(kd => kd.Kitap!.KitapAd)
                                   .ThenBy(kd => kd.Ogrenci!.OgrenciAdSoyad)
            };

            return await q.ToListAsync(ct);
        }

        // ======================================
        // GET BY ID
        // ======================================
        public async Task<KitapDetayModel?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            return await _db.KitapDetaylar
                .Include(kd => kd.Kitap)
                .Include(kd => kd.Ogrenci)
                .FirstOrDefaultAsync(kd => kd.KitapDetayId == id, ct);
        }

        // ======================================
        // ADD
        // ======================================
        public async Task<int> AddAsync(KitapDetayModel model, CancellationToken ct = default)
        {
            // Kitap başkasında mı?
            var aktifMi = await _db.KitapDetaylar
                .AnyAsync(kd => kd.KitapId == model.KitapId &&
                                kd.KitapDurum == KitapDurumu.Alındı, ct);

            if (aktifMi)
                throw new InvalidOperationException("Bu kitap şu anda başka bir öğrencide.");

            var entity = new KitapDetayModel
            {
                KitapId = model.KitapId,
                OgrenciId = model.OgrenciId,
                KitapAlTarih = model.KitapAlTarih == default ? DateTime.Now : model.KitapAlTarih,
                KitapDurum = KitapDurumu.Alındı,
                KitapVerTarih = null
            };

            _db.KitapDetaylar.Add(entity);
            await _db.SaveChangesAsync(ct);
            return entity.KitapDetayId;
        }

        // ======================================
        // UPDATE
        // ======================================
        public async Task UpdateAsync(KitapDetayModel model, CancellationToken ct = default)
        {
            var detay = await _db.KitapDetaylar
                .FirstOrDefaultAsync(kd => kd.KitapDetayId == model.KitapDetayId, ct);

            if (detay == null)
                throw new InvalidOperationException("Kitap detayı bulunamadı.");

            // Kitap değişti ve yeni kitap başkasında mı?
            if (detay.KitapId != model.KitapId &&
                model.KitapDurum == KitapDurumu.Alındı)
            {
                var aktifMi = await _db.KitapDetaylar
                    .AnyAsync(kd => kd.KitapId == model.KitapId &&
                                    kd.KitapDurum == KitapDurumu.Alındı, ct);

                if (aktifMi)
                    throw new InvalidOperationException("Seçilen kitap şu anda başka bir öğrencide.");
            }

            detay.KitapId = model.KitapId;
            detay.OgrenciId = model.OgrenciId;
            detay.KitapAlTarih = model.KitapAlTarih;
            detay.KitapDurum = model.KitapDurum;

            if (detay.KitapDurum == KitapDurumu.TeslimEdildi &&
                model.KitapVerTarih == null)
                detay.KitapVerTarih = DateTime.Now;
            else
                detay.KitapVerTarih = model.KitapVerTarih;

            await _db.SaveChangesAsync(ct);
        }

        // ======================================
        // TESLİM AL
        // ======================================
        public async Task TeslimAlAsync(int id, CancellationToken ct = default)
        {
            var detay = await _db.KitapDetaylar
                .FirstOrDefaultAsync(kd => kd.KitapDetayId == id, ct);

            if (detay == null)
                throw new InvalidOperationException("Kitap detayı bulunamadı.");

            detay.KitapDurum = KitapDurumu.TeslimEdildi;

            if (detay.KitapVerTarih == null)
                detay.KitapVerTarih = DateTime.Now;

            await _db.SaveChangesAsync(ct);
        }

        // ======================================
        // SELECT LIST
        // ======================================
        public async Task<IReadOnlyList<SelectListItem>> GetKitapSelectListAsync(CancellationToken ct = default)
        {
            return await _db.Kitaplar
                .AsNoTracking()
                .Where(k => k.KitapDurum)
                .OrderBy(k => k.KitapAd)
                .Select(k => new SelectListItem
                {
                    Value = k.KitapId.ToString(),
                    Text = k.KitapAd
                })
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<SelectListItem>> GetOgrenciSelectListAsync(CancellationToken ct = default)
        {
            return await _db.Ogrenciler
                .AsNoTracking()
                .Where(o => o.OgrenciDurum)
                .OrderBy(o => o.OgrenciAdSoyad)
                .Select(o => new SelectListItem
                {
                    Value = o.OgrenciId.ToString(),
                    Text = o.OgrenciAdSoyad
                })
                .ToListAsync(ct);
        }
    }
}
