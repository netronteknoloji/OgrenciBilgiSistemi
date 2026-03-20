using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OgrenciBilgiSistemi.Data;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Models.Enums;
using OgrenciBilgiSistemi.Services.Interfaces;

namespace OgrenciBilgiSistemi.Services.Implementations
{
    public class PersonelService : IPersonelService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<PersonelService> _logger;
        private readonly IWebHostEnvironment _env;

        public PersonelService(
            AppDbContext db,
            ILogger<PersonelService> logger,
            IWebHostEnvironment env)
        {
            _db = db;
            _logger = logger;
            _env = env;
        }

        public async Task<int> AddAsync(PersonelModel model, IFormFile? gorsel, CancellationToken ct = default)
        {
            model.PersonelKartNo = NormalizeKartNo(model.PersonelKartNo);

            if (!string.IsNullOrWhiteSpace(model.PersonelKartNo))
            {
                var exists = await _db.Personeller.AsNoTracking()
                    .AnyAsync(p => p.PersonelKartNo == model.PersonelKartNo, ct);

                if (exists)
                    throw new InvalidOperationException("Bu kart numarası başka bir personelde kayıtlı.");
            }

            // Görsel kaydet
            if (gorsel != null && gorsel.Length > 0)
                model.PersonelGorselPath = await SaveImageAsync(gorsel, ct);

            _db.Personeller.Add(model);
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("Personel eklendi: {Id} - {AdSoyad}", model.PersonelId, model.PersonelAdSoyad);
            return model.PersonelId;
        }

        public async Task UpdateAsync(PersonelModel model, IFormFile? gorsel, CancellationToken ct = default)
        {
            var entity = await _db.Personeller
                .FirstOrDefaultAsync(p => p.PersonelId == model.PersonelId, ct);

            if (entity is null)
                throw new KeyNotFoundException("Personel bulunamadı.");

            // Kart no normalize + tekillik kontrolü
            var normalized = NormalizeKartNo(model.PersonelKartNo);

            if (!string.Equals(entity.PersonelKartNo, normalized, StringComparison.Ordinal))
            {
                if (!string.IsNullOrWhiteSpace(normalized))
                {
                    var exists = await _db.Personeller.AsNoTracking()
                        .AnyAsync(p => p.PersonelId != entity.PersonelId && p.PersonelKartNo == normalized, ct);

                    if (exists)
                        throw new InvalidOperationException("Bu kart numarası başka bir personelde kayıtlı.");
                }
                entity.PersonelKartNo = normalized;
            }

            entity.PersonelAdSoyad = model.PersonelAdSoyad?.Trim() ?? string.Empty;
            entity.PersonelDurum = model.PersonelDurum;
            entity.PersonelTipi = model.PersonelTipi;
            entity.BirimId = model.BirimId;
            entity.PersonelEmail = model.PersonelEmail;
            entity.PersonelTelefon = model.PersonelTelefon;

            if (gorsel != null && gorsel.Length > 0)
            {
                entity.PersonelGorselPath = await SaveImageAsync(gorsel, ct);
            }

            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Personel güncellendi: {Id} - {AdSoyad}", entity.PersonelId, entity.PersonelAdSoyad);
        }

        public async Task DeleteAsync(int personelId, CancellationToken ct = default)
        {
            var entity = await _db.Personeller
                .FirstOrDefaultAsync(p => p.PersonelId == personelId, ct);

            if (entity is null)
                return;

            // Soft delete
            entity.PersonelDurum = false;
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("Personel soft delete: {Id} - {AdSoyad}", entity.PersonelId, entity.PersonelAdSoyad);
        }

        public async Task<bool> CihazaGonderAsync(int cihazId, bool sadeceAktifler = true, CancellationToken ct = default)
        {
            // Personelleri topla
            var q = _db.Personeller.AsNoTracking();

            if (sadeceAktifler)
                q = q.Where(p => p.PersonelDurum);

            var list = await q
                .Where(p => !string.IsNullOrEmpty(p.PersonelKartNo))
                .Select(p => new { p.PersonelId, p.PersonelAdSoyad, p.PersonelKartNo })
                .ToListAsync(ct);

            _logger.LogInformation("CihazaGonderAsync -> CihazId:{CihazId}, Kisi:{Say}", cihazId, list.Count);
            return true;
        }

        public async Task<List<SelectListItem>> GetSelectListAsync(
            PersonelTipi? tipi = null,
            PersonelFiltre filtre = PersonelFiltre.Aktif,
            CancellationToken ct = default)
        {
            var q = _db.Personeller.AsNoTracking().AsQueryable();

            if (filtre != PersonelFiltre.Tum)
            {
                bool aktifMi = filtre == PersonelFiltre.Aktif;
                q = q.Where(b => b.PersonelDurum == aktifMi);
            }

            if (tipi.HasValue)
                q = q.Where(p => p.PersonelTipi == tipi.Value);

            return await q
                .OrderBy(p => p.PersonelAdSoyad)
                .Select(p => new SelectListItem
                {
                    Value = p.PersonelId.ToString(),
                    Text = p.PersonelAdSoyad ?? ""
                })
                .ToListAsync(ct);
        }


        public async Task<PersonelModel?> GetByIdAsync(int id, bool tumPersoneller = false, CancellationToken ct = default)
        {
            var q = _db.Personeller.AsQueryable();
            if (!tumPersoneller)
                q = q.Where(p => p.PersonelDurum);

            return await q.FirstOrDefaultAsync(p => p.PersonelId == id, ct);
        }

        public async Task<SayfalanmisListeModel<PersonelModel>> SearchPagedAsync(
            string? searchString,
            int page,
            int pageSize,
            PersonelFiltre filtre = PersonelFiltre.Aktif,
            CancellationToken ct = default)
        {
            var q = _db.Personeller
                .AsNoTracking()
                .Include(p => p.Birim)
                .AsQueryable();

            // 1) Aktif / Pasif / Tümü filtresi
            q = filtre switch
            {
                PersonelFiltre.Aktif => q.Where(p => p.PersonelDurum),
                PersonelFiltre.Pasif => q.Where(p => !p.PersonelDurum),
                _ => q
            };

            // 2) AdSoyad veya KartNo araması
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var s = searchString.Trim();

                q = q.Where(p =>
                    (p.PersonelAdSoyad != null && (
                        EF.Functions.Like(EF.Functions.Collate(p.PersonelAdSoyad, "Turkish_100_CI_AI"), $"%{s}%") ||
                        EF.Functions.Like(EF.Functions.Collate(p.PersonelAdSoyad, "Latin1_General_CI_AI"), $"%{s}%")
                    ))
                    || (p.PersonelKartNo != null && EF.Functions.Like(p.PersonelKartNo, $"%{s}%"))
                );
            }

            // 3) Sıralama
            q = q.OrderBy(p => p.PersonelAdSoyad).ThenBy(p => p.PersonelId);

            // 4) Sayfalama
            page = Math.Max(1, page);
            pageSize = Math.Max(1, pageSize);

            return await SayfalanmisListeModel<PersonelModel>.CreateAsync(q, page, pageSize, ct);
        }

        private static string? NormalizeKartNo(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            var t = s.Trim();
            t = t.TrimStart('0');
            return t;
        }

        private async Task<string> SaveImageAsync(IFormFile file, CancellationToken ct)
        {
            var root = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads", "personel");
            Directory.CreateDirectory(root);

            var ext = Path.GetExtension(file.FileName);
            var name = $"per_{Guid.NewGuid():N}{ext}";
            var full = Path.Combine(root, name);

            using (var fs = new FileStream(full, FileMode.Create))
                await file.CopyToAsync(fs, ct);

            // Uygulamada kullanılacak relative path
            var rel = Path.Combine("uploads", "personel", name).Replace("\\", "/");
            return "/" + rel;
        }
    }
}
