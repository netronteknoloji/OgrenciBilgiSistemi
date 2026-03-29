using System.Data;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Retry;
using zkemkeeper;
using ZKTecoWindowsService.Data;
using ZKTecoWindowsService.Models;
using ZKTecoWindowsService.Models.Enums;
using ZKTecoWindowsService.Services;

public sealed class Worker : BackgroundService
{
    // ---- Ayarlar ----
    private const int MACHINE_NUMBER = 1;
    private const int MAX_DEGREE_OF_PARALLELISM = 3;
    private static readonly TimeSpan POLL_INTERVAL = TimeSpan.FromMinutes(1);

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<Worker> _logger;
    private readonly AsyncRetryPolicy _retryPolicy;

    public Worker(IServiceProvider serviceProvider, ILogger<Worker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        // Jitter'lı beklemeler (20 sn civarı, 3 deneme)
        var delays = Backoff.DecorrelatedJitterBackoffV2(
            medianFirstRetryDelay: TimeSpan.FromSeconds(20),
            retryCount: 3
        );

        _retryPolicy = Policy
            .Handle<Exception>(ex => ex is not OperationCanceledException)
            .WaitAndRetryAsync(
                sleepDurations: delays,
                onRetry: (ex, delay, i, _) =>
                {
                    _logger.LogWarning(ex, "[Retry {Retry}] Hata. {Delay} sonra tekrar.", i, delay);
                });
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker başlatıldı.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // 1) Yemekhane: ZKTeco’dan sadece BUGÜN’ÜN İLK GİRİŞLERİNİ DB’ye ekle (SMS burada atılmaz)
                await _retryPolicy.ExecuteAsync(ct => ProcessYemekhaneFromZktecoAsync(ct), stoppingToken);

                // 2) SMS Dispatcher: Bugünün tüm pending (Yemekhane + AnaKapi) kayıtlarına SMS gönder
                await _retryPolicy.ExecuteAsync(ct => SendPendingSmsAsync(ct), stoppingToken);

                _logger.LogInformation("Tur tamamlandı.");
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { _logger.LogError(ex, "Tur sırasında beklenmeyen hata!"); }

            try { await Task.Delay(POLL_INTERVAL, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }

        _logger.LogInformation("Worker durduruluyor.");
    }

    // --------------------------
    // 1) Yemekhane (ZKTeco)
    // --------------------------
    private sealed record DeviceProcessResult(CihazModel Device, List<OgrenciDetayModel> NewLogs, bool DeviceOk, bool AnyTodayLog);

    private async Task ProcessYemekhaneFromZktecoAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Sadece aktif, ZKTeco ve Yemekhane cihazları (IP/Port zorunlu)
        var cihazlar = await context.Cihazlar.AsNoTracking()
            .Where(c => c.Aktif
                     && c.DonanimTipi == DonanimTipi.ZKTeco
                     && c.IstasyonTipi == IstasyonTipi.Yemekhane
                     && c.IpAdresi != null
                     && c.PortNo.HasValue && c.PortNo.Value > 0)
            .Select(c => new CihazModel
            {
                CihazId = c.CihazId,
                CihazAdi = c.CihazAdi,
                IpAdresi = c.IpAdresi,
                PortNo = c.PortNo,
                IstasyonTipi = c.IstasyonTipi,
                DonanimTipi = c.DonanimTipi,
                Aktif = c.Aktif
            })
            .ToListAsync(ct);

        if (cihazlar.Count == 0)
        {
            _logger.LogInformation("Yemekhane ZKTeco cihazı bulunamadı.");
            return;
        }

        var results = new List<DeviceProcessResult>(cihazlar.Count);

        // Cihazları paralel işle (IO-bound; COM çağrıları izole)
        await Parallel.ForEachAsync(
            source: cihazlar,
            new ParallelOptions { CancellationToken = ct, MaxDegreeOfParallelism = MAX_DEGREE_OF_PARALLELISM },
            async (cihaz, token) =>
            {
                try
                {
                    var res = await CihazdanTumYemekhaneLogIsleAsync(cihaz, token);
                    lock (results) results.Add(res);
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Yemekhane cihazı işlenirken hata: {Ip}", cihaz.IpAdresi);
                    lock (results) results.Add(new DeviceProcessResult(cihaz, new List<OgrenciDetayModel>(), DeviceOk: false, AnyTodayLog: false));
                }
            });

        // Cihazlardan toplanan yeni loglar
        var toInsert = results.SelectMany(r => r.NewLogs).ToList();

        // Sadece BUGÜN ve Öğrenci + Gün bazında tekilleştir (farklı cihazlardan gelse bile GÜNDE 1 giriş)
        var today = DateTime.Now.Date;
        var dedup = toInsert
            .Where(x => x.OgrenciGTarih.HasValue && x.OgrenciGTarih.Value.Date == today)
            .GroupBy(x => new { x.OgrenciId, Day = x.OgrenciGTarih!.Value.Date })
            .Select(g => g.OrderBy(z => z.OgrenciGTarih).First())
            .ToList();

        // Index kullanmadan kesin tekillik: sp_getapplock ile öğrenci-gün anahtarına kilit alarak tek tek ekle
        if (dedup.Count > 0)
        {
            foreach (var log in dedup)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    var ok = await InsertYemekhaneOnceAsync(context, log, today, ct);
                    if (!ok)
                    {
                        _logger.LogDebug("Yemekhane tekil kontrol: Aynı gün ikinci GİRİŞ yazılmadı. OgrId={O}", log.OgrenciId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Yemekhane tekil ekleme hatası: OgrId={O}", log.OgrenciId);
                }
            }
        }

        // ---- TEMİZLİK: "Bugün cihazda kayıt YOKSA" temizle ----
        bool allOk = results.All(r => r.DeviceOk);
        if (allOk)
        {
            foreach (var r in results)
            {
                if (r.AnyTodayLog) continue; // bugün kaydı varsa temizlik yapma
                await SafeClearDeviceLogsAsync(r.Device, ct);
            }
        }
    }

    private async Task<DeviceProcessResult> CihazdanTumYemekhaneLogIsleAsync(CihazModel cihaz, CancellationToken ct)
    {
        var newLogs = new List<OgrenciDetayModel>();
        bool deviceOk = false;
        bool anyTodayLog = false;

        // BUGÜN (server saati)
        var today = DateTime.Now.Date;

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // ---- Yemek hakkı: sadece BUGÜNÜN AY/YIL için aktif olanlar ----
        // OgrenciYemekler: Ay (int), Yil (int), Aktif (bool) varsayıldı
        var validStudentIds = await context.OgrenciYemekler
            .AsNoTracking()
            .Where(y => y.Aktif == true
                        && y.Ay == today.Month
                        && y.Yil == today.Year)
            .Select(y => y.OgrenciId)
            .Distinct()
            .ToListAsync(ct);
        var validSet = new HashSet<int>(validStudentIds);

        // BUGÜN Yemekhane için zaten GİRİŞ’i olanlar (cihaz bağımsız)
        var existingToday = await context.OgrenciDetaylar
            .AsNoTracking()
            .Where(x => x.IstasyonTipi == IstasyonTipi.Yemekhane
                        && x.OgrenciGecisTipi == "GİRİŞ"
                        && x.OgrenciGTarih.HasValue
                        && x.OgrenciGTarih.Value.Date == today)
            .Select(x => x.OgrenciId)
            .Distinct()
            .ToListAsync(ct);
        var existingSet = new HashSet<int>(existingToday);

        CZKEM? zkem = null;
        bool connected = false;

        try
        {
            zkem = new CZKEM();
            connected = zkem.Connect_Net(cihaz.IpAdresi!, cihaz.PortNo!.Value);
            if (!connected)
            {
                int code = 0; zkem.GetLastError(ref code);
                _logger.LogWarning("Cihaza bağlanılamadı (SC403/Yemekhane): {Ip}:{Port} Kod={Code}", cihaz.IpAdresi, cihaz.PortNo, code);
                return new DeviceProcessResult(cihaz, newLogs, DeviceOk: false, AnyTodayLog: false);
            }

            if (!zkem.EnableDevice(MACHINE_NUMBER, false))
                _logger.LogWarning("Cihaz devre dışı bırakılamadı: {Ip}", cihaz.IpAdresi);

            if (!zkem.ReadGeneralLogData(MACHINE_NUMBER))
            {
                _logger.LogInformation("Cihazda log yok (SC403/Yemekhane): {Ip}:{Port}", cihaz.IpAdresi, cihaz.PortNo);
                deviceOk = true;
                return new DeviceProcessResult(cihaz, newLogs, deviceOk, AnyTodayLog: false);
            }

            // SC403 imzası
            int dwTMachineNumber = 0;
            int dwEnrollNumber = 0;
            int dwEMachineNumber = 0;
            int dwVerifyMode = 0;
            int dwInOutMode = 0;
            int dwYear = 0, dwMonth = 0, dwDay = 0, dwHour = 0, dwMinute = 0;

            while (zkem.GetGeneralLogData(
                MACHINE_NUMBER,
                ref dwTMachineNumber,
                ref dwEnrollNumber,
                ref dwEMachineNumber,
                ref dwVerifyMode,
                ref dwInOutMode,
                ref dwYear, ref dwMonth, ref dwDay,
                ref dwHour, ref dwMinute))
            {
                ct.ThrowIfCancellationRequested();

                // defansif tarih kontrolü
                if (dwYear < 2000 || dwYear > DateTime.Now.Year ||
                    dwMonth is < 1 or > 12 ||
                    dwDay is < 1 or > 31)
                {
                    _logger.LogWarning("Geçersiz tarih atlandı: {Y}-{M}-{D} {H}:{Min}",
                        dwYear, dwMonth, dwDay, dwHour, dwMinute);
                    continue;
                }

                var ts = new DateTime(dwYear, dwMonth, dwDay, dwHour, dwMinute, 0);

                // >>> SADECE BUGÜN
                if (ts.Date != today) continue;

                anyTodayLog = true;
                int ogrenciId = dwEnrollNumber;

                // hızlı kontroller (bellek içi)
                if (!validSet.Contains(ogrenciId)) continue;          // bugün yemek hakkı yoksa alma
                if (existingSet.Contains(ogrenciId)) continue;         // bugün zaten GİRİŞ yazıldıysa alma

                // yalnızca "GİRİŞ" yaz (çıkışı yazmıyoruz)
                newLogs.Add(new OgrenciDetayModel
                {
                    OgrenciId = ogrenciId,
                    IstasyonTipi = IstasyonTipi.Yemekhane,
                    OgrenciGTarih = ts,        // sadece giriş tarihi
                    OgrenciCTarih = null,
                    OgrenciGecisTipi = "GİRİŞ",
                    CihazId = cihaz.CihazId,
                    OgrenciSmsGonderildi = false
                });

                existingSet.Add(ogrenciId);
            }

            deviceOk = true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SC403 (Yemekhane) cihazından log çekme hatası: {Ip}", cihaz.IpAdresi);
            deviceOk = false;
        }
        finally
        {
            if (zkem != null)
            {
                try
                {
                    if (connected)
                    {
                        try
                        {
                            if (!zkem.EnableDevice(MACHINE_NUMBER, true))
                                _logger.LogWarning("Cihaz tekrar etkinleştirilemedi: {Ip}", cihaz.IpAdresi);
                            zkem.Disconnect();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Bağlantı sonlandırılırken hata: {Ip}", cihaz.IpAdresi);
                        }
                    }
                }
                finally
                {
                    try { Marshal.FinalReleaseComObject(zkem); } catch { /* ignore */ }
                }
            }
        }

        return new DeviceProcessResult(cihaz, newLogs, deviceOk, anyTodayLog);
    }

    private async Task SafeClearDeviceLogsAsync(CihazModel device, CancellationToken ct)
    {
        CZKEM? z = null;
        bool connected = false;
        try
        {
            z = new CZKEM();
            connected = z.Connect_Net(device.IpAdresi!, device.PortNo!.Value);
            if (!connected)
            {
                int code = 0; z.GetLastError(ref code);
                _logger.LogWarning("Temizlik için bağlanılamadı: {Ip}:{Port} Kod={Code}", device.IpAdresi, device.PortNo, code);
                return;
            }

            if (!z.ClearGLog(MACHINE_NUMBER))
                _logger.LogWarning("Yemekhane cihaz logları temizlenemedi: {Ip}", device.IpAdresi);
            else
                _logger.LogInformation("Yemekhane cihaz logları temizlendi: {Ip}", device.IpAdresi);

            z.RefreshData(MACHINE_NUMBER);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Yemekhane log temizleme hatası: {Ip}", device.IpAdresi);
        }
        finally
        {
            if (z != null)
            {
                try
                {
                    if (connected) z.Disconnect();
                }
                catch { /* ignore */ }
                finally
                {
                    try { Marshal.FinalReleaseComObject(z); } catch { /* ignore */ }
                }
            }
        }
    }

    // -----------------------------------
    // 2) SMS Dispatcher (Yemekhane + AnaKapi)
    // -----------------------------------
    private async Task SendPendingSmsAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var smsService = scope.ServiceProvider.GetRequiredService<IOgrenciSmsService>();

        var today = DateTime.Now.Date;

        // Bugünün ve SMS atılmamış tüm kayıtlar (Yemekhane + AnaKapi)
        var pending = await context.OgrenciDetaylar
            .Where(x => x.OgrenciSmsGonderildi != true &&
                        (
                            (x.OgrenciGTarih.HasValue && x.OgrenciGTarih.Value.Date == today) ||
                            (x.OgrenciCTarih.HasValue && x.OgrenciCTarih.Value.Date == today)
                        ))
            .ToListAsync(ct);

        if (pending.Count == 0) return;

        // Öğrencileri tek seferde çek → dictionary
        var ogrIds = pending.Select(p => p.OgrenciId).Distinct().ToList();
        var ogrenciler = await context.Ogrenciler
            .AsNoTracking()
            .Where(o => ogrIds.Contains(o.OgrenciId))
            .ToDictionaryAsync(o => o.OgrenciId, ct);

        foreach (var log in pending)
        {
            ct.ThrowIfCancellationRequested();
            if (!ogrenciler.TryGetValue(log.OgrenciId, out var ogrenci)) continue;
            if (string.IsNullOrWhiteSpace(ogrenci.OgrenciVeliTelefon)) continue;

            // --- Mesajı doğrudan DB alanlarına göre, kapı bazlı üret ---
            string mesaj = BuildSmsMessage(log, ogrenci);

            try
            {
                var res = await smsService.SendSmsAsync(ogrenci.OgrenciVeliTelefon!, mesaj, ct);
                if (res.Success)
                {
                    log.OgrenciSmsGonderildi = true;
                    _logger.LogInformation("[SMS OK][{Ist}] OgrId:{OgrId}", log.IstasyonTipi, ogrenci.OgrenciId);
                }
                else
                {
                    _logger.LogWarning("[SMS FAIL][{Ist}] OgrId:{OgrId} Http:{Code} Err:{Err}",
                        log.IstasyonTipi, ogrenci.OgrenciId, res.StatusCode, res.Error ?? "-");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SMS EX][{Ist}] OgrId={OgrId}", log.IstasyonTipi, ogrenci.OgrenciId);
            }
        }

        await context.SaveChangesAsync(ct);
    }

    // --------------------------
    // Yardımcılar
    // --------------------------

    private const string OkulAdi = "Abdülhamit Han Ortaokulu";
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    private static string NormalizeGecisTipi(string? tip)
    {
        if (string.IsNullOrWhiteSpace(tip)) return "GİRİŞ";
        tip = tip.Trim().ToUpper(Tr);
        return tip == "ÇIKIŞ" ? "ÇIKIŞ" : "GİRİŞ";
    }

    private static DateTime SelectTs(DateTime? g, DateTime? c)
    {
        // Artık TR saat dilimi kullanılmıyor; server saati esas
        return g ?? c ?? DateTime.Now;
    }

    private static string BuildSmsMessage(OgrenciDetayModel log, OgrenciModel ogrenci)
    {
        var tip = NormalizeGecisTipi(log.OgrenciGecisTipi);
        var ts = SelectTs(log.OgrenciGTarih, log.OgrenciCTarih);

        return log.IstasyonTipi switch
        {
            IstasyonTipi.Yemekhane => tip == "ÇIKIŞ"
                ? $"{OkulAdi} öğrencimiz {ogrenci.OgrenciAdSoyad} yemekhaneden çıkış yaptı. Zaman: {ts:dd.MM.yyyy HH:mm:ss}"
                : $"{OkulAdi} öğrencimiz {ogrenci.OgrenciAdSoyad} yemekhaneye giriş yaptı. Zaman: {ts:dd.MM.yyyy HH:mm:ss}",

            IstasyonTipi.AnaKapi => tip == "ÇIKIŞ"
                ? $"{OkulAdi} öğrencimiz {ogrenci.OgrenciAdSoyad} okuldan çıkış yaptı. Zaman: {ts:dd.MM.yyyy HH:mm:ss}"
                : $"{OkulAdi} öğrencimiz {ogrenci.OgrenciAdSoyad} okula giriş yaptı. Zaman: {ts:dd.MM.yyyy HH:mm:ss}",

            _ => tip == "ÇIKIŞ"
                ? $"{OkulAdi} öğrencimiz {ogrenci.OgrenciAdSoyad} çıkış yaptı. Zaman: {ts:dd.MM.yyyy HH:mm:ss}"
                : $"{OkulAdi} öğrencimiz {ogrenci.OgrenciAdSoyad} giriş yaptı. Zaman: {ts:dd.MM.yyyy HH:mm:ss}",
        };
    }

    /// <summary>
    /// Index olmadan tekillik: (OgrenciId, Gün) için uygulama-kilidi alıp,
    /// transaction içinde "var mı?" kontrol ederek ekleme yapar.
    /// </summary>
    private static async Task<bool> InsertYemekhaneOnceAsync(
        AppDbContext context,
        OgrenciDetayModel log,   // IstasyonTipi=Yemekhane, OgrenciGecisTipi="GİRİŞ", OgrenciGTarih bugünün bir saati
        DateTime today,
        CancellationToken ct)
    {
        var resource = $"YEMEKHANE:{log.OgrenciId}:{today:yyyyMMdd}";

        await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        try
        {
            // 1) Uygulama kilidi (5 sn bekle)
            var lockSql = "EXEC sp_getapplock @Resource=@p0, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=5000;";
            await context.Database.ExecuteSqlRawAsync(lockSql, [resource], ct);

            // 2) Kesin kontrol — aynı gün GİRİŞ var mı?
            bool exists = await context.OgrenciDetaylar
                .AsNoTracking()
                .AnyAsync(x =>
                    x.OgrenciId == log.OgrenciId &&
                    x.IstasyonTipi == IstasyonTipi.Yemekhane &&
                    x.OgrenciGecisTipi == "GİRİŞ" &&
                    x.OgrenciGTarih.HasValue &&
                    x.OgrenciGTarih.Value.Date == today, ct);

            if (exists)
            {
                await tx.CommitAsync(ct);   // yazmadan çık
                return false;
            }

            // 3) Yaz ve commit
            context.OgrenciDetaylar.Add(log);
            await context.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return true;
        }
        catch (SqlException ex) when (ex.Number == 1205) // deadlock emniyeti
        {
            await tx.RollbackAsync(ct);
            return false; // istersen yeniden deneyebilirsin
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
}

//using System.Globalization;
//using System.Runtime.InteropServices;
//using Microsoft.EntityFrameworkCore;
//using Polly;
//using Polly.Contrib.WaitAndRetry;
//using Polly.Retry;
//using zkemkeeper;
//using ZKTecoWindowsService.Data;
//using ZKTecoWindowsService.Models;
//using ZKTecoWindowsService.Models.Enums;
//using ZKTecoWindowsService.Services;

//public sealed class Worker : BackgroundService
//{
//    // ---- Ayarlar ----
//    private const int MACHINE_NUMBER = 1;
//    private const int MAX_DEGREE_OF_PARALLELISM = 3;
//    private static readonly TimeSpan POLL_INTERVAL = TimeSpan.FromMinutes(1);

//    private readonly IServiceProvider _serviceProvider;
//    private readonly ILogger<Worker> _logger;
//    private readonly AsyncRetryPolicy _retryPolicy;

//    public Worker(IServiceProvider serviceProvider, ILogger<Worker> logger)
//    {
//        _serviceProvider = serviceProvider;
//        _logger = logger;

//        // Jitter'lı beklemeler (20 sn civarı, 3 deneme)
//        var delays = Backoff.DecorrelatedJitterBackoffV2(
//            medianFirstRetryDelay: TimeSpan.FromSeconds(20),
//            retryCount: 3
//        );

//        _retryPolicy = Policy
//            .Handle<Exception>(ex => ex is not OperationCanceledException)
//            .WaitAndRetryAsync(
//                sleepDurations: delays,
//                onRetry: (ex, delay, i, _) =>
//                {
//                    _logger.LogWarning(ex, "[Retry {Retry}] Hata. {Delay} sonra tekrar.", i, delay);
//                });
//    }

//    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//    {
//        _logger.LogInformation("Worker başlatıldı.");

//        while (!stoppingToken.IsCancellationRequested)
//        {
//            try
//            {
//                // 1) Yemekhane: ZKTeco’dan sadece İLK GİRİŞLERİ DB’ye ekle (SMS burada atılmaz)
//                await _retryPolicy.ExecuteAsync(ct => ProcessYemekhaneFromZktecoAsync(ct), stoppingToken);

//                // 2) SMS Dispatcher: Bugünün tüm pending (Yemekhane + AnaKapi) kayıtlarına SMS gönder
//                await _retryPolicy.ExecuteAsync(ct => SendPendingSmsAsync(ct), stoppingToken);

//                _logger.LogInformation("Tur tamamlandı.");
//            }
//            catch (OperationCanceledException) { break; }
//            catch (Exception ex) { _logger.LogError(ex, "Tur sırasında beklenmeyen hata!"); }

//            try { await Task.Delay(POLL_INTERVAL, stoppingToken); }
//            catch (OperationCanceledException) { break; }
//        }

//        _logger.LogInformation("Worker durduruluyor.");
//    }

//    // --------------------------
//    // 1) Yemekhane (ZKTeco)
//    // --------------------------
//    private sealed record DeviceProcessResult(CihazModel Device, List<OgrenciDetayModel> NewLogs, bool DeviceOk, bool AnyTodayLog);

//    private async Task ProcessYemekhaneFromZktecoAsync(CancellationToken ct)
//    {
//        using var scope = _serviceProvider.CreateScope();
//        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

//        // Sadece aktif, ZKTeco ve Yemekhane cihazları (IP/Port zorunlu)
//        var cihazlar = await context.Cihazlar.AsNoTracking()
//            .Where(c => c.Aktif
//                     && c.DonanimTipi == DonanimTipi.ZKTeco
//                     && c.IstasyonTipi == IstasyonTipi.Yemekhane
//                     && c.IpAdresi != null
//                     && c.PortNo.HasValue && c.PortNo.Value > 0)
//            .Select(c => new CihazModel
//            {
//                CihazId = c.CihazId,
//                CihazAdi = c.CihazAdi,
//                IpAdresi = c.IpAdresi,
//                PortNo = c.PortNo,
//                IstasyonTipi = c.IstasyonTipi,
//                DonanimTipi = c.DonanimTipi,
//                Aktif = c.Aktif
//            })
//            .ToListAsync(ct);

//        if (cihazlar.Count == 0)
//        {
//            _logger.LogInformation("Yemekhane ZKTeco cihazı bulunamadı.");
//            return;
//        }

//        var results = new List<DeviceProcessResult>(cihazlar.Count);

//        // Cihazları paralel işle (IO-bound; COM çağrıları izole)
//        await Parallel.ForEachAsync(
//            source: cihazlar,
//            new ParallelOptions { CancellationToken = ct, MaxDegreeOfParallelism = MAX_DEGREE_OF_PARALLELISM },
//            async (cihaz, token) =>
//            {
//                try
//                {
//                    var res = await CihazdanTumYemekhaneLogIsleAsync(cihaz, token);
//                    lock (results) results.Add(res);
//                }
//                catch (OperationCanceledException) { throw; }
//                catch (Exception ex)
//                {
//                    _logger.LogError(ex, "Yemekhane cihazı işlenirken hata: {Ip}", cihaz.IpAdresi);
//                    lock (results) results.Add(new DeviceProcessResult(cihaz, new List<OgrenciDetayModel>(), DeviceOk: false, AnyTodayLog: false));
//                }
//            });

//        // Cihazlardan toplanan yeni loglar
//        var toInsert = results.SelectMany(r => r.NewLogs).ToList();

//        // Öğrenci + Gün bazında tekilleştir (farklı cihazlardan gelse bile GÜNDE 1 giriş)
//        var dedup = toInsert
//            .GroupBy(x => new { x.OgrenciId, Day = x.OgrenciGTarih!.Value.Date })
//            .Select(g => g.OrderBy(z => z.OgrenciGTarih).First())
//            .ToList();

//        if (dedup.Count > 0)
//        {
//            var strategy = context.Database.CreateExecutionStrategy();
//            await strategy.ExecuteAsync(async () =>
//            {
//                await using var tx = await context.Database.BeginTransactionAsync(ct);
//                try
//                {
//                    await context.OgrenciDetaylar.AddRangeAsync(dedup, ct);
//                    await context.SaveChangesAsync(ct);
//                    await tx.CommitAsync(ct);
//                }
//                catch
//                {
//                    await tx.RollbackAsync(ct);
//                    throw;
//                }
//            });
//        }

//        // ---- TEMİZLİK (1. adım): "Bugün cihazda kayıt YOKSA" temizle ----
//        // Böylece fiilen “dün ve öncesi” silinmiş olur; bugünlük kayıtlar etkilenmez.
//        bool allOk = results.All(r => r.DeviceOk);
//        if (allOk)
//        {
//            foreach (var r in results)
//            {
//                if (r.AnyTodayLog) continue; // bugün kaydı varsa temizlik yapma
//                await SafeClearDeviceLogsAsync(r.Device, ct);
//            }
//        }
//    }

//    private async Task<DeviceProcessResult> CihazdanTumYemekhaneLogIsleAsync(CihazModel cihaz, CancellationToken ct)
//    {
//        var newLogs = new List<OgrenciDetayModel>();
//        bool deviceOk = false;
//        bool anyTodayLog = false;

//        var today = TodayInTr();
//        var tomorrow = today.AddDays(1);

//        using var scope = _serviceProvider.CreateScope();
//        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

//        // 1) Geçerli öğrenciler → HashSet (tercihen sadece aktif öğrenciler)
//        var validStudentIds = await context.Ogrenciler
//            .AsNoTracking()
//            .Where(o => o.OgrenciDurum)
//            .Select(o => o.OgrenciId)
//            .ToListAsync(ct);
//        var validSet = new HashSet<int>(validStudentIds);

//        // 2) BUGÜN Yemekhane için zaten kaydı olan öğrenciler (CIHAZ BAĞIMSIZ!)
//        var existingToday = await context.OgrenciDetaylar
//            .AsNoTracking()
//            .Where(x => x.IstasyonTipi == IstasyonTipi.Yemekhane
//                        && x.OgrenciGTarih >= today && x.OgrenciGTarih < tomorrow)
//            .Select(x => x.OgrenciId)
//            .Distinct()
//            .ToListAsync(ct);
//        var existingSet = new HashSet<int>(existingToday);

//        CZKEM? zkem = null;
//        bool connected = false;

//        try
//        {
//            zkem = new CZKEM();
//            connected = zkem.Connect_Net(cihaz.IpAdresi!, cihaz.PortNo!.Value);
//            if (!connected)
//            {
//                int code = 0; zkem.GetLastError(ref code);
//                _logger.LogWarning("Cihaza bağlanılamadı (SC403/Yemekhane): {Ip}:{Port} Kod={Code}", cihaz.IpAdresi, cihaz.PortNo, code);
//                return new DeviceProcessResult(cihaz, newLogs, DeviceOk: false, AnyTodayLog: false);
//            }

//            if (!zkem.EnableDevice(MACHINE_NUMBER, false))
//                _logger.LogWarning("Cihaz devre dışı bırakılamadı: {Ip}", cihaz.IpAdresi);

//            if (!zkem.ReadGeneralLogData(MACHINE_NUMBER))
//            {
//                _logger.LogInformation("Cihazda log yok (SC403/Yemekhane): {Ip}:{Port}", cihaz.IpAdresi, cihaz.PortNo);
//                deviceOk = true;
//                return new DeviceProcessResult(cihaz, newLogs, deviceOk, AnyTodayLog: false);
//            }

//            int dwTMachineNumber = 0;
//            int dwEnrollNumber = 0;
//            int dwEMachineNumber = 0;
//            int dwVerifyMode = 0;
//            int dwInOutMode = 0;
//            int dwYear = 0, dwMonth = 0, dwDay = 0, dwHour = 0, dwMinute = 0;

//            while (zkem.GetGeneralLogData(
//                MACHINE_NUMBER,
//                ref dwTMachineNumber,
//                ref dwEnrollNumber,
//                ref dwEMachineNumber,
//                ref dwVerifyMode,
//                ref dwInOutMode,
//                ref dwYear, ref dwMonth, ref dwDay,
//                ref dwHour, ref dwMinute))
//            {
//                ct.ThrowIfCancellationRequested();

//                // defansif tarih kontrolü
//                if (dwYear < 2000 || dwYear > DateTime.Now.Year ||
//                    dwMonth is < 1 or > 12 ||
//                    dwDay is < 1 or > 31)
//                {
//                    _logger.LogWarning("Geçersiz tarih atlandı: {Y}-{M}-{D} {H}:{Min}",
//                        dwYear, dwMonth, dwDay, dwHour, dwMinute);
//                    continue;
//                }

//                var ts = new DateTime(dwYear, dwMonth, dwDay, dwHour, dwMinute, 0);

//                // >>> SADECE BUGÜN
//                if (ts.Date != today) continue;

//                anyTodayLog = true;
//                int ogrenciId = dwEnrollNumber;

//                // hızlı kontroller (bellek içi)
//                if (!validSet.Contains(ogrenciId)) continue;

//                // bugün yemekhane için zaten kaydı varsa atla (CIHAZDAN BAĞIMSIZ)
//                if (existingSet.Contains(ogrenciId)) continue;

//                // yalnızca "GİRİŞ" yaz (çıkışı yazmıyoruz)
//                newLogs.Add(new OgrenciDetayModel
//                {
//                    OgrenciId = ogrenciId,
//                    IstasyonTipi = IstasyonTipi.Yemekhane,
//                    OgrenciGTarih = ts,
//                    OgrenciCTarih = null,
//                    OgrenciGecisTipi = "GİRİŞ",
//                    CihazId = cihaz.CihazId,
//                    OgrenciSmsGonderildi = false
//                });

//                existingSet.Add(ogrenciId);
//            }

//            deviceOk = true;
//        }
//        catch (OperationCanceledException) { throw; }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "SC403 (Yemekhane) cihazından log çekme hatası: {Ip}", cihaz.IpAdresi);
//            deviceOk = false;
//        }
//        finally
//        {
//            if (zkem != null)
//            {
//                try
//                {
//                    if (connected)
//                    {
//                        try
//                        {
//                            if (!zkem.EnableDevice(MACHINE_NUMBER, true))
//                                _logger.LogWarning("Cihaz tekrar etkinleştirilemedi: {Ip}", cihaz.IpAdresi);
//                            zkem.Disconnect();
//                        }
//                        catch (Exception ex)
//                        {
//                            _logger.LogError(ex, "Bağlantı sonlandırılırken hata: {Ip}", cihaz.IpAdresi);
//                        }
//                    }
//                }
//                finally
//                {
//                    try { Marshal.FinalReleaseComObject(zkem); } catch { /* ignore */ }
//                }
//            }
//        }

//        return new DeviceProcessResult(cihaz, newLogs, deviceOk, anyTodayLog);
//    }

//    private async Task SafeClearDeviceLogsAsync(CihazModel device, CancellationToken ct)
//    {
//        CZKEM? z = null;
//        bool connected = false;
//        try
//        {
//            z = new CZKEM();
//            connected = z.Connect_Net(device.IpAdresi!, device.PortNo!.Value);
//            if (!connected)
//            {
//                int code = 0; z.GetLastError(ref code);
//                _logger.LogWarning("Temizlik için bağlanılamadı: {Ip}:{Port} Kod={Code}", device.IpAdresi, device.PortNo, code);
//                return;
//            }

//            if (!z.ClearGLog(MACHINE_NUMBER))
//                _logger.LogWarning("Yemekhane cihaz logları temizlenemedi: {Ip}", device.IpAdresi);
//            else
//                _logger.LogInformation("Yemekhane cihaz logları temizlendi: {Ip}", device.IpAdresi);

//            z.RefreshData(MACHINE_NUMBER);
//            await Task.CompletedTask;
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Yemekhane log temizleme hatası: {Ip}", device.IpAdresi);
//        }
//        finally
//        {
//            if (z != null)
//            {
//                try
//                {
//                    if (connected) z.Disconnect();
//                }
//                catch { /* ignore */ }
//                finally
//                {
//                    try { Marshal.FinalReleaseComObject(z); } catch { /* ignore */ }
//                }
//            }
//        }
//    }

//    // -----------------------------------
//    // 2) SMS Dispatcher (Yemekhane + AnaKapi)
//    // -----------------------------------
//    private async Task SendPendingSmsAsync(CancellationToken ct)
//    {
//        using var scope = _serviceProvider.CreateScope();
//        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
//        var smsService = scope.ServiceProvider.GetRequiredService<IOgrenciSmsService>();

//        var today = TodayInTr();
//        var tomorrow = today.AddDays(1);

//        // Bugünün ve SMS atılmamış tüm kayıtlar (Yemekhane + AnaKapi)
//        var pending = await context.OgrenciDetaylar
//            .Where(x => x.OgrenciSmsGonderildi != true &&
//                   (
//                       (x.OgrenciGTarih != null && x.OgrenciGTarih >= today && x.OgrenciGTarih < tomorrow) ||
//                       (x.OgrenciCTarih != null && x.OgrenciCTarih >= today && x.OgrenciCTarih < tomorrow)
//                   ))
//            .ToListAsync(ct);

//        if (pending.Count == 0) return;

//        // Öğrencileri tek seferde çek → dictionary
//        var ogrIds = pending.Select(p => p.OgrenciId).Distinct().ToList();
//        var ogrenciler = await context.Ogrenciler
//            .AsNoTracking()
//            .Where(o => ogrIds.Contains(o.OgrenciId))
//            .ToDictionaryAsync(o => o.OgrenciId, ct);

//        foreach (var log in pending)
//        {
//            ct.ThrowIfCancellationRequested();
//            if (!ogrenciler.TryGetValue(log.OgrenciId, out var ogrenci)) continue;
//            if (string.IsNullOrWhiteSpace(ogrenci.OgrenciVeliTelefon)) continue;

//            // --- Mesajı doğrudan DB alanlarına göre, kapı bazlı üret ---
//            string mesaj = BuildSmsMessage(log, ogrenci);

//            try
//            {
//                var res = await smsService.SendSmsAsync(ogrenci.OgrenciVeliTelefon!, mesaj, ct);
//                if (res.Success)
//                {
//                    log.OgrenciSmsGonderildi = true;
//                    _logger.LogInformation("[SMS OK][{Ist}] OgrId:{OgrId}", log.IstasyonTipi, ogrenci.OgrenciId);
//                }
//                else
//                {
//                    _logger.LogWarning("[SMS FAIL][{Ist}] OgrId:{OgrId} Http:{Code} Err:{Err}",
//                        log.IstasyonTipi, ogrenci.OgrenciId, res.StatusCode, res.Error ?? "-");
//                }
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "[SMS EX][{Ist}] OgrId={OgrId}", log.IstasyonTipi, ogrenci.OgrenciId);
//            }
//        }

//        await context.SaveChangesAsync(ct);
//    }

//    // --------------------------
//    // Yardımcılar: TR saat, geçiş tipi normalize ve mesaj inşası
//    // --------------------------

//    private const string OkulAdi = "Abdülhamit Han Ortaokulu";
//    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

//    private static string NormalizeGecisTipi(string? tip)
//    {
//        if (string.IsNullOrWhiteSpace(tip)) return "GİRİŞ";
//        tip = tip.Trim().ToUpper(Tr);
//        return tip == "ÇIKIŞ" ? "ÇIKIŞ" : "GİRİŞ";
//    }

//    private static DateTime SelectTsTr(DateTime? g, DateTime? c)
//    {
//        var tz = GetTurkeyTimeZone();
//        var nowTr = TimeZoneInfo.ConvertTime(DateTime.UtcNow, tz);
//        return g ?? c ?? nowTr;
//    }

//    private static string BuildSmsMessage(OgrenciDetayModel log, OgrenciModel ogrenci)
//    {
//        var tip = NormalizeGecisTipi(log.OgrenciGecisTipi);
//        var ts = SelectTsTr(log.OgrenciGTarih, log.OgrenciCTarih);

//        return log.IstasyonTipi switch
//        {
//            IstasyonTipi.Yemekhane => tip == "ÇIKIŞ"
//                ? $"{OkulAdi} öğrencimiz {ogrenci.OgrenciAdSoyad} yemekhaneden çıkış yaptı. Zaman: {ts:dd.MM.yyyy HH:mm:ss}"
//                : $"{OkulAdi} öğrencimiz {ogrenci.OgrenciAdSoyad} yemekhaneye giriş yaptı. Zaman: {ts:dd.MM.yyyy HH:mm:ss}",

//            IstasyonTipi.AnaKapi => tip == "ÇIKIŞ"
//                ? $"{OkulAdi} öğrencimiz {ogrenci.OgrenciAdSoyad} okuldan çıkış yaptı. Zaman: {ts:dd.MM.yyyy HH:mm:ss}"
//                : $"{OkulAdi} öğrencimiz {ogrenci.OgrenciAdSoyad} okula giriş yaptı. Zaman: {ts:dd.MM.yyyy HH:mm:ss}",

//            _ => tip == "ÇIKIŞ"
//                ? $"{OkulAdi} öğrencimiz {ogrenci.OgrenciAdSoyad} çıkış yaptı. Zaman: {ts:dd.MM.yyyy HH:mm:ss}"
//                : $"{OkulAdi} öğrencimiz {ogrenci.OgrenciAdSoyad} giriş yaptı. Zaman: {ts:dd.MM.yyyy HH:mm:ss}",
//        };
//    }

//    // --------------------------
//    // Yardımcı: Türkiye saatine göre "bugün"
//    // --------------------------
//    private static DateTime TodayInTr()
//    {
//        var tz = GetTurkeyTimeZone();
//        var nowTr = TimeZoneInfo.ConvertTime(DateTime.UtcNow, tz);
//        return nowTr.Date;
//    }

//    private static TimeZoneInfo GetTurkeyTimeZone()
//    {
//        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
//            return TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
//        return TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
//    }
//}


////using System.Runtime.InteropServices;
////using Microsoft.EntityFrameworkCore;
////using Polly;
////using Polly.Contrib.WaitAndRetry;
////using Polly.Retry;
////using zkemkeeper;
////using ZKTecoWindowsService.Data;
////using ZKTecoWindowsService.Models;
////using ZKTecoWindowsService.Models.Enums;
////using ZKTecoWindowsService.Services;

////public sealed class Worker : BackgroundService
////{
////    // ---- Ayarlar ----
////    private const int MACHINE_NUMBER = 1;
////    private const int MAX_DEGREE_OF_PARALLELISM = 3;      // aynı anda en fazla 3 cihaz
////    private static readonly TimeSpan POLL_INTERVAL = TimeSpan.FromMinutes(1);

////    private readonly IServiceProvider _serviceProvider;
////    private readonly ILogger<Worker> _logger;
////    private readonly AsyncRetryPolicy _retryPolicy;

////    public Worker(IServiceProvider serviceProvider, ILogger<Worker> logger)
////    {
////        _serviceProvider = serviceProvider;
////        _logger = logger;

////        // Jitter'lı beklemeler (20s civarı, 3 deneme)
////        var delays = Backoff.DecorrelatedJitterBackoffV2(
////            medianFirstRetryDelay: TimeSpan.FromSeconds(20),
////            retryCount: 3);

////        _retryPolicy = Policy
////            .Handle<Exception>(ex => ex is not OperationCanceledException)
////            .WaitAndRetryAsync(
////                sleepDurations: Backoff.DecorrelatedJitterBackoffV2(
////                    medianFirstRetryDelay: TimeSpan.FromSeconds(20),
////                    retryCount: 3),
////                onRetry: (ex, delay, i, _) =>
////                {
////                    _logger.LogWarning(ex, "[Retry {Retry}] Hata. {Delay} sonra tekrar.", i, delay);
////                });
////    }

////    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
////    {
////        _logger.LogInformation("Worker başlatıldı.");

////        while (!stoppingToken.IsCancellationRequested)
////        {
////            try
////            {
////                // 1) Yemekhane: ZKTeco’dan sadece İLK GİRİŞLERİ DB’ye ekle (SMS burada atılmaz)
////                await _retryPolicy.ExecuteAsync(ct => ProcessYemekhaneFromZktecoAsync(ct), stoppingToken);

////                // 2) SMS Dispatcher: Bugünün tüm pending (Yemekhane + AnaKapi) kayıtlarına SMS gönder
////                await _retryPolicy.ExecuteAsync(ct => SendPendingSmsAsync(ct), stoppingToken);

////                _logger.LogInformation("Tur tamamlandı.");
////            }
////            catch (OperationCanceledException) { break; }
////            catch (Exception ex) { _logger.LogError(ex, "Tur sırasında beklenmeyen hata!"); }

////            try { await Task.Delay(POLL_INTERVAL, stoppingToken); }
////            catch (OperationCanceledException) { break; }
////        }

////        _logger.LogInformation("Worker durduruluyor.");
////    }

////    // --------------------------
////    // 1) Yemekhane (ZKTeco)
////    // --------------------------
////    private sealed record DeviceProcessResult(CihazModel Device, List<OgrenciDetayModel> NewLogs, bool DeviceOk, bool AnyTodayLog);

////    private async Task ProcessYemekhaneFromZktecoAsync(CancellationToken ct)
////    {
////        using var scope = _serviceProvider.CreateScope();
////        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

////        // Sadece aktif, ZKTeco ve Yemekhane cihazları (IP/Port zorunlu)
////        var cihazlar = await context.Cihazlar.AsNoTracking()
////            .Where(c => c.Aktif
////                     && c.DonanimTipi == DonanimTipi.ZKTeco
////                     && c.IstasyonTipi == IstasyonTipi.Yemekhane
////                     && c.IpAdresi != null
////                     && c.PortNo.HasValue && c.PortNo.Value > 0)
////            .Select(c => new CihazModel
////            {
////                CihazId = c.CihazId,
////                CihazAdi = c.CihazAdi,
////                IpAdresi = c.IpAdresi,
////                PortNo = c.PortNo,
////                IstasyonTipi = c.IstasyonTipi,
////                DonanimTipi = c.DonanimTipi,
////                Aktif = c.Aktif
////            })
////            .ToListAsync(ct);

////        if (cihazlar.Count == 0)
////        {
////            _logger.LogInformation("Yemekhane ZKTeco cihazı bulunamadı.");
////            return;
////        }

////        var results = new List<DeviceProcessResult>(cihazlar.Count);

////        // Cihazları paralel işle (IO-bound; COM çağrıları izole)
////        await Parallel.ForEachAsync(
////            source: cihazlar,
////            new ParallelOptions { CancellationToken = ct, MaxDegreeOfParallelism = MAX_DEGREE_OF_PARALLELISM },
////            async (cihaz, token) =>
////            {
////                try
////                {
////                    var res = await CihazdanTumYemekhaneLogIsleAsync(cihaz, token);
////                    lock (results) results.Add(res);
////                }
////                catch (OperationCanceledException) { throw; }
////                catch (Exception ex)
////                {
////                    _logger.LogError(ex, "Yemekhane cihazı işlenirken hata: {Ip}", cihaz.IpAdresi);
////                    lock (results) results.Add(new DeviceProcessResult(cihaz, new List<OgrenciDetayModel>(), DeviceOk: false, AnyTodayLog: false));
////                }
////            });

////        // Cihazlardan toplanan yeni loglar
////        var toInsert = results.SelectMany(r => r.NewLogs).ToList();

////        // ---- ÖNEMLİ: Öğrenci + Gün bazında tekilleştir (farklı cihazlardan gelse bile GÜNDE 1 giriş) ----
////        var dedup = toInsert
////            .GroupBy(x => new { x.OgrenciId, Day = x.OgrenciGTarih!.Value.Date })
////            .Select(g => g.OrderBy(z => z.OgrenciGTarih).First())  // en erken giriş kalsın
////            .ToList();

////        if (dedup.Count > 0)
////        {
////            var strategy = context.Database.CreateExecutionStrategy();
////            await strategy.ExecuteAsync(async () =>
////            {
////                await using var tx = await context.Database.BeginTransactionAsync(ct);
////                try
////                {
////                    await context.OgrenciDetaylar.AddRangeAsync(dedup, ct);
////                    await context.SaveChangesAsync(ct);
////                    await tx.CommitAsync(ct);
////                }
////                catch
////                {
////                    await tx.RollbackAsync(ct);
////                    throw;
////                }
////            });
////        }

////        // Tüm cihazlar OK ve bugün log okunduysa → cihaz loglarını temizle
////        bool allOk = results.All(r => r.DeviceOk);
////        if (allOk)
////        {
////            foreach (var r in results)
////            {
////                if (!r.AnyTodayLog) continue; // bugün log yoksa temizlik şart değil
////                await SafeClearDeviceLogsAsync(r.Device, ct);
////            }
////        }
////    }

////    private async Task<DeviceProcessResult> CihazdanTumYemekhaneLogIsleAsync(CihazModel cihaz, CancellationToken ct)
////    {
////        var newLogs = new List<OgrenciDetayModel>();
////        bool deviceOk = false;
////        bool anyLog = false;

////        // İstanbul/Local gün sınırları
////        var today = DateTime.Today;
////        var tomorrow = today.AddDays(1);

////        using var scope = _serviceProvider.CreateScope();
////        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

////        // 1) Geçerli öğrenciler → HashSet
////        var validStudentIds = await context.Ogrenciler
////            .AsNoTracking()
////            .Select(o => o.OgrenciId)
////            .ToListAsync(ct);
////        var validSet = new HashSet<int>(validStudentIds);

////        // 2) BUGÜN Yemekhane için zaten kaydı olan öğrenciler (CIHAZ BAĞIMSIZ!)
////        var existingToday = await context.OgrenciDetaylar
////            .AsNoTracking()
////            .Where(x => x.IstasyonTipi == IstasyonTipi.Yemekhane
////                        && x.OgrenciGTarih >= today && x.OgrenciGTarih < tomorrow)
////            .Select(x => x.OgrenciId)
////            .Distinct()
////            .ToListAsync(ct);
////        var existingSet = new HashSet<int>(existingToday);

////        CZKEM? zkem = null;
////        bool connected = false;

////        try
////        {
////            zkem = new CZKEM();
////            connected = zkem.Connect_Net(cihaz.IpAdresi!, cihaz.PortNo!.Value);
////            if (!connected)
////            {
////                int code = 0; zkem.GetLastError(ref code);
////                _logger.LogWarning("Cihaza bağlanılamadı (SC403/Yemekhane): {Ip}:{Port} Kod={Code}", cihaz.IpAdresi, cihaz.PortNo, code);
////                return new DeviceProcessResult(cihaz, newLogs, DeviceOk: false, AnyTodayLog: false);
////            }

////            if (!zkem.EnableDevice(MACHINE_NUMBER, false))
////                _logger.LogWarning("Cihaz devre dışı bırakılamadı: {Ip}", cihaz.IpAdresi);

////            if (!zkem.ReadGeneralLogData(MACHINE_NUMBER))
////            {
////                _logger.LogInformation("Cihazda log yok (SC403/Yemekhane): {Ip}:{Port}", cihaz.IpAdresi, cihaz.PortNo);
////                deviceOk = true;
////                return new DeviceProcessResult(cihaz, newLogs, deviceOk, AnyTodayLog: false);
////            }

////            // SC403 imzası
////            int dwTMachineNumber = 0;
////            int dwEnrollNumber = 0;
////            int dwEMachineNumber = 0;
////            int dwVerifyMode = 0;
////            int dwInOutMode = 0;
////            int dwYear = 0, dwMonth = 0, dwDay = 0, dwHour = 0, dwMinute = 0;

////            while (zkem.GetGeneralLogData(
////                MACHINE_NUMBER,
////                ref dwTMachineNumber,
////                ref dwEnrollNumber,
////                ref dwEMachineNumber,
////                ref dwVerifyMode,
////                ref dwInOutMode,
////                ref dwYear, ref dwMonth, ref dwDay,
////                ref dwHour, ref dwMinute))
////            {
////                ct.ThrowIfCancellationRequested();

////                // defansif tarih kontrolü
////                if (dwYear < 2000 || dwYear > DateTime.Now.Year ||
////                    dwMonth is < 1 or > 12 ||
////                    dwDay is < 1 or > 31)
////                {
////                    _logger.LogWarning("Geçersiz tarih atlandı: {Y}-{M}-{D} {H}:{Min}",
////                        dwYear, dwMonth, dwDay, dwHour, dwMinute);
////                    continue;
////                }

////                var ts = new DateTime(dwYear, dwMonth, dwDay, dwHour, dwMinute, 0);
////                anyLog = true;

////                // >>> YENİ: sadece bugünün kayıtlarını işle
////                if (ts.Date != today) continue;

////                int ogrenciId = dwEnrollNumber;

////                // hızlı kontroller (bellek içi)
////                if (!validSet.Contains(ogrenciId)) continue;

////                // bugün yemekhane için zaten kaydı varsa atla (CIHAZDAN BAĞIMSIZ)
////                if (existingSet.Contains(ogrenciId)) continue;

////                // >>> YENİ: yalnızca "GİRİŞ" yaz (çıkışı hiç yazma)
////                newLogs.Add(new OgrenciDetayModel
////                {
////                    OgrenciId = ogrenciId,
////                    IstasyonTipi = IstasyonTipi.Yemekhane,
////                    OgrenciGTarih = ts,        // sadece giriş tarihi dolu
////                    OgrenciCTarih = null,
////                    OgrenciGecisTipi = "GİRİŞ",
////                    CihazId = cihaz.CihazId,
////                    OgrenciSmsGonderildi = false
////                });

////                existingSet.Add(ogrenciId);
////            }

////            deviceOk = true;
////        }
////        catch (OperationCanceledException) { throw; }
////        catch (Exception ex)
////        {
////            _logger.LogError(ex, "SC403 (Yemekhane) cihazından log çekme hatası: {Ip}", cihaz.IpAdresi);
////            deviceOk = false;
////        }
////        finally
////        {
////            if (zkem != null)
////            {
////                try
////                {
////                    if (connected)
////                    {
////                        try
////                        {
////                            if (!zkem.EnableDevice(MACHINE_NUMBER, true))
////                                _logger.LogWarning("Cihaz tekrar etkinleştirilemedi: {Ip}", cihaz.IpAdresi);
////                            zkem.Disconnect();
////                        }
////                        catch (Exception ex)
////                        {
////                            _logger.LogError(ex, "Bağlantı sonlandırılırken hata: {Ip}", cihaz.IpAdresi);
////                        }
////                    }
////                }
////                finally
////                {
////                    try { Marshal.FinalReleaseComObject(zkem); } catch { /* ignore */ }
////                }
////            }
////        }

////        return new DeviceProcessResult(cihaz, newLogs, deviceOk, anyLog);
////    }

////    private async Task SafeClearDeviceLogsAsync(CihazModel device, CancellationToken ct)
////    {
////        CZKEM? z = null;
////        bool connected = false;
////        try
////        {
////            z = new CZKEM();
////            connected = z.Connect_Net(device.IpAdresi!, device.PortNo!.Value);
////            if (!connected)
////            {
////                int code = 0; z.GetLastError(ref code);
////                _logger.LogWarning("Temizlik için bağlanılamadı: {Ip}:{Port} Kod={Code}", device.IpAdresi, device.PortNo, code);
////                return;
////            }

////            if (!z.ClearGLog(MACHINE_NUMBER))
////                _logger.LogWarning("Yemekhane cihaz logları temizlenemedi: {Ip}", device.IpAdresi);
////            else
////                _logger.LogInformation("Yemekhane cihaz logları temizlendi: {Ip}", device.IpAdresi);

////            z.RefreshData(MACHINE_NUMBER);
////            await Task.CompletedTask;
////        }
////        catch (Exception ex)
////        {
////            _logger.LogError(ex, "Yemekhane log temizleme hatası: {Ip}", device.IpAdresi);
////        }
////        finally
////        {
////            if (z != null)
////            {
////                try
////                {
////                    if (connected) z.Disconnect();
////                }
////                catch { /* ignore */ }
////                finally
////                {
////                    try { Marshal.FinalReleaseComObject(z); } catch { /* ignore */ }
////                }
////            }
////        }
////    }

////    // -----------------------------------
////    // 2) SMS Dispatcher (Yemekhane + AnaKapi)
////    // -----------------------------------
////    private async Task SendPendingSmsAsync(CancellationToken ct)
////    {
////        using var scope = _serviceProvider.CreateScope();
////        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
////        var smsService = scope.ServiceProvider.GetRequiredService<IOgrenciSmsService>();

////        var today = DateTime.Today;
////        var tomorrow = today.AddDays(1);

////        // Bugünün ve SMS atılmamış tüm kayıtlar (Yemekhane + AnaKapi)
////        var pending = await context.OgrenciDetaylar
////            .Where(x => x.OgrenciSmsGonderildi != true &&
////                   (
////                       (x.OgrenciGTarih != null && x.OgrenciGTarih >= today && x.OgrenciGTarih < tomorrow) ||
////                       (x.OgrenciCTarih != null && x.OgrenciCTarih >= today && x.OgrenciCTarih < tomorrow)
////                   ))
////            .ToListAsync(ct);

////        if (pending.Count == 0) return;

////        // Öğrencileri tek seferde çek → dictionary
////        var ogrIds = pending.Select(p => p.OgrenciId).Distinct().ToList();
////        var ogrenciler = await context.Ogrenciler
////            .AsNoTracking()
////            .Where(o => ogrIds.Contains(o.OgrenciId))
////            .ToDictionaryAsync(o => o.OgrenciId, ct);

////        foreach (var log in pending)
////        {
////            ct.ThrowIfCancellationRequested();
////            if (!ogrenciler.TryGetValue(log.OgrenciId, out var ogrenci)) continue;
////            if (string.IsNullOrWhiteSpace(ogrenci.OgrenciVeliTelefon)) continue;

////            var ts = log.OgrenciGTarih ?? log.OgrenciCTarih ?? DateTime.Now;

////            var isGiris = log.OgrenciGTarih is not null && log.OgrenciCTarih is null;

////            string mesaj = log.IstasyonTipi == IstasyonTipi.Yemekhane
////                ? $"Abdülhamit Han Ortaokulu öğrencimiz {ogrenci.OgrenciAdSoyad} yemekhaneye giriş yaptı. Zaman: {ts:dd.MM.yyyy HH:mm:ss}"
////                : isGiris
////                    ? $"Abdülhamit Han Ortaokulu öğrencimiz {ogrenci.OgrenciAdSoyad} okula giriş yaptı. Zaman: {ts:dd.MM.yyyy HH:mm:ss}"
////                    : $"Abdülhamit Han Ortaokulu öğrencimiz {ogrenci.OgrenciAdSoyad} okuldan çıkış yaptı. Zaman: {ts:dd.MM.yyyy HH:mm:ss}";

////            try
////            {
////                var res = await smsService.SendSmsAsync(ogrenci.OgrenciVeliTelefon!, mesaj, ct);
////                if (res.Success)
////                {
////                    log.OgrenciSmsGonderildi = true;
////                    _logger.LogInformation("[SMS OK][{Ist}] OgrId:{OgrId}", log.IstasyonTipi, ogrenci.OgrenciId);
////                }
////                else
////                {
////                    _logger.LogWarning("[SMS FAIL][{Ist}] OgrId:{OgrId} Http:{Code} Err:{Err}",
////                        log.IstasyonTipi, ogrenci.OgrenciId, res.StatusCode, res.Error ?? "-");
////                }
////            }
////            catch (Exception ex)
////            {
////                _logger.LogError(ex, "[SMS EX][{Ist}] OgrId={OgrId}", log.IstasyonTipi, ogrenci.OgrenciId);
////            }
////        }

////        await context.SaveChangesAsync(ct);
////    }
////}