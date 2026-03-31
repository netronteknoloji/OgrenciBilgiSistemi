using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OgrenciBilgiSistemi.Data;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Services.Interfaces;

namespace OgrenciBilgiSistemi.Services.Implementations
{
    public sealed class KitapService : IKitapService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<KitapService> _logger;
        private readonly IWebHostEnvironment _env;

        public KitapService(
            AppDbContext db,
            ILogger<KitapService> logger,
            IWebHostEnvironment env)
        {
            _db = db;
            _logger = logger;
            _env = env;
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
                .Where(k => k.KitapDurum);

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
                q = q.Where(k => k.KitapDurum);

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
            if (gorsel != null)
            {
                model.KitapGorsel = await UploadImageAsync(gorsel, null, ct);
            }

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
            kitap.KitapDurum = model.KitapDurum;

            if (gorsel != null)
            {
                kitap.KitapGorsel = await UploadImageAsync(gorsel, kitap.KitapGorsel, ct);
            }

            await _db.SaveChangesAsync(ct);
        }

        public async Task SoftDeleteAsync(int id, CancellationToken ct = default)
        {
            var kitap = await _db.Kitaplar.FirstOrDefaultAsync(k => k.KitapId == id, ct);
            if (kitap == null)
                throw new InvalidOperationException("Kitap bulunamadı.");

            kitap.KitapDurum = false;
            await _db.SaveChangesAsync(ct);
        }

        public async Task<KitapModel?> GetByIdAsync(int id, bool tumKitaplar = false, CancellationToken ct = default)
        {
            var q = _db.Kitaplar.AsNoTracking().AsQueryable();

            if (!tumKitaplar)
                q = q.Where(k => k.KitapDurum);

            return await q.FirstOrDefaultAsync(k => k.KitapId == id, ct);
        }

        private async Task<string> UploadImageAsync(IFormFile file, string? existingPath, CancellationToken ct)
        {
            if (file == null || file.Length == 0)
                return existingPath ?? string.Empty;

            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // Eski görsel varsa sil
            if (!string.IsNullOrEmpty(existingPath))
            {
                var oldPhysicalPath = Path.Combine(_env.WebRootPath, existingPath.TrimStart('/'));
                if (File.Exists(oldPhysicalPath))
                {
                    try
                    {
                        File.Delete(oldPhysicalPath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Eski kitap görseli silinemedi: {Path}", oldPhysicalPath);
                    }
                }
            }

            var fileName = Guid.NewGuid().ToString("N") + Path.GetExtension(file.FileName);
            var newPath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(newPath, FileMode.Create))
            {
                await file.CopyToAsync(stream, ct);
            }

            return "/uploads/" + fileName;
        }
    }
}
