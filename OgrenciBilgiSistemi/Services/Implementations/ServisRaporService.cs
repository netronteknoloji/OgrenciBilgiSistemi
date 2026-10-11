using Microsoft.EntityFrameworkCore;
using OgrenciBilgiSistemi.Data;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Services.Interfaces;

namespace OgrenciBilgiSistemi.Services.Implementations
{
    public class ServisRaporService : IServisRaporService
    {
        private readonly AppDbContext _db;
        public ServisRaporService(AppDbContext db) { _db = db; }

        public async Task<List<ServisProfilModel>> ServisleriGetirAsync(CancellationToken ct = default)
        {
            return await _db.ServisProfiller
                .AsNoTracking()
                .Include(s => s.Kullanici)
                .OrderBy(s => s.Plaka)
                .ToListAsync(ct);
        }

        public async Task<List<ServisProfilModel>> SoforListesiAsync(string? search, CancellationToken ct = default)
        {
            var q = _db.ServisProfiller
                .AsNoTracking()
                .Include(s => s.Kullanici)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                q = q.Where(sp => sp.Plaka.Contains(s) ||
                    sp.Kullanici.KullaniciAdi.Contains(s) ||
                    (sp.SoforAdSoyad != null && sp.SoforAdSoyad.Contains(s)) ||
                    (sp.HostesAdSoyad != null && sp.HostesAdSoyad.Contains(s)));
            }

            var liste = await q.OrderBy(s => s.Plaka).ToListAsync(ct);

            var sayilar = await _db.Ogrenciler
                .Where(o => o.ServisId != null)
                .GroupBy(o => o.ServisId!.Value)
                .Select(g => new { ServisId = g.Key, Sayi = g.Count() })
                .ToDictionaryAsync(x => x.ServisId, x => x.Sayi, ct);

            foreach (var s in liste)
                s.OgrenciSayisi = sayilar.GetValueOrDefault(s.KullaniciId, 0);

            return liste;
        }

        public async Task<List<OgrenciModel>> SoforOgrenciListesiAsync(int? servisId, string? search, CancellationToken ct = default)
        {
            var q = _db.Ogrenciler
                .AsNoTracking()
                .Include(o => o.Birim)
                .Where(o => o.ServisId != null);

            if (servisId.HasValue)
                q = q.Where(o => o.ServisId == servisId.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                q = int.TryParse(s, out var no)
                    ? q.Where(o => o.OgrenciNo == no || o.OgrenciAdSoyad.Contains(s))
                    : q.Where(o => o.OgrenciAdSoyad.Contains(s));
            }

            return await q
                .OrderBy(o => o.ServisId)
                .ThenBy(o => o.OgrenciAdSoyad)
                .ToListAsync(ct);
        }

        public async Task<SayfalanmisListeModel<ServisYoklamaModel>> ServisYoklamaListeleAsync(
            int? servisId, string? search, DateTime? startDate, DateTime? endDate, int page, CancellationToken ct = default)
        {
            var q = ServisYoklamaSorgusu(servisId, search, startDate, endDate)
                .OrderByDescending(x => x.OlusturulmaTarihi);
            return await SayfalanmisListeModel<ServisYoklamaModel>.CreateAsync(q, page, 50, ct);
        }

        public async Task<List<ServisYoklamaModel>> TumServisYoklamaListeleAsync(
            int? servisId, string? search, DateTime? startDate, DateTime? endDate, CancellationToken ct = default)
        {
            return await ServisYoklamaSorgusu(servisId, search, startDate, endDate)
                .OrderByDescending(x => x.OlusturulmaTarihi)
                .ToListAsync(ct);
        }

        private IQueryable<ServisYoklamaModel> ServisYoklamaSorgusu(
            int? servisId, string? search, DateTime? startDate, DateTime? endDate)
        {
            var q = _db.ServisYoklamalar
                .AsNoTracking()
                .Include(x => x.Ogrenci).ThenInclude(o => o.Birim)
                .Include(x => x.Kullanici).ThenInclude(k => k.ServisProfil)
                .AsQueryable();

            if (servisId.HasValue)
                q = q.Where(x => x.KullaniciId == servisId.Value);

            if (startDate.HasValue)
            {
                var sv = startDate.Value.Date;
                q = q.Where(x => x.OlusturulmaTarihi >= sv);
            }
            if (endDate.HasValue)
            {
                var ev = endDate.Value.Date.AddDays(1);
                q = q.Where(x => x.OlusturulmaTarihi < ev);
            }
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                q = q.Where(x => x.Ogrenci != null && x.Ogrenci.OgrenciAdSoyad != null &&
                    EF.Functions.Like(x.Ogrenci.OgrenciAdSoyad, $"%{s}%"));
            }

            return q;
        }
    }
}
