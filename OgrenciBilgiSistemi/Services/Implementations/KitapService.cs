using OgrenciBilgiSistemi.Abstractions;
using Microsoft.EntityFrameworkCore;
using OgrenciBilgiSistemi.Data;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Services.Interfaces;

namespace OgrenciBilgiSistemi.Services.Implementations
{
    public sealed class KitapService : IKitapService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<KitapService> _logger;
        private readonly IFileStorage _files;

        public KitapService(
            AppDbContext db,
            ILogger<KitapService> logger,
            IFileStorage files)
        {
            _db = db;
            _logger = logger;
            _files = files;
        }

        // ======================
        // LIST & SEARCH
        // ======================
        public async Task<SayfalanmisListeModel<KitapModel>> SearchPagedAsync(
            string? sortOrder,
            string? searchString,
            int pageIndex,
            int pageSize,
            CancellationToken ct = default)
        {
            var q = _db.Kitaplar
                .AsNoTracking()
                .Where(k => !k.IsDeleted);

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var s = searchString.Trim();
                q = q.Where(p =>
                    EF.Functions.Like(p.KitapAd, $"%{s}%") ||
                    (p.KitapTurAd != null && EF.Functions.Like(p.KitapTurAd, $"%{s}%"))
                );
            }

            q = sortOrder switch
            {
                "Ad_desc" => q.OrderByDescending(p => p.KitapAd),
                "Ad" => q.OrderBy(p => p.KitapAd),
                _ => q.OrderBy(p => p.KitapAd)
            };

            if (pageIndex < 1) pageIndex = 1;

            return await SayfalanmisListeModel<KitapModel>.CreateAsync(q, pageIndex, pageSize, ct);
        }

        public async Task<List<KitapModel>> GetFilteredListAsync(
            string? sortOrder,
            string? searchString,
            bool onlyActive = true,
            CancellationToken ct = default)
        {
            var q = _db.Kitaplar
                .AsNoTracking()
                .AsQueryable();

            if (onlyActive)
                q = q.Where(k => !k.IsDeleted);

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var s = searchString.Trim();
                q = q.Where(p =>
                    EF.Functions.Like(p.KitapAd, $"%{s}%") ||
                    (p.KitapTurAd != null && EF.Functions.Like(p.KitapTurAd, $"%{s}%"))
                );
            }

            q = sortOrder switch
            {
                "Ad_desc" => q.OrderByDescending(p => p.KitapAd),
                "Ad" => q.OrderBy(p => p.KitapAd),
                _ => q.OrderBy(p => p.KitapAd)
            };

            return await q.ToListAsync(ct);
        }

        // ======================
        // CRUD
        // ======================
        public async Task<int> AddAsync(KitapModel model, IFormFile? gorsel, CancellationToken ct = default)
        {
            // Görsel yolu yalnızca yüklenen dosyadan gelir; formdan gelen değer kullanılmaz
            model.KitapGorsel = gorsel != null ? await _files.SaveImageAsync(gorsel, null, ct) : null;

            _db.Kitaplar.Add(model);
            await _db.SaveChangesAsync(ct);
            return model.KitapId;
        }

        public async Task UpdateAsync(KitapModel model, IFormFile? gorsel, CancellationToken ct = default)
        {
            var kitap = await _db.Kitaplar.FirstOrDefaultAsync(k => k.KitapId == model.KitapId, ct);
            if (kitap == null)
                throw new InvalidOperationException("Kitap bulunamadı.");

            kitap.KitapAd = model.KitapAd;
            kitap.KitapTurAd = model.KitapTurAd;
            kitap.KitapGun = model.KitapGun;
            kitap.IsDeleted = model.IsDeleted;

            if (gorsel != null)
            {
                kitap.KitapGorsel = await _files.SaveImageAsync(gorsel, kitap.KitapGorsel, ct);
            }

            await _db.SaveChangesAsync(ct);
        }

        public async Task SoftDeleteAsync(int id, CancellationToken ct = default)
        {
            var kitap = await _db.Kitaplar.FirstOrDefaultAsync(k => k.KitapId == id, ct);
            if (kitap == null)
                throw new InvalidOperationException("Kitap bulunamadı.");

            kitap.IsDeleted = true;
            await _db.SaveChangesAsync(ct);
        }

        public async Task<KitapModel?> GetByIdAsync(int id, bool tumKitaplar = false, CancellationToken ct = default)
        {
            var q = _db.Kitaplar.AsNoTracking().AsQueryable();

            if (!tumKitaplar)
                q = q.Where(k => !k.IsDeleted);

            return await q.FirstOrDefaultAsync(k => k.KitapId == id, ct);
        }

    }
}
