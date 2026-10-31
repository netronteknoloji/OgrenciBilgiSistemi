using Microsoft.EntityFrameworkCore;
using OgrenciBilgiSistemi.Data;
using OgrenciBilgiSistemi.Services.Interfaces;

namespace OgrenciBilgiSistemi.Services.Implementations
{
    public sealed class CihazSenkronService : ICihazSenkronService
    {
        private readonly AppDbContext _db;
        private readonly CihazSenkronTetikleyici _tetikleyici;
        private readonly TimeProvider _saat;

        public CihazSenkronService(AppDbContext db, CihazSenkronTetikleyici tetikleyici, TimeProvider saat)
        {
            _db = db;
            _tetikleyici = tetikleyici;
            _saat = saat;
        }

        private DateTime Simdi => _saat.GetLocalNow().DateTime;

        // Bağlanılabilir ZKTeco cihazları (CihazService.ZkBaglantiUygunMu ile aynı ölçüt).
        // UPDLOCK + HOLDLOCK: eşzamanlı iki kayıt aynı bekleyen satırı iki kez eklemesin.
        public Task<int> KuyrugaEkleAsync(int ogrenciId, CancellationToken ct = default)
        {
            var simdi = Simdi;
            return _db.Database.ExecuteSqlInterpolatedAsync($@"
INSERT INTO CihazSenkronKuyrugu (CihazId, OgrenciId, Durum, SonIslem, DenemeSayisi, SonrakiDeneme, OlusturmaZamani)
SELECT C.CihazId, {ogrenciId}, 0, 0, 0, {simdi}, {simdi}
FROM Cihazlar C
WHERE C.IsDeleted = 0 AND C.DonanimTipi = 2
  AND C.IpAdresi IS NOT NULL AND C.IpAdresi <> '' AND C.PortNo > 0
  AND NOT EXISTS (SELECT 1 FROM CihazSenkronKuyrugu K WITH (UPDLOCK, HOLDLOCK)
                  WHERE K.CihazId = C.CihazId AND K.OgrenciId = {ogrenciId} AND K.Durum = 0)", ct);
        }

        // Pasif öğrenciler de alınır: cihazda kalmışlarsa silinirler. Cihazda olmayan ve olmaması gereken
        // öğrenci işleyicide cihaza dokunmadan tamamlanır.
        public async Task<int> CihaziEsitleAsync(int cihazId, CancellationToken ct = default)
        {
            var simdi = Simdi;
            await _db.Database.ExecuteSqlInterpolatedAsync($@"
UPDATE CihazSenkronKuyrugu SET SonrakiDeneme = {simdi}
WHERE CihazId = {cihazId} AND Durum = 0 AND SonrakiDeneme > {simdi}", ct);

            var eklenen = await _db.Database.ExecuteSqlInterpolatedAsync($@"
INSERT INTO CihazSenkronKuyrugu (CihazId, OgrenciId, Durum, SonIslem, DenemeSayisi, SonrakiDeneme, OlusturmaZamani)
SELECT {cihazId}, O.OgrenciId, 0, 0, 0, {simdi}, {simdi}
FROM Ogrenciler O
WHERE NOT EXISTS (SELECT 1 FROM CihazSenkronKuyrugu K WITH (UPDLOCK, HOLDLOCK)
                  WHERE K.CihazId = {cihazId} AND K.OgrenciId = O.OgrenciId AND K.Durum = 0)", ct);

            Tetikle();
            return eklenen;
        }

        // Yalnızca bağlanılabilir ZKTeco yemekhane cihazları; ana kapı cihazlarına dokunulmaz
        public async Task<(int CihazSayisi, int Eklenen)> YemekhaneCihazlariniEsitleAsync(CancellationToken ct = default)
        {
            var cihazIdler = await _db.Cihazlar.AsNoTracking()
                .Where(c => !c.IsDeleted && c.DonanimTipi == DonanimTipi.ZKTeco
                         && c.IstasyonTipi == IstasyonTipi.Yemekhane
                         && c.IpAdresi != null && c.IpAdresi != "" && c.PortNo > 0)
                .Select(c => c.CihazId)
                .ToListAsync(ct);

            var eklenen = 0;
            foreach (var cihazId in cihazIdler)
                eklenen += await CihaziEsitleAsync(cihazId, ct);

            return (cihazIdler.Count, eklenen);
        }

        public async Task<int> HatalariYenidenDeneAsync(int cihazId, CancellationToken ct = default)
        {
            var simdi = Simdi;
            // Aynı öğrenci için bekleyen satır varsa veya birden çok başarısız satır varsa yalnızca biri döner
            var donen = await _db.Database.ExecuteSqlInterpolatedAsync($@"
DELETE K FROM CihazSenkronKuyrugu K
WHERE K.CihazId = {cihazId} AND K.Durum = 3
  AND (EXISTS (SELECT 1 FROM CihazSenkronKuyrugu X WHERE X.CihazId = K.CihazId AND X.OgrenciId = K.OgrenciId AND X.Durum = 0)
       OR EXISTS (SELECT 1 FROM CihazSenkronKuyrugu X WHERE X.CihazId = K.CihazId AND X.OgrenciId = K.OgrenciId
                  AND X.Durum = 3 AND X.CihazSenkronKuyruguId > K.CihazSenkronKuyruguId));

UPDATE CihazSenkronKuyrugu
SET Durum = 0, DenemeSayisi = 0, SonrakiDeneme = {simdi}, KilitBitis = NULL, SonHata = NULL
WHERE CihazId = {cihazId} AND Durum = 3;", ct);

            Tetikle();
            return donen;
        }

        public async Task<Dictionary<int, CihazSenkronOzetDto>> OzetlerAsync(
            IReadOnlyCollection<int> cihazIdler, CancellationToken ct = default)
        {
            if (cihazIdler.Count == 0)
                return new Dictionary<int, CihazSenkronOzetDto>();

            var satirlar = _db.CihazSenkronKuyrugu.AsNoTracking().Where(k => cihazIdler.Contains(k.CihazId));

            var sayilar = await satirlar
                .GroupBy(k => k.CihazId)
                .Select(g => new
                {
                    CihazId = g.Key,
                    Bekleyen = g.Count(k => k.Durum == CihazSenkronDurumu.Bekliyor || k.Durum == CihazSenkronDurumu.Isleniyor),
                    Hatali = g.Count(k => k.Durum == CihazSenkronDurumu.Basarisiz),
                    SonBasarili = g.Where(k => k.Durum == CihazSenkronDurumu.Tamamlandi && k.SonHata == null).Max(k => k.TamamlanmaZamani)
                })
                .ToListAsync(ct);

            // Son hata: bekleyenlerde cihazın erişilemediğini, başarısızlarda çakışmayı/reddi gösterir
            var sonHatalar = await satirlar
                .Where(k => (k.Durum == CihazSenkronDurumu.Bekliyor || k.Durum == CihazSenkronDurumu.Basarisiz) && k.SonHata != null)
                .GroupBy(k => k.CihazId)
                .Select(g => g.OrderByDescending(k => k.CihazSenkronKuyruguId).Select(k => new { k.CihazId, k.SonHata }).First())
                .ToListAsync(ct);

            return sayilar.ToDictionary(
                s => s.CihazId,
                s => new CihazSenkronOzetDto(s.Bekleyen, s.Hatali, s.SonBasarili,
                    sonHatalar.FirstOrDefault(h => h.CihazId == s.CihazId)?.SonHata));
        }

        public void Tetikle() => _tetikleyici.Tetikle();
    }
}
