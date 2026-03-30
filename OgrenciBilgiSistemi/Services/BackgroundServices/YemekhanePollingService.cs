using System.Data;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OgrenciBilgiSistemi.Data;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Services.Interfaces;
using OgrenciBilgiSistemi.Sms;
using OgrenciBilgiSistemi.Shared.Enums;
using zkemkeeper;

namespace OgrenciBilgiSistemi.Services.BackgroundServices;

/// <summary>
/// Yemekhane ZKTeco cihazlarından periyodik polling ile toplu log çeker,
/// yemek hakkı kontrolü yapar, günde 1 giriş kaydı yazar ve veli SMS gönderir.
/// </summary>
public sealed class YemekhanePollingService : BackgroundService
{
    private const int MACHINE_NUMBER = 1;
    private const int MAX_DEGREE_OF_PARALLELISM = 3;
    private static readonly TimeSpan POLL_INTERVAL = TimeSpan.FromMinutes(1);

    private const string OkulAdi = "Abdülhamit Han Ortaokulu";
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<YemekhanePollingService> _logger;

    public YemekhanePollingService(
        IServiceScopeFactory scopeFactory,
        ILogger<YemekhanePollingService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Yemekhane polling servisi başlatıldı.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await YemekhaneLogIsle(stoppingToken);
                await BekleyenSmsGonder(stoppingToken);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Yemekhane polling turu sırasında beklenmeyen hata!");
            }

            try { await Task.Delay(POLL_INTERVAL, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }

        _logger.LogInformation("Yemekhane polling servisi durduruluyor.");
    }

    // -------------------------------------------------------
    // 1) Cihazlardan log çek, DB'ye yaz
    // -------------------------------------------------------
    private async Task YemekhaneLogIsle(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var cihazlar = await db.Cihazlar.AsNoTracking()
            .Where(c => c.Aktif
                     && c.DonanimTipi == DonanimTipi.ZKTeco
                     && c.IstasyonTipi == IstasyonTipi.Yemekhane
                     && c.IpAdresi != null
                     && c.PortNo.HasValue && c.PortNo!.Value > 0)
            .Select(c => new { c.CihazId, c.CihazAdi, c.IpAdresi, c.PortNo })
            .ToListAsync(ct);

        if (cihazlar.Count == 0)
        {
            _logger.LogDebug("Yemekhane ZKTeco cihazı bulunamadı.");
            return;
        }
        _logger.LogInformation("{Count} yemekhane cihazı bulundu.", cihazlar.Count);

        var today = DateTime.Now.Date;

        // Yemek hakkı olan öğrenciler (bu ay)
        var gecerliOgrenciIdler = await db.OgrenciYemekler.AsNoTracking()
            .Where(y => y.Aktif && y.Ay == today.Month && y.Yil == today.Year)
            .Select(y => y.OgrenciId)
            .Distinct()
            .ToListAsync(ct);
        var gecerliSet = new HashSet<int>(gecerliOgrenciIdler);

        // Bugün zaten GİRİŞ kaydı olan öğrenciler
        var mevcutBugun = await db.OgrenciDetaylar.AsNoTracking()
            .Where(x => x.IstasyonTipi == IstasyonTipi.Yemekhane
                        && x.OgrenciGecisTipi == "GİRİŞ"
                        && x.OgrenciGTarih.HasValue
                        && x.OgrenciGTarih.Value.Date == today)
            .Select(x => x.OgrenciId)
            .Distinct()
            .ToListAsync(ct);
        var mevcutSet = new HashSet<int>(mevcutBugun);

        var tumYeniLoglar = new List<OgrenciDetayModel>();
        var temizlenecekCihazlar = new List<(string Ip, int Port)>();

        foreach (var cihaz in cihazlar)
        {
            ct.ThrowIfCancellationRequested();

            var (yeniLoglar, cihazOk, bugunLogVar) = CihazdanLogCek(
                cihaz.CihazId, cihaz.IpAdresi!, cihaz.PortNo!.Value,
                today, gecerliSet, mevcutSet);

            tumYeniLoglar.AddRange(yeniLoglar);

            if (cihazOk && !bugunLogVar)
                temizlenecekCihazlar.Add((cihaz.IpAdresi!, cihaz.PortNo!.Value));
        }

        // Öğrenci+Gün bazında tekilleştir
        var dedup = tumYeniLoglar
            .Where(x => x.OgrenciGTarih.HasValue && x.OgrenciGTarih.Value.Date == today)
            .GroupBy(x => x.OgrenciId)
            .Select(g => g.OrderBy(z => z.OgrenciGTarih).First())
            .ToList();

        // sp_getapplock ile tekil ekleme
        foreach (var log in dedup)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var ok = await TekilYemekhaneEkle(db, log, today, ct);
                if (!ok)
                    _logger.LogDebug("Yemekhane tekil kontrol: Aynı gün ikinci GİRİŞ yazılmadı. OgrId={O}", log.OgrenciId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Yemekhane tekil ekleme hatası: OgrId={O}", log.OgrenciId);
            }
        }

        // Bugün logu olmayan cihazların loglarını temizle
        foreach (var (ip, port) in temizlenecekCihazlar)
            CihazLogTemizle(ip, port);
    }

    /// <summary>
    /// Tek bir ZKTeco cihazından bugünün loglarını okur.
    /// COM nesnesi bu metot içinde oluşturulur ve temizlenir.
    /// </summary>
    private (List<OgrenciDetayModel> YeniLoglar, bool CihazOk, bool BugunLogVar) CihazdanLogCek(
        int cihazId, string ip, int port,
        DateTime today, HashSet<int> gecerliSet, HashSet<int> mevcutSet)
    {
        var yeniLoglar = new List<OgrenciDetayModel>();
        bool cihazOk = false;
        bool bugunLogVar = false;

        CZKEM? zkem = null;
        bool connected = false;

        try
        {
            zkem = new CZKEM();
            _logger.LogDebug("Cihaza bağlanılıyor: {Ip}:{Port}", ip, port);
            connected = zkem.Connect_Net(ip, port);
            if (!connected)
            {
                int code = 0; zkem.GetLastError(ref code);
                _logger.LogWarning("Yemekhane cihazına bağlanılamadı: {Ip}:{Port} Kod={Code}", ip, port, code);
                return (yeniLoglar, false, false);
            }
            _logger.LogDebug("Cihaza bağlandı: {Ip}:{Port}", ip, port);

            zkem.EnableDevice(MACHINE_NUMBER, false);

            if (!zkem.ReadGeneralLogData(MACHINE_NUMBER))
            {
                _logger.LogDebug("Cihazda log yok: {Ip}", ip);
                cihazOk = true;
                return (yeniLoglar, cihazOk, false);
            }

            _logger.LogDebug("Cihazdan loglar okunuyor. GecerliOgrSayisi={GecerliSayisi}, MevcutBugunSayisi={MevcutSayisi}", gecerliSet.Count, mevcutSet.Count);

            int dwTMachineNumber = 0, dwEnrollNumber = 0, dwEMachineNumber = 0;
            int dwVerifyMode = 0, dwInOutMode = 0;
            int dwYear = 0, dwMonth = 0, dwDay = 0, dwHour = 0, dwMinute = 0;
            int toplamLog = 0, bugunLog = 0, yemekHakkiYok = 0, zatenVar = 0;

            while (zkem.GetGeneralLogData(
                MACHINE_NUMBER,
                ref dwTMachineNumber, ref dwEnrollNumber, ref dwEMachineNumber,
                ref dwVerifyMode, ref dwInOutMode,
                ref dwYear, ref dwMonth, ref dwDay, ref dwHour, ref dwMinute))
            {
                toplamLog++;

                if (dwYear < 2000 || dwYear > DateTime.Now.Year ||
                    dwMonth is < 1 or > 12 ||
                    dwDay is < 1 or > 31)
                    continue;

                var ts = new DateTime(dwYear, dwMonth, dwDay, dwHour, dwMinute, 0);
                if (ts.Date != today) continue;

                bugunLog++;
                bugunLogVar = true;
                int ogrenciId = dwEnrollNumber;

                if (!gecerliSet.Contains(ogrenciId)) { yemekHakkiYok++; continue; }
                if (mevcutSet.Contains(ogrenciId)) { zatenVar++; continue; }

                yeniLoglar.Add(new OgrenciDetayModel
                {
                    OgrenciId = ogrenciId,
                    IstasyonTipi = IstasyonTipi.Yemekhane,
                    OgrenciGTarih = ts,
                    OgrenciCTarih = null,
                    OgrenciGecisTipi = "GİRİŞ",
                    CihazId = cihazId,
                    OgrenciSmsGonderildi = false
                });

                mevcutSet.Add(ogrenciId);
            }

            _logger.LogInformation("Cihaz özet: ToplamLog={ToplamLog}, BugünLog={BugunLog}, YemekHakkıYok={YemekHakkiYok}, ZatenVar={ZatenVar}, YeniKayıt={YeniKayit}", toplamLog, bugunLog, yemekHakkiYok, zatenVar, yeniLoglar.Count);

            cihazOk = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Yemekhane cihazından log çekme hatası: {Ip}", ip);
        }
        finally
        {
            if (zkem != null)
            {
                try
                {
                    if (connected)
                    {
                        zkem.EnableDevice(MACHINE_NUMBER, true);
                        zkem.Disconnect();
                    }
                }
                catch { }
                finally
                {
                    try { Marshal.FinalReleaseComObject(zkem); } catch { }
                }
            }
        }

        return (yeniLoglar, cihazOk, bugunLogVar);
    }

    /// <summary>
    /// sp_getapplock ile öğrenci-gün bazında tekil yemekhane giriş kaydı ekler.
    /// </summary>
    private static async Task<bool> TekilYemekhaneEkle(
        AppDbContext db, OgrenciDetayModel log, DateTime today, CancellationToken ct)
    {
        var strategy = db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            var resource = $"YEMEKHANE:{log.OgrenciId}:{today:yyyyMMdd}";

            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
            try
            {
                var lockSql = "EXEC sp_getapplock @Resource=@p0, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=5000;";
                await db.Database.ExecuteSqlRawAsync(lockSql, new object[] { resource }, ct);

                bool varMi = await db.OgrenciDetaylar.AsNoTracking()
                    .AnyAsync(x =>
                        x.OgrenciId == log.OgrenciId &&
                        x.IstasyonTipi == IstasyonTipi.Yemekhane &&
                        x.OgrenciGecisTipi == "GİRİŞ" &&
                        x.OgrenciGTarih.HasValue &&
                        x.OgrenciGTarih.Value.Date == today, ct);

                if (varMi)
                {
                    await tx.CommitAsync(ct);
                    return false;
                }

                db.OgrenciDetaylar.Add(log);
                await db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
                return true;
            }
            catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number == 1205)
            {
                await tx.RollbackAsync(ct);
                return false;
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        });
    }

    /// <summary>
    /// Cihaz loglarını temizler (bugün kaydı yoksa).
    /// </summary>
    private void CihazLogTemizle(string ip, int port)
    {
        CZKEM? z = null;
        bool connected = false;
        try
        {
            z = new CZKEM();
            connected = z.Connect_Net(ip, port);
            if (!connected) return;

            if (z.ClearGLog(MACHINE_NUMBER))
                _logger.LogInformation("Yemekhane cihaz logları temizlendi: {Ip}", ip);
            else
                _logger.LogWarning("Yemekhane cihaz logları temizlenemedi: {Ip}", ip);

            z.RefreshData(MACHINE_NUMBER);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Yemekhane log temizleme hatası: {Ip}", ip);
        }
        finally
        {
            if (z != null)
            {
                try { if (connected) z.Disconnect(); } catch { }
                finally { try { Marshal.FinalReleaseComObject(z); } catch { } }
            }
        }
    }

    // -------------------------------------------------------
    // 2) Bekleyen SMS'leri gönder
    // -------------------------------------------------------
    private async Task BekleyenSmsGonder(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var smsService = scope.ServiceProvider.GetRequiredService<ISmsService>();
        var smsAyar = scope.ServiceProvider.GetRequiredService<IOptions<SmsAyarlari>>().Value;

        if (!smsAyar.Aktif) return;

        var today = DateTime.Now.Date;

        // Bugünkü SMS gönderilmemiş yemekhane kayıtları
        var bekleyenler = await db.OgrenciDetaylar
            .Where(x => x.IstasyonTipi == IstasyonTipi.Yemekhane
                        && x.OgrenciSmsGonderildi != true
                        && x.OgrenciGTarih.HasValue
                        && x.OgrenciGTarih.Value.Date == today)
            .ToListAsync(ct);

        if (bekleyenler.Count == 0) return;

        // Öğrenci bilgilerini toplu çek
        var ogrIdler = bekleyenler.Select(p => p.OgrenciId).Distinct().ToList();

        var ogrenciBilgileri = await db.Ogrenciler.AsNoTracking()
            .Where(o => ogrIdler.Contains(o.OgrenciId) && o.VeliId != null)
            .Select(o => new
            {
                o.OgrenciId,
                o.OgrenciAdSoyad,
                VeliTelefon = o.Veli!.Telefon
            })
            .ToDictionaryAsync(o => o.OgrenciId, ct);

        foreach (var log in bekleyenler)
        {
            ct.ThrowIfCancellationRequested();

            if (!ogrenciBilgileri.TryGetValue(log.OgrenciId, out var bilgi)) continue;
            if (string.IsNullOrWhiteSpace(bilgi.VeliTelefon)) continue;

            var ts = log.OgrenciGTarih ?? DateTime.Now;
            var mesaj = $"{OkulAdi} öğrencimiz {bilgi.OgrenciAdSoyad} yemekhaneye giriş yaptı. Zaman: {ts:dd.MM.yyyy HH:mm:ss}";

            try
            {
                var sonuc = await smsService.Gonder(bilgi.VeliTelefon!, mesaj, ct);
                if (sonuc.Basarili)
                {
                    log.OgrenciSmsGonderildi = true;
                    _logger.LogInformation("[SMS OK][Yemekhane] OgrId:{OgrId}", log.OgrenciId);
                }
                else
                {
                    _logger.LogWarning("[SMS FAIL][Yemekhane] OgrId:{OgrId} Hata:{Hata}", log.OgrenciId, sonuc.Hata);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SMS EX][Yemekhane] OgrId={OgrId}", log.OgrenciId);
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
