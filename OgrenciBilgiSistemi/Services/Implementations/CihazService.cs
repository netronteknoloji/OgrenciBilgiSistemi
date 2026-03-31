using Microsoft.EntityFrameworkCore;
using OgrenciBilgiSistemi.Data;
using OgrenciBilgiSistemi.Dtos;
using OgrenciBilgiSistemi.Models;
using System.Runtime.InteropServices;
using zkemkeeper;

namespace OgrenciBilgiSistemi.Services.Implementations
{
    public class CihazService : ICihazService
    {
        private const int MACHINE_NO = 1;

        private readonly AppDbContext _context;
        private readonly ILogger<CihazService> _logger;

        // Bellekteki anlık cihaz listesi
        private List<CihazModel> _cihazlar = new();
        private readonly SemaphoreSlim _refreshLock = new(1, 1);

        public CihazService(ILogger<CihazService> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // --- Yardımcılar ---
        private static bool ZkBaglantiUygunMu(CihazModel c) =>
            c is { Aktif: true } &&
            c.DonanimTipi == DonanimTipi.ZKTeco &&
            !string.IsNullOrWhiteSpace(c.IpAdresi) &&
            c.PortNo is > 0;

        private static CZKEM CreateZk() => new CZKEM();

        private static void ReleaseZk(CZKEM? zk)
        {
            try { zk?.EnableDevice(MACHINE_NO, true); } catch { }
            try { zk?.Disconnect(); } catch { }
            if (zk != null)
            {
                try { Marshal.FinalReleaseComObject(zk); } catch { }
            }
        }

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

        public async Task<List<CihazModel>> GetCihazlarAsync(CancellationToken ct = default)
        {
            await EnsureCihazListAsync(ct);
            return _cihazlar;
        }

        public List<CihazModel> GetCihazlar() => _cihazlar;

        // ---------------------- KULLANICI UPSERT (SSR -> Legacy) ----------------------
        public async Task<bool> CihazaOgrenciEkleAsync(OgrenciModel ogrenci, CancellationToken ct = default)
        {
            await EnsureCihazListAsync(ct);
            bool isSuccess = true;

            foreach (var cihaz in _cihazlar.Where(ZkBaglantiUygunMu))
            {
                ct.ThrowIfCancellationRequested();
                CZKEM? zk = null;
                try
                {
                    zk = CreateZk();
                    if (!zk.Connect_Net(cihaz.IpAdresi, cihaz.PortNo!.Value))
                    {
                        _logger.LogWarning("Bağlantı başarısız: {ip}:{port}", cihaz.IpAdresi, cihaz.PortNo);
                        isSuccess = false;
                        continue;
                    }

                    zk.EnableDevice(MACHINE_NO, false);

                    // İsim direkt gönderiliyor (kısaltma yok)
                    var adSoyad = ogrenci.OgrenciAdSoyad ?? string.Empty;

                    // Şifre boş
                    var sifre = string.Empty;

                    // (Opsiyonel) Kart tamponu – desteklenmeyebilir
                    if (!string.IsNullOrEmpty(ogrenci.OgrenciKartNo))
                    {
                        if (!zk.SetStrCardNumber(ogrenci.OgrenciKartNo))
                            _logger.LogWarning("SC403 kart tamponu desteklemiyor olabilir: {ip}", cihaz.IpAdresi);
                    }

                    // Legacy SetUserInfo
                    var ok = zk.SetUserInfo(
                        MACHINE_NO,
                        ogrenci.OgrenciId,
                        adSoyad,
                        sifre,
                        0,   // Normal user
                        true // Enabled
                    );

                    if (!ok)
                    {
                        _logger.LogError("Öğrenci eklenemedi (SC403 legacy): {ip}", cihaz.IpAdresi);
                        isSuccess = false;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Öğrenci eklerken hata (SC403): {ip}", cihaz.IpAdresi);
                    isSuccess = false;
                }
                finally
                {
                    ReleaseZk(zk);
                }
            }

            return isSuccess;
        }

        public async Task<bool> CihazaOgrenciGuncelleAsync(OgrenciModel ogrenci, CancellationToken ct = default)
        {
            // SC403 için ekleme ve güncelleme aynı "upsert" akışı
            return await CihazaOgrenciEkleAsync(ogrenci, ct);
        }

        // ---------------------- KULLANICI SİLME (SSR -> Legacy) ----------------------
        public async Task<bool> CihazaOgrenciSilAsync(int ogrenciId, CancellationToken ct = default)
        {
            await EnsureCihazListAsync(ct);
            bool isSuccess = true;

            foreach (var cihaz in _cihazlar.Where(ZkBaglantiUygunMu))
            {
                ct.ThrowIfCancellationRequested();
                CZKEM? zk = null;
                try
                {
                    zk = CreateZk();
                    if (!zk.Connect_Net(cihaz.IpAdresi, cihaz.PortNo!.Value))
                    {
                        _logger.LogWarning("Bağlantı başarısız: {ip}:{port}", cihaz.IpAdresi, cihaz.PortNo);
                        isSuccess = false; continue;
                    }

                    zk.EnableDevice(MACHINE_NO, false);

                    var ok = zk.SSR_DeleteEnrollData(MACHINE_NO, ogrenciId.ToString(), 12);
                    if (!ok)
                    {
                        // Legacy imza: DeleteEnrollData(int,int,int,int)
                        if (int.TryParse(ogrenciId.ToString(), out var idInt))
                        {
                            // dwBackupNumber=12 (tüm biyometriler), dwMachinePrivilege=0 (normal)
                            ok = zk.DeleteEnrollData(MACHINE_NO, idInt, 12, 0);
                        }
                    }

                    if (!ok)
                    {
                        _logger.LogError("Öğrenci silinemedi: {ip}", cihaz.IpAdresi);
                        isSuccess = false;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Öğrenci silinirken hata: {ip}", cihaz.IpAdresi);
                    isSuccess = false;
                }
                finally
                {
                    ReleaseZk(zk);
                }
            }

            return isSuccess;
        }

        // ---------------------- TOPLU EKLEME (SSR -> Legacy) ----------------------
        public async Task<bool> CihazaOgrencileriGonderAsync(
        int cihazId, List<OgrenciModel> ogrenciListesi, CancellationToken ct = default)
        {
            await EnsureCihazListAsync(ct);
            bool isSuccess = true;

            var cihaz = _cihazlar.FirstOrDefault(c => c.CihazId == cihazId && ZkBaglantiUygunMu(c));
            if (cihaz is null)
            {
                _logger.LogWarning("Uygun cihaz bulunamadı veya bağlantı kriterlerini karşılamıyor: cihazId={id}", cihazId);
                return false;
            }

            CZKEM? zk = null;
            try
            {
                zk = CreateZk();
                if (!zk.Connect_Net(cihaz.IpAdresi, cihaz.PortNo!.Value))
                {
                    _logger.LogWarning("Bağlantı başarısız: {ip}:{port}", cihaz.IpAdresi, cihaz.PortNo);
                    return false;
                }

                zk.EnableDevice(MACHINE_NO, false);

                // 0) ÖNCE SİL — ClearData(1) YOK (loglar korunur)
                bool cleared = false;

                // 5 → kullanıcı bilgileri + şablonlar
                try
                {
                    if (zk.ClearData(MACHINE_NO, 5))
                    {
                        _logger.LogInformation("ClearData(5) başarılı: {ip}", cihaz.IpAdresi);
                        cleared = true;
                    }
                    else
                    {
                        var err = GetZkLastError(zk);
                        _logger.LogWarning("ClearData(5) başarısız: {ip} (ZKERR={err})", cihaz.IpAdresi, err);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "ClearData(5) exception: {ip}", cihaz.IpAdresi);
                }

                // 5 çalışmazsa 2 (şablonlar) + 4 (user/pin)
                if (!cleared)
                {
                    bool partTpl = false, partUsr = false;

                    try { partTpl = zk.ClearData(MACHINE_NO, 2); } catch (Exception ex) { _logger.LogWarning(ex, "ClearData(2) exception: {ip}", cihaz.IpAdresi); }
                    try { partUsr = zk.ClearData(MACHINE_NO, 4); } catch (Exception ex) { _logger.LogWarning(ex, "ClearData(4) exception: {ip}", cihaz.IpAdresi); }

                    _logger.LogInformation("ClearData(2)={tpl} ClearData(4)={usr} ip={ip}", partTpl, partUsr, cihaz.IpAdresi);
                    cleared = partTpl || partUsr;
                }

                // Silme başarısızsa eklemeyi durdur
                try { zk.RefreshData(MACHINE_NO); } catch { /* ignore */ }
                if (!cleared)
                {
                    _logger.LogWarning("Toplu kullanıcı silme başarısız; ekleme yapılmadı. ip={ip}", cihaz.IpAdresi);
                    return false;
                }

                // 1) EKLE — mevcut akış
                int okCount = 0, failCount = 0;

                foreach (var ogrenci in ogrenciListesi)
                {
                    ct.ThrowIfCancellationRequested();

                    if (!string.IsNullOrEmpty(ogrenci.OgrenciKartNo))
                    {
                        var cardAdded = zk.SetStrCardNumber(ogrenci.OgrenciKartNo);
                        if (!cardAdded)
                            _logger.LogWarning("SC403 kart tamponu desteklemiyor olabilir: {ip} / {ogrId}", cihaz.IpAdresi, ogrenci.OgrenciId);
                    }

                    var adSoyad = ogrenci.OgrenciAdSoyad ?? string.Empty;
                    var sifre = string.Empty;

                    var ok = zk.SetUserInfo(
                        MACHINE_NO,
                        ogrenci.OgrenciId,
                        adSoyad,
                        sifre,
                        0,
                        true
                    );

                    if (!ok)
                    {
                        _logger.LogError("Öğrenci eklenemedi (SC403): {ip} / {ogrId}", cihaz.IpAdresi, ogrenci.OgrenciId);
                        isSuccess = false;
                        failCount++;
                    }
                    else
                    {
                        okCount++;
                    }
                }

                try { zk.RefreshData(MACHINE_NO); } catch { /* ignore */ }

                _logger.LogInformation("SC403 toplu ekleme bitti: cihazId={id}, {ip} OK={ok} FAIL={fail}",
                    cihazId, cihaz.IpAdresi, okCount, failCount);

                return failCount == 0 && isSuccess;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Toplu ekleme hatası (SC403): cihazId={id}, ip={ip}", cihazId, cihaz.IpAdresi);
                return false;
            }
            finally
            {
                if (zk != null)
                {
                    try { zk.EnableDevice(MACHINE_NO, true); } catch { /* ignore */ }
                    ReleaseZk(zk);
                }
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

            CZKEM? zk = null;
            try
            {
                zk = CreateZk();
                if (!zk.Connect_Net(cihaz.IpAdresi, cihaz.PortNo!.Value))
                {
                    var code = GetZkLastError(zk);
                    _logger.LogWarning("Bağlantı başarısız (TÜM SİL / SC403): {ip}:{port} (ZKERR={code})",
                        cihaz.IpAdresi, cihaz.PortNo, code);
                    return false;
                }

                zk.EnableDevice(MACHINE_NO, false);

                // --- SADECE CLEAR DATA DENEMELERİ (no fallback to per-user) ---
                bool cleared = false;

                // 1) En yaygın: 5 → kullanıcı bilgileri + şablonlar
                try
                {
                    if (zk.ClearData(MACHINE_NO, 5))
                    {
                        _logger.LogInformation("ClearData(5) başarılı: {ip}", cihaz.IpAdresi);
                        cleared = true;
                    }
                    else
                    {
                        var err = GetZkLastError(zk);
                        _logger.LogWarning("ClearData(5) başarısız: {ip} (ZKERR={err})", cihaz.IpAdresi, err);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "ClearData(5) exception: {ip}", cihaz.IpAdresi);
                }

                // 2) Bazı firmware’larda 5 çalışmaz; kullanıcı verisi iki parçaya bölünür:
                //    2 = fingerprint templates, 4 = password/user info
                if (!cleared)
                {
                    bool part1 = false, part2 = false;

                    try
                    {
                        part1 = zk.ClearData(MACHINE_NO, 2); // şablonlar
                        _logger.LogInformation("ClearData(2) sonuc={ok} ip={ip}", part1, cihaz.IpAdresi);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "ClearData(2) exception: {ip}", cihaz.IpAdresi);
                    }

                    try
                    {
                        part2 = zk.ClearData(MACHINE_NO, 4); // user/pin
                        _logger.LogInformation("ClearData(4) sonuc={ok} ip={ip}", part2, cihaz.IpAdresi);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "ClearData(4) exception: {ip}", cihaz.IpAdresi);
                    }

                    cleared = part1 || part2;
                }

                // (OPSİYONEL) 3) Bazı FW’larda 1 tüm veri: İSTEMİYORSANIZ KALDIRIN
                if (!cleared)
                {
                    try
                    {
                        if (zk.ClearData(MACHINE_NO, 1))
                        {
                            _logger.LogInformation("ClearData(1) (genel) başarılı: {ip}", cihaz.IpAdresi);
                            cleared = true;
                        }
                        else
                        {
                            var err = GetZkLastError(zk);
                            _logger.LogWarning("ClearData(1) başarısız: {ip} (ZKERR={err})", cihaz.IpAdresi, err);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "ClearData(1) exception: {ip}", cihaz.IpAdresi);
                    }
                }

                try { zk.RefreshData(MACHINE_NO); } catch { /* ignore */ }

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
            finally
            {
                if (zk != null)
                {
                    try { zk.EnableDevice(MACHINE_NO, true); } catch { /* ignore */ }
                    ReleaseZk(zk);
                }
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
            CZKEM? zk = null;

            try
            {
                zk = CreateZk();
                if (!zk.Connect_Net(cihaz.IpAdresi, cihaz.PortNo!.Value))
                {
                    var code = GetZkLastError(zk);
                    _logger.LogWarning("Bağlantı başarısız (LİSTELE / SC403): {ip}:{port} (ZKERR={code})", cihaz.IpAdresi, cihaz.PortNo, code);
                    return result;
                }

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
                        CardNumber = "1"
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
            finally
            {
                ReleaseZk(zk);
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
                model.Aktif = true;

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
                ent.Aktif = model.Aktif;
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

                ent.Aktif = false;
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

        private static int GetZkLastError(CZKEM zk)
        {
            try { int code = 0; zk.GetLastError(ref code); return code; } catch { return 0; }
        }
    }
}
