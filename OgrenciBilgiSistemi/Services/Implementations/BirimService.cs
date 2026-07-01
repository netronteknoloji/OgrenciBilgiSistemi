using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OgrenciBilgiSistemi.Data;
using OgrenciBilgiSistemi.Dtos;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Services.Interfaces;

namespace OgrenciBilgiSistemi.Services.Implementations
{
    public sealed class BirimService : IBirimService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<BirimService> _logger;

        public BirimService(AppDbContext db, ILogger<BirimService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<IReadOnlyList<BirimDto>> GetAllAsync(bool? sinifMi = null, CancellationToken ct = default)
            => await _db.Birimler
                .AsNoTracking()
                .Where(b => !b.IsDeleted)
                .Where(b => !sinifMi.HasValue || b.BirimSinifMi == sinifMi.Value)
                .OrderBy(b => b.BirimAd)
                .Select(b => new BirimDto { Id = b.BirimId, Ad = b.BirimAd })
                .ToListAsync(ct);

        public async Task<List<SelectListItem>> GetSelectListAsync(
            int? selectedId = null,
            bool? sinifMi = null,
            BirimFiltre filtre = BirimFiltre.Aktif,
            CancellationToken ct = default)
        {
            var q = _db.Birimler.AsNoTracking().AsQueryable();

            // Durum filtresi (Aktif/Pasif/Tümü)
            if (filtre != BirimFiltre.Tum)
            {
                bool aktifMi = filtre == BirimFiltre.Aktif;
                q = q.Where(b => b.IsDeleted != aktifMi);
            }

            // Sınıf filtresi (opsiyonel)
            if (sinifMi.HasValue)
                q = q.Where(b => b.BirimSinifMi == sinifMi.Value);

            var items = await q
                .OrderBy(b => b.BirimAd)
                .Select(b => new SelectListItem
                {
                    Value = b.BirimId.ToString(),
                    Text = b.BirimAd,
                    Selected = selectedId.HasValue && b.BirimId == selectedId.Value
                })
                .ToListAsync(ct);

            var headerText = sinifMi == true ? "Tüm Sınıflar" : "Tüm Birimler";
            items.Insert(0, new SelectListItem
            {
                Value = "",
                Text = headerText,
                Selected = !selectedId.HasValue
            });

            return items;
        }

        public async Task<SayfalanmisListeModel<BirimModel>> SearchPagedAsync(
            string? searchString,
            int page,
            int pageSize,
            BirimFiltre filtre = BirimFiltre.Aktif,
            bool? sinifMi = null,
            CancellationToken ct = default)
        {
            var q = _db.Birimler.AsNoTracking().AsQueryable();

            q = filtre switch
            {
                BirimFiltre.Aktif => q.Where(p => !p.IsDeleted),
                BirimFiltre.Pasif => q.Where(p => p.IsDeleted),
                _ => q
            };

            if (sinifMi.HasValue)
                q = q.Where(b => b.BirimSinifMi == sinifMi.Value);

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var s = searchString.Trim();
                q = q.Where(b => EF.Functions.Like(b.BirimAd, $"%{s}%"));
            }

            q = q.OrderBy(b => b.BirimAd).ThenBy(b => b.BirimId);

            page = Math.Max(1, page);
            pageSize = Math.Max(1, pageSize);
            var total = await q.CountAsync(ct);
            var totalPages = total > 0 ? (int)Math.Ceiling(total / (double)pageSize) : 1;
            var safePage = Math.Min(page, totalPages);

            return await SayfalanmisListeModel<BirimModel>.CreateAsync(q, safePage, pageSize, ct);
        }

        // --- TEK KAYIT ---

        public Task<BirimModel?> GetByIdAsync(int id, bool tumBirimler = false, CancellationToken ct = default)
            => _db.Birimler.AsNoTracking()
               .FirstOrDefaultAsync(b => b.BirimId == id && (tumBirimler || !b.IsDeleted), ct);

        public async Task<bool> ExistsWithNameAsync(string ad, int? excludeId = null, CancellationToken ct = default)
        {
            ad = (ad ?? string.Empty).Trim();

            return await _db.Birimler.AnyAsync(b =>
                b.BirimAd.ToUpper() == ad.ToUpper() &&
                (!excludeId.HasValue || b.BirimId != excludeId.Value), ct);
        }

        // --- CRUD ---

        public async Task AddAsync(BirimModel model, CancellationToken ct = default)
        {
            model.BirimAd = (model.BirimAd ?? string.Empty).Trim();
            model.IsDeleted = false;
            try
            {
                _db.Birimler.Add(model);
                await _db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Birim eklenirken hata. Ad: {Ad}", model.BirimAd);
                throw;
            }
        }

        public async Task UpdateAsync(BirimModel model, CancellationToken ct = default)
        {
            var ent = await _db.Birimler.FirstOrDefaultAsync(b => b.BirimId == model.BirimId, ct)
                      ?? throw new KeyNotFoundException("Birim bulunamadı.");

            ent.BirimAd = (model.BirimAd ?? string.Empty).Trim();
            ent.IsDeleted = model.IsDeleted;
            ent.BirimSinifMi = model.BirimSinifMi;

            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Birim güncellenirken hata. Id: {Id}", model.BirimId);
                throw;
            }
        }

        public async Task DeleteAsync(int id, CancellationToken ct = default)
        {
            var ent = await _db.Birimler.FirstOrDefaultAsync(b => b.BirimId == id, ct);
            if (ent == null) return;

            ent.IsDeleted = true;
            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Birim silinirken hata. Id: {Id}", id);
                throw;
            }
        }
    }
}
