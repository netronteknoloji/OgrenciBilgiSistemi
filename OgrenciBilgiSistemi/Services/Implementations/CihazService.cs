using Microsoft.EntityFrameworkCore;
using OgrenciBilgiSistemi.Data;
using OgrenciBilgiSistemi.Dtos;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Services.Interfaces;
using zkemkeeper;

namespace OgrenciBilgiSistemi.Services.Implementations
{
    public class CihazService : ICihazService
    {
        private const int MACHINE_NO = 1;

        private readonly AppDbContext _context;
        private readonly IZKTecoService _zkTeco;
        private readonly ILogger<CihazService> _logger;

        // Bellekteki anlık cihaz listesi
        private List<CihazModel> _cihazlar = new();
        private readonly SemaphoreSlim _refreshLock = new(1, 1);

        public CihazService(ILogger<CihazService> logger, AppDbContext context, IZKTecoService zkTeco)
        {
            _logger = logger;
            _context = context;
            _zkTeco = zkTeco;
        }

        // --- Yardımcılar ---
        private static bool ZkBaglantiUygunMu(CihazModel c) =>
            c is { IsDeleted: false } &&
            c.DonanimTipi == DonanimTipi.ZKTeco &&
            !string.IsNullOrWhiteSpace(c.IpAdresi) &&
            c.PortNo is > 0;

        private async Task EnsureCihazListAsync(CancellationToken ct = default)
        {
            if (_cihazlar.Count == 0)
                await YenileCihazListesiAsync(ct);
        }

        public async Task YenileCihazListesiAsync(CancellationToken ct = default)
        {
            await _refreshLock.WaitAsync(ct);
            try
            {
                _cihazlar = await _context.Cihazlar.AsNoTracking().ToListAsync(ct);
                _logger.LogInformation("Cihaz listesi veritabanından güncellendi. Toplam: {n}", _cihazlar.Count);
            }
            finally
            {
                _refreshLock.Release();
            }
        }

        // CİHAZDAKİ TÜM KULLANICILARI SİL
        public async Task<bool> CihazdakiTumKullanicilariSilAsync(int cihazId, CancellationToken ct = default)
        {
            await EnsureCihazListAsync(ct);

            var cihaz = _cihazlar.FirstOrDefault(c => c.CihazId == cihazId && ZkBaglantiUygunMu(c));
            if (cihaz is null)
            {
                _logger.LogWarning("Uygun cihaz bulunamadı veya kriterleri karşılamıyor: cihazId={id}", cihazId);
                return false;
            }

            // Senkron işleyici ve yemekhane taramasıyla aynı anda bağlanılmaz
            using var kilit = await ZkCihazKilidi.AlAsync(cihaz.IpAdresi!, cihaz.PortNo!.Value, ct);
            var (baglanti, code) = await _zkTeco.CihazBaglantisiAcAsync(cihaz.IpAdresi!, cihaz.PortNo!.Value, ct);
            if (baglanti is null)
            {
                _logger.LogWarning("Bağlantı başarısız (TÜM SİL / SC403): {ip}:{port} (ZKERR={code})",
                    cihaz.IpAdresi, cihaz.PortNo, code);
                return false;
            }

            using var oturum = baglanti;
            try
            {
                var zk = oturum.Zk;
                zk.EnableDevice(MACHINE_NO, false);

                bool cleared = ClearKullaniciVerisi(zk, cihaz.IpAdresi!, genelTemizlikDahil: true);

                _logger.LogInformation("SC403 toplu kullanıcı silme bitti: cihazId={id}, ip={ip}, sonuc={ok}",
                    cihazId, cihaz.IpAdresi, cleared);

                return cleared;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SC403 toplu kullanıcı silme hatası: cihazId={id}, ip={ip}", cihazId, cihaz.IpAdresi);
                return false;
            }
        }

        // CİHAZDAKİ TÜM KULLANICILARI LİSTELE
        public async Task<List<ZkUserDto>> CihazdanKullanicilariListeleAsync(int cihazId, CancellationToken ct = default)
        {
            await EnsureCihazListAsync(ct);

            var cihaz = _cihazlar.FirstOrDefault(c => c.CihazId == cihazId && ZkBaglantiUygunMu(c));
            if (cihaz is null)
            {
                _logger.LogWarning("Uygun cihaz bulunamadı veya kriterleri karşılamıyor: cihazId={id}", cihazId);
                return new List<ZkUserDto>();
            }

            var result = new List<ZkUserDto>();
            using var kilit = await ZkCihazKilidi.AlAsync(cihaz.IpAdresi!, cihaz.PortNo!.Value, ct);
            var (baglanti, code) = await _zkTeco.CihazBaglantisiAcAsync(cihaz.IpAdresi!, cihaz.PortNo!.Value, ct);
            if (baglanti is null)
            {
                _logger.LogWarning("Bağlantı başarısız (LİSTELE / SC403): {ip}:{port} (ZKERR={code})", cihaz.IpAdresi, cihaz.PortNo, code);
                return result;
            }

            using var oturum = baglanti;
            try
            {
                var zk = oturum.Zk;
                zk.EnableDevice(MACHINE_NO, false);

                // LEGACY İMZA: GetAllUserInfo iterate eder; true döndükçe next kayıt gelir.
                int id = 0;
                string name = string.Empty, pwd = string.Empty;
                int privilege = 0;
                bool enabled = false;

                bool next = zk.GetAllUserInfo(MACHINE_NO, ref id, ref name, ref pwd, ref privilege, ref enabled);
                while (next)
                {
                    ct.ThrowIfCancellationRequested();

                    result.Add(new ZkUserDto
                    {
                        UserId = id.ToString(),
                        Name = name,
                        Privilege = privilege,
                        Enabled = enabled,
                        CardNumber = string.Empty
                    });

                    next = zk.GetAllUserInfo(MACHINE_NO, ref id, ref name, ref pwd, ref privilege, ref enabled);
                }

                _logger.LogInformation("SC403 kullanıcı listesi okundu: cihazId={id}, ip={ip}, adet={n}", cihazId, cihaz.IpAdresi, result.Count);
                return result;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SC403 kullanıcı listesi hatası: cihazId={id}, ip={ip}", cihazId, cihaz.IpAdresi);
                return result;
            }
        }

        public async Task<CihazModel?> CihazGetByIdAsync(int id, CancellationToken ct = default)
        {
            return await _context.Cihazlar
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CihazId == id, ct);
        }

        public async Task<bool> CihazEkleAsync(CihazModel model, CancellationToken ct = default)
        {
            try
            {
                // ---- Inline normalize (helper yok) ----
                model.CihazAdi = model.CihazAdi?.Trim() ?? string.Empty;
                model.IpAdresi = string.IsNullOrWhiteSpace(model.IpAdresi) ? null : model.IpAdresi!.Trim();

                if (model.DonanimTipi != DonanimTipi.ZKTeco)
                {
                    model.IpAdresi = null;
                    model.PortNo = null;
                }

                // ---- Basit doğrulamalar ----
                if (string.IsNullOrWhiteSpace(model.CihazAdi))
                    return false;

                if (model.DonanimTipi == DonanimTipi.ZKTeco)
                {
                    if (string.IsNullOrWhiteSpace(model.IpAdresi)) return false;
                    if (!(model.PortNo is > 0)) return false;
                }

                if (model.CihazKodu == Guid.Empty)
                    model.CihazKodu = Guid.NewGuid();

                // ---- Benzersizlik kontrolleri ----
                var nameClash = await _context.Cihazlar.AnyAsync(c => c.CihazAdi == model.CihazAdi, ct);
                if (nameClash) return false;

                var codeClash = await _context.Cihazlar.AnyAsync(c => c.CihazKodu == model.CihazKodu, ct);
                if (codeClash) return false;

                // ---- Kaydet ----
                model.IsDeleted = false;

                _context.Cihazlar.Add(model);
                await _context.SaveChangesAsync(ct);

                await YenileCihazListesiAsync(ct);

                _logger.LogInformation("✅ Yeni cihaz eklendi: {Ad} ({Ip}:{Port}) Kod:{Kod}",
                    model.CihazAdi, model.IpAdresi, model.PortNo, model.CihazKodu);

                return true;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ CihazEkleAsync hata");
                return false;
            }
        }

        public async Task<bool> CihazGuncelleAsync(CihazModel model, CancellationToken ct = default)
        {
            try
            {
                var ent = await _context.Cihazlar.FirstOrDefaultAsync(c => c.CihazId == model.CihazId, ct);
                if (ent is null) return false;

                // ---- Inline normalize (helper yok) ----
                var temizAd = model.CihazAdi?.Trim() ?? string.Empty;
                var temizIp = string.IsNullOrWhiteSpace(model.IpAdresi) ? null : model.IpAdresi!.Trim();

                // ---- Benzersizlik (kendisi hariç) ----
                var nameClash = await _context.Cihazlar
                    .AnyAsync(c => c.CihazAdi == temizAd && c.CihazId != model.CihazId, ct);
                if (nameClash) return false;

                // ---- Basit doğrulamalar ----
                if (string.IsNullOrWhiteSpace(temizAd))
                    return false;

                if (model.DonanimTipi == DonanimTipi.ZKTeco)
                {
                    if (string.IsNullOrWhiteSpace(temizIp)) return false;
                    if (!(model.PortNo is > 0)) return false;
                }

                // ---- Alan bazlı güncelle ----
                ent.CihazAdi = temizAd;
                ent.IpAdresi = temizIp;
                ent.PortNo = model.PortNo;
                ent.DonanimTipi = model.DonanimTipi;
                ent.IstasyonTipi = model.IstasyonTipi;

                // ZKTeco değilse IP/Port’u temizle
                if (ent.DonanimTipi != DonanimTipi.ZKTeco)
                {
                    ent.IpAdresi = null;
                    ent.PortNo = null;
                }

                await _context.SaveChangesAsync(ct);
                await YenileCihazListesiAsync(ct);

                _logger.LogInformation("✅ Cihaz güncellendi: {Ad} ({Ip}:{Port}) Kod:{Kod}",
                    ent.CihazAdi, ent.IpAdresi, ent.PortNo, ent.CihazKodu);

                return true;
            }
            catch (OperationCanceledException) { throw; }
            catch (DbUpdateConcurrencyException)
            {
                _logger.LogWarning("⚠️ CihazGuncelleAsync concurrency (ID:{Id})", model.CihazId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ CihazGuncelleAsync hata");
                return false;
            }
        }

        public async Task<bool> CihazSilAsync(int id, CancellationToken ct = default)
        {
            try
            {
                var ent = await _context.Cihazlar.FirstOrDefaultAsync(c => c.CihazId == id, ct);
                if (ent is null) return false;

                ent.IsDeleted = true;
                await _context.SaveChangesAsync(ct);

                await YenileCihazListesiAsync(ct);

                _logger.LogInformation("✅ Cihaz pasif yapıldı: {Ad} ({Ip}:{Port}) Kod:{Kod}",
                    ent.CihazAdi, ent.IpAdresi, ent.PortNo, ent.CihazKodu);

                return true;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ CihazSilAsync hata");
                return false;
            }
        }

        /// <summary>
        /// Cihazdan kullanıcı verilerini temizler. Firmware uyumluluğuna göre fallback stratejisi uygular:
        /// ClearData(5) → ClearData(2)+ClearData(4) → opsiyonel ClearData(1)
        /// </summary>
        private bool ClearKullaniciVerisi(CZKEM zk, string ipAdresi, bool genelTemizlikDahil)
        {
            bool cleared = false;

            // 1) ClearData(5): kullanıcı bilgileri + şablonlar (en yaygın)
            try
            {
                if (zk.ClearData(MACHINE_NO, 5))
                {
                    _logger.LogInformation("ClearData(5) başarılı: {ip}", ipAdresi);
                    cleared = true;
                }
                else
                {
                    var err = GetZkLastError(zk);
                    _logger.LogWarning("ClearData(5) başarısız: {ip} (ZKERR={err})", ipAdresi, err);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ClearData(5) exception: {ip}", ipAdresi);
            }

            // 2) Bazı firmware'larda 5 çalışmaz; 2 (şablonlar) + 4 (user/pin) dene
            if (!cleared)
            {
                bool part1 = false, part2 = false;

                try
                {
                    part1 = zk.ClearData(MACHINE_NO, 2);
                    _logger.LogInformation("ClearData(2) sonuc={ok} ip={ip}", part1, ipAdresi);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "ClearData(2) exception: {ip}", ipAdresi);
                }

                try
                {
                    part2 = zk.ClearData(MACHINE_NO, 4);
                    _logger.LogInformation("ClearData(4) sonuc={ok} ip={ip}", part2, ipAdresi);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "ClearData(4) exception: {ip}", ipAdresi);
                }

                cleared = part1 || part2;
            }

            // 3) Opsiyonel: ClearData(1) — tüm veri (loglar dahil silinir)
            if (!cleared && genelTemizlikDahil)
            {
                try
                {
                    if (zk.ClearData(MACHINE_NO, 1))
                    {
                        _logger.LogInformation("ClearData(1) (genel) başarılı: {ip}", ipAdresi);
                        cleared = true;
                    }
                    else
                    {
                        var err = GetZkLastError(zk);
                        _logger.LogWarning("ClearData(1) başarısız: {ip} (ZKERR={err})", ipAdresi, err);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "ClearData(1) exception: {ip}", ipAdresi);
                }
            }

            try { zk.RefreshData(MACHINE_NO); } catch { /* ignore */ }

            return cleared;
        }

        private static int GetZkLastError(CZKEM zk)
        {
            try { int code = 0; zk.GetLastError(ref code); return code; } catch { return 0; }
        }

        public async Task<SayfalanmisListeModel<CihazModel>> SearchPagedAsync(
            string? search, int page, int pageSize, CancellationToken ct = default)
        {
            var query = _context.Cihazlar
                .Where(c => !c.IsDeleted)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                if (Guid.TryParse(s, out var g))
                {
                    query = query.Where(c => c.CihazKodu == g);
                }
                else
                {
                    query = query.Where(c =>
                        EF.Functions.Like(c.CihazAdi, $"%{s}%") ||
                        EF.Functions.Like(c.IpAdresi ?? "", $"%{s}%"));
                }
            }

            query = query.OrderBy(c => c.CihazAdi).ThenBy(c => c.CihazId);
            return await SayfalanmisListeModel<CihazModel>.CreateAsync(query, page, pageSize, ct);
        }

        public async Task<CihazModel?> CihazBulByKodAsync(Guid cihazKodu, CancellationToken ct = default)
        {
            return await _context.Cihazlar.AsNoTracking()
                .FirstOrDefaultAsync(c => c.CihazKodu == cihazKodu, ct);
        }

        public async Task<CihazModel?> CihazBulAktifByKodAsync(Guid cihazKodu, CancellationToken ct = default)
        {
            return await _context.Cihazlar.AsNoTracking()
                .FirstOrDefaultAsync(c => c.CihazKodu == cihazKodu && !c.IsDeleted, ct);
        }

        public async Task<CihazModel?> CihazBulVarsayilanAsync(CancellationToken ct = default)
        {
            return await _context.Cihazlar.AsNoTracking()
                .OrderBy(c => c.IsDeleted)
                .ThenBy(c => c.CihazId)
                .FirstOrDefaultAsync(ct);
        }
    }
}
