using Microsoft.EntityFrameworkCore;
using OgrenciBilgiSistemi.Data;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Services.Interfaces;
using OgrenciBilgiSistemi.ViewModels;

namespace OgrenciBilgiSistemi.Services.Implementations
{
    public class GecisRaporService : IGecisRaporService
    {
        private readonly AppDbContext _db;
        public GecisRaporService(AppDbContext db) { _db = db; }

        // --- Genel listeler (Detay action) ---

        public async Task<SayfalanmisListeModel<SinifYoklamaModel>> SinifYoklamaListeleAsync(
            string? search, DateTime? startDate, DateTime? endDate, int page, CancellationToken ct = default)
        {
            var q = SinifYoklamaSorgusu(search, startDate, endDate)
                .OrderByDescending(x => x.OlusturulmaTarihi);
            return await SayfalanmisListeModel<SinifYoklamaModel>.CreateAsync(q, page, 50, ct);
        }

        public async Task<SayfalanmisListeModel<ServisYoklamaModel>> ServisYoklamaListeleAsync(
            string? search, DateTime? startDate, DateTime? endDate, int page, CancellationToken ct = default)
        {
            var q = ServisYoklamaSorgusu(search, startDate, endDate)
                .OrderByDescending(x => x.OlusturulmaTarihi);
            return await SayfalanmisListeModel<ServisYoklamaModel>.CreateAsync(q, page, 50, ct);
        }

        public async Task<SayfalanmisListeModel<OgrenciDetayModel>> GecisListeleAsync(
            string? sortOrder, string? search, DateTime? startDate, DateTime? endDate,
            IstasyonTipi? istasyon, int page, CancellationToken ct = default)
        {
            var q = GecisSorgusu(search, startDate, endDate, istasyon);

            q = sortOrder switch
            {
                "AdSoyad" => q.OrderBy(x => x.Ogrenci!.OgrenciAdSoyad).ThenBy(x => x.Ogrenci!.OgrenciNo),
                "AdSoyad_desc" => q.OrderByDescending(x => x.Ogrenci!.OgrenciAdSoyad).ThenBy(x => x.Ogrenci!.OgrenciNo),
                "No" => q.OrderBy(x => x.Ogrenci!.OgrenciNo).ThenBy(x => x.Ogrenci!.OgrenciAdSoyad),
                "No_desc" => q.OrderByDescending(x => x.Ogrenci!.OgrenciNo).ThenBy(x => x.Ogrenci!.OgrenciAdSoyad),
                "Tarih" => q.OrderBy(x => (x.OgrenciGTarih ?? x.OgrenciCTarih)).ThenBy(x => x.OgrenciDetayId),
                "Tarih_desc" => q.OrderByDescending(x => (x.OgrenciGTarih ?? x.OgrenciCTarih)).ThenByDescending(x => x.OgrenciDetayId),
                _ => q.OrderByDescending(x => (x.OgrenciGTarih ?? x.OgrenciCTarih)).ThenByDescending(x => x.OgrenciDetayId)
            };

            return await SayfalanmisListeModel<OgrenciDetayModel>.CreateAsync(q, page, 50, ct);
        }

        // --- Genel Excel (DetayExcel action) ---

        public async Task<List<SinifYoklamaModel>> TumSinifYoklamaListeleAsync(
            string? search, DateTime? startDate, DateTime? endDate, CancellationToken ct = default)
        {
            return await SinifYoklamaSorgusu(search, startDate, endDate)
                .OrderByDescending(x => x.OlusturulmaTarihi)
                .ToListAsync(ct);
        }

        public async Task<List<ServisYoklamaModel>> TumServisYoklamaListeleAsync(
            string? search, DateTime? startDate, DateTime? endDate, CancellationToken ct = default)
        {
            return await ServisYoklamaSorgusu(search, startDate, endDate)
                .OrderByDescending(x => x.OlusturulmaTarihi)
                .ToListAsync(ct);
        }

        public async Task<List<OgrenciDetayModel>> TumGecislerGetirAsync(
            string? search, DateTime? startDate, DateTime? endDate, IstasyonTipi? istasyon, CancellationToken ct = default)
        {
            return await GecisSorgusu(search, startDate, endDate, istasyon)
                .OrderByDescending(l => l.OgrenciCTarih ?? l.OgrenciGTarih)
                .ToListAsync(ct);
        }

        // --- Tek öğrenci (GirisCikisDetay + DetayExportToExcel) ---

        public async Task<OgrenciModel?> OgrenciBulAsync(int ogrenciId, CancellationToken ct = default)
        {
            return await _db.Ogrenciler
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.OgrenciId == ogrenciId, ct);
        }

        public async Task<SayfalanmisListeModel<OgrenciGirisCikisVm>> OgrenciGecisListeleAsync(
            int ogrenciId, DateTime? startDate, DateTime? endDate, IstasyonTipi? istasyon, int page, CancellationToken ct = default)
        {
            var q = _db.OgrenciDetaylar
                .AsNoTracking()
                .Include(d => d.Cihaz)
                .Include(d => d.Ogrenci)
                .Where(d => d.OgrenciId == ogrenciId);

            var hasStart = startDate.HasValue;
            var hasEnd = endDate.HasValue;
            var s = startDate?.Date;
            var eExclusive = endDate?.Date.AddDays(1);

            if (hasStart && hasEnd)
                q = q.Where(d =>
                    (d.OgrenciGTarih.HasValue && d.OgrenciGTarih.Value >= s!.Value && d.OgrenciGTarih.Value < eExclusive!.Value) ||
                    (d.OgrenciCTarih.HasValue && d.OgrenciCTarih.Value >= s!.Value && d.OgrenciCTarih.Value < eExclusive!.Value));
            else if (hasStart)
                q = q.Where(d =>
                    (d.OgrenciGTarih.HasValue && d.OgrenciGTarih.Value >= s!.Value) ||
                    (d.OgrenciCTarih.HasValue && d.OgrenciCTarih.Value >= s!.Value));
            else if (hasEnd)
                q = q.Where(d =>
                    (d.OgrenciGTarih.HasValue && d.OgrenciGTarih.Value < eExclusive!.Value) ||
                    (d.OgrenciCTarih.HasValue && d.OgrenciCTarih.Value < eExclusive!.Value));

            if (istasyon.HasValue)
                q = q.Where(d => d.Cihaz != null && d.Cihaz.IstasyonTipi == istasyon.Value);

            q = q.Where(d => d.OgrenciGTarih.HasValue || d.OgrenciCTarih.HasValue);

            q = q
                .OrderByDescending(d => (d.OgrenciGTarih ?? d.OgrenciCTarih)!.Value.Date)
                .ThenBy(d =>
                    1 + _db.OgrenciDetaylar
                        .Where(x =>
                            x.OgrenciId == d.OgrenciId &&
                            x.OgrenciGTarih.HasValue &&
                            EF.Functions.DateDiffDay(
                                x.OgrenciGTarih.Value,
                                (d.OgrenciGTarih ?? d.OgrenciCTarih)!.Value) == 0 &&
                            x.OgrenciGTarih.Value > (d.OgrenciGTarih ?? d.OgrenciCTarih)!.Value
                        )
                        .Count()
                )
                .ThenByDescending(d => d.OgrenciGTarih.HasValue)
                .ThenByDescending(d => d.OgrenciGTarih ?? d.OgrenciCTarih)
                .ThenByDescending(d => d.OgrenciDetayId);

            var proj = q.Select(h => new OgrenciGirisCikisVm
            {
                OgrenciDetayId = h.OgrenciDetayId,
                OgrenciAdSoyad = h.Ogrenci != null ? h.Ogrenci.OgrenciAdSoyad : "Bilinmiyor",
                OgrenciKartNo = h.Ogrenci != null ? h.Ogrenci.OgrenciKartNo ?? "-" : "-",
                OgrenciGTarih = h.OgrenciGTarih,
                OgrenciCTarih = h.OgrenciCTarih,
                OgrenciGecisTipi = h.OgrenciGecisTipi ?? string.Empty,
                CihazAdi = h.Cihaz != null ? h.Cihaz.CihazAdi : "Bilinmiyor"
            });

            return await SayfalanmisListeModel<OgrenciGirisCikisVm>.CreateAsync(proj, page, 25, ct);
        }

        public async Task<List<SinifYoklamaModel>> OgrenciSinifYoklamaListeleAsync(
            int ogrenciId, DateTime? startDate, DateTime? endDate, CancellationToken ct = default)
        {
            return await OgrenciSinifYoklamaSorgusu(ogrenciId, startDate, endDate)
                .OrderByDescending(x => x.OlusturulmaTarihi)
                .ToListAsync(ct);
        }

        public async Task<List<ServisYoklamaModel>> OgrenciServisYoklamaListeleAsync(
            int ogrenciId, DateTime? startDate, DateTime? endDate, CancellationToken ct = default)
        {
            return await OgrenciServisYoklamaSorgusu(ogrenciId, startDate, endDate)
                .OrderByDescending(x => x.OlusturulmaTarihi)
                .ToListAsync(ct);
        }

        public async Task<List<OgrenciDetayModel>> OgrenciGecislerGetirAsync(
            int ogrenciId, DateTime? startDate, DateTime? endDate, IstasyonTipi? istasyon, CancellationToken ct = default)
        {
            var q = _db.OgrenciDetaylar
                .AsNoTracking()
                .Include(d => d.Cihaz)
                .Where(d => d.OgrenciId == ogrenciId);

            if (startDate.HasValue)
            {
                var sv = startDate.Value.Date;
                q = q.Where(d =>
                    (d.OgrenciGTarih.HasValue && d.OgrenciGTarih.Value >= sv) ||
                    (d.OgrenciCTarih.HasValue && d.OgrenciCTarih.Value >= sv));
            }
            if (endDate.HasValue)
            {
                var ev = endDate.Value.Date.AddDays(1);
                q = q.Where(d =>
                    (d.OgrenciGTarih.HasValue && d.OgrenciGTarih.Value < ev) ||
                    (d.OgrenciCTarih.HasValue && d.OgrenciCTarih.Value < ev));
            }
            if (istasyon.HasValue)
                q = q.Where(d => d.Cihaz != null && d.Cihaz.IstasyonTipi == istasyon.Value);

            return await q.OrderByDescending(d => d.OgrenciGTarih ?? d.OgrenciCTarih).ToListAsync(ct);
        }

        // --- Özel sorgular ---

        private IQueryable<SinifYoklamaModel> SinifYoklamaSorgusu(
            string? search, DateTime? startDate, DateTime? endDate)
        {
            var q = _db.SinifYoklamalar
                .AsNoTracking()
                .Include(x => x.Ogrenci).ThenInclude(o => o.Birim)
                .Include(x => x.Kullanici)
                .AsQueryable();

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

        private IQueryable<ServisYoklamaModel> ServisYoklamaSorgusu(
            string? search, DateTime? startDate, DateTime? endDate)
        {
            var q = _db.ServisYoklamalar
                .AsNoTracking()
                .Include(x => x.Ogrenci).ThenInclude(o => o.Birim)
                .Include(x => x.Kullanici)
                .AsQueryable();

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

        private IQueryable<OgrenciDetayModel> GecisSorgusu(
            string? search, DateTime? startDate, DateTime? endDate, IstasyonTipi? istasyon)
        {
            var q = _db.OgrenciDetaylar
                .AsNoTracking()
                .Include(x => x.Ogrenci).ThenInclude(o => o.Birim)
                .Include(x => x.Cihaz)
                .AsQueryable();

            if (startDate.HasValue)
            {
                var sv = startDate.Value.Date;
                q = q.Where(x => (x.OgrenciGTarih ?? x.OgrenciCTarih) >= sv);
            }
            if (endDate.HasValue)
            {
                var ev = endDate.Value.Date.AddDays(1);
                q = q.Where(x => (x.OgrenciGTarih ?? x.OgrenciCTarih) < ev);
            }
            if (istasyon.HasValue)
                q = q.Where(x => x.IstasyonTipi == istasyon.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                if (int.TryParse(s, out var no))
                {
                    q = q.Where(x =>
                        (x.Ogrenci != null && x.Ogrenci.OgrenciNo == no) ||
                        (x.Ogrenci != null && x.Ogrenci.OgrenciKartNo != null && (
                            x.Ogrenci.OgrenciKartNo == s ||
                            EF.Functions.Like(x.Ogrenci.OgrenciKartNo, $"%{s}%")
                        )) ||
                        (x.Ogrenci != null && x.Ogrenci.OgrenciAdSoyad != null && (
                            EF.Functions.Like(EF.Functions.Collate(x.Ogrenci.OgrenciAdSoyad, "Turkish_100_CI_AI"), $"%{s}%") ||
                            EF.Functions.Like(EF.Functions.Collate(x.Ogrenci.OgrenciAdSoyad, "Latin1_General_CI_AI"), $"%{s}%")
                        )));
                }
                else
                {
                    q = q.Where(x =>
                        x.Ogrenci != null && (
                            (x.Ogrenci.OgrenciAdSoyad != null && (
                                EF.Functions.Like(EF.Functions.Collate(x.Ogrenci.OgrenciAdSoyad, "Turkish_100_CI_AI"), $"%{s}%") ||
                                EF.Functions.Like(EF.Functions.Collate(x.Ogrenci.OgrenciAdSoyad, "Latin1_General_CI_AI"), $"%{s}%")
                            )) ||
                            (x.Ogrenci.OgrenciKartNo != null &&
                                EF.Functions.Like(x.Ogrenci.OgrenciKartNo, $"%{s}%"))
                        )
                    );
                }
            }

            return q;
        }

        private IQueryable<SinifYoklamaModel> OgrenciSinifYoklamaSorgusu(
            int ogrenciId, DateTime? startDate, DateTime? endDate)
        {
            var q = _db.SinifYoklamalar
                .AsNoTracking()
                .Include(x => x.Kullanici)
                .Where(x => x.OgrenciId == ogrenciId);

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
            return q;
        }

        private IQueryable<ServisYoklamaModel> OgrenciServisYoklamaSorgusu(
            int ogrenciId, DateTime? startDate, DateTime? endDate)
        {
            var q = _db.ServisYoklamalar
                .AsNoTracking()
                .Include(x => x.Kullanici)
                .Where(x => x.OgrenciId == ogrenciId);

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
            return q;
        }
    }
}
