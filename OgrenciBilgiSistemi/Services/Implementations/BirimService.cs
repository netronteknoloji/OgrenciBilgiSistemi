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

        public async Task<List<SelectListItem>> GetSelectListAsync(
            int? selectedId = null,
            bool? sinifMi = null,
            BirimFiltre filtre = BirimFiltre.Aktif,
            CancellationToken ct = default)
        {
            bool durumFiltreli = filtre != BirimFiltre.Tum;
            bool aktifMi = filtre == BirimFiltre.Aktif;

            // Durum (Aktif/Pasif/Tümü) ve sınıf filtresi. Seçili birim filtre dışında kalsa da listede tutulur;
            // aksi halde formda boş seçenek seçilir ve kaydedince öğrencinin birimi sessizce silinir.
            var q = _db.Birimler.AsNoTracking().Where(b =>
                ((!durumFiltreli || b.IsDeleted != aktifMi) &&
                 (!sinifMi.HasValue || b.BirimSinifMi == sinifMi.Value))
                || (selectedId.HasValue && b.BirimId == selectedId.Value));

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

        // Aktif öğrencisi (ve istenirse aktif öğretmeni) olan birim pasiflenirse öğrenciler
        // mobil sınıf listesinden ve yoklamadan düşer; bu yüzden işlem engellenir.
        private async Task BagliKayitKontrolAsync(int birimId, string islem, bool ogretmenDahil, CancellationToken ct)
        {
            var ogrenciSayisi = await _db.Ogrenciler
                .CountAsync(o => o.BirimId == birimId && !o.IsDeleted, ct);

            var ogretmenSayisi = ogretmenDahil
                ? await _db.OgretmenProfiller.CountAsync(op => op.BirimId == birimId && !op.IsDeleted, ct)
                : 0;

            if (ogrenciSayisi == 0 && ogretmenSayisi == 0)
                return;

            var bagli = new List<string>();
            if (ogrenciSayisi > 0) bagli.Add($"{ogrenciSayisi} aktif öğrenci");
            if (ogretmenSayisi > 0) bagli.Add($"{ogretmenSayisi} aktif öğretmen");

            throw new InvalidOperationException(
                $"Bu birimde {string.Join(" ve ", bagli)} var; birim {islem}. Önce kayıtları başka birime taşıyın.");
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

            if (!ent.IsDeleted && model.IsDeleted)
                await BagliKayitKontrolAsync(ent.BirimId, "pasif yapılamaz", ogretmenDahil: true, ct);
            else if (ent.BirimSinifMi && !model.BirimSinifMi)
                await BagliKayitKontrolAsync(ent.BirimId, "sınıf dışı yapılamaz", ogretmenDahil: false, ct);

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

            await BagliKayitKontrolAsync(ent.BirimId, "silinemez", ogretmenDahil: true, ct);

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
