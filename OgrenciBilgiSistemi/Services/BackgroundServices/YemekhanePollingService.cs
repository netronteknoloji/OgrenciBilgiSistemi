using System.Collections.Concurrent;
using System.Data;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using OgrenciBilgiSistemi.Data;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Services.Implementations;
using OgrenciBilgiSistemi.Services.Interfaces;
using zkemkeeper;
using OgrenciBilgiSistemi.Shared.Services;

namespace OgrenciBilgiSistemi.Services.BackgroundServices;

/// <summary>
/// Yemekhane ZKTeco cihazlarından günde bir kez (öğle sonrası) toplu log çeker ve anlık olay
/// bağlantısının kaçırdığı geçişleri tamamlar: öğrenci-gün başına tek GİRİŞ yazar, bugünkülerin
/// veli SMS'ini kuyruğa alır. Okuma başarısızsa başarılı olana kadar tekrar dener.
/// Cihaz logları yalnızca ayın son günü gece, aynı kilit oturumundaki okuma + DB yazımı
/// başarılıysa ve cihazdaki log sayısı okunanla aynıysa silinir; böylece okunmamış kayıt silinmez ve
/// kaçırılan günler sonraki okumada tamamlanır. Cihaza IZKTecoService.CihazBaglantisiAcAsync ile bağlanılır
/// (olay bağlantısı aynı cihazdaysa ödünç alınır; cihaz ikinci oturumu -307 ile reddediyor).
/// </summary>
public sealed class YemekhanePollingService : BackgroundService
{
    private const int MACHINE_NUMBER = 1;
    private static readonly TimeSpan TUR_ARALIGI = TimeSpan.FromMinutes(1);
    // Öğle kalabalığından sonra: okuma sırasında cihaz kilitlenir (EnableDevice false)
    private static readonly TimeSpan OKUMA_SAATI = new(13, 30, 0);
    private static readonly TimeSpan TEKRAR_ARALIGI = TimeSpan.FromMinutes(5);
    private const int TEMIZLIK_BASLANGIC_SAATI = 22;

    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    private readonly TenantKapsamFabrikasi _tenantKapsam;
    private readonly KuyrukTetikleyici _tetikleyici;
    private readonly IZKTecoService _zk;
    private readonly ILogger<YemekhanePollingService> _logger;

    // (okul, cihaz) başına: bugün okundu mu, bugün temizlendi mi, son deneme ne zaman
    private readonly ConcurrentDictionary<(string OkulKodu, int CihazId), CihazDurumu> _durumlar = new();

    private sealed class CihazDurumu
    {
        public DateTime? BasariliGun;
        public DateTime? TemizlikGunu;
        public DateTime SonDeneme = DateTime.MinValue;
    }

    private sealed record OkumaSonucu(bool Ok, List<OgrenciDetayModel> Loglar, int ToplamLog, int OgrenciDegil, int Gecersiz);

    public YemekhanePollingService(
        TenantKapsamFabrikasi tenantKapsam,
        KuyrukTetikleyici tetikleyici,
        IZKTecoService zk,
        ILogger<YemekhanePollingService> logger)
    {
        _tenantKapsam = tenantKapsam;
        _tetikleyici = tetikleyici;
        _zk = zk;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Yemekhane polling servisi başlatıldı.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (DateTime.Now.TimeOfDay >= OKUMA_SAATI)
                    await _tenantKapsam.HerOkulIcinAsync("Yemekhane log işleme", YemekhaneLogIsle, stoppingToken);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Yemekhane polling turu sırasında beklenmeyen hata!");
            }

            try { await Task.Delay(TUR_ARALIGI, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }

        _logger.LogInformation("Yemekhane polling servisi durduruluyor.");
    }

    // -------------------------------------------------------
    // 1) Sırası gelen cihazlardan log çek, DB'ye yaz, ay sonunda temizle
    // -------------------------------------------------------
    private async Task YemekhaneLogIsle(IServiceProvider sp, CancellationToken ct)
    {
        var db = sp.GetRequiredService<AppDbContext>();
        var okulKodu = sp.GetRequiredService<TenantBaglami>().OkulKodu;

        var simdi = DateTime.Now;
        var today = simdi.Date;
        var aySonuGecesi = simdi.Day == DateTime.DaysInMonth(simdi.Year, simdi.Month)
                           && simdi.Hour >= TEMIZLIK_BASLANGIC_SAATI;

        var cihazlar = await db.Cihazlar.AsNoTracking()
            .Where(c => !c.IsDeleted
                     && c.DonanimTipi == DonanimTipi.ZKTeco
                     && c.IstasyonTipi == IstasyonTipi.Yemekhane
                     && c.IpAdresi != null
                     && c.PortNo.HasValue && c.PortNo!.Value > 0)
            .Select(c => new { c.CihazId, c.CihazAdi, c.IpAdresi, c.PortNo })
            .ToListAsync(ct);

        // Sırası gelmeyen cihazlar için DB'ye ve cihaza dokunulmaz
        var siradakiler = cihazlar
            .Select(c => new { Cihaz = c, Durum = _durumlar.GetOrAdd((okulKodu, c.CihazId), _ => new CihazDurumu()) })
            .Select(x => new
            {
                x.Cihaz,
                x.Durum,
                Temizlik = aySonuGecesi && x.Durum.TemizlikGunu != today
            })
            .Where(x => (x.Durum.BasariliGun != today || x.Temizlik)
                        && simdi - x.Durum.SonDeneme >= TEKRAR_ARALIGI)
            .ToList();

        if (siradakiler.Count == 0) return;

        // Cihazdaki kullanıcı Id = OgrenciId; silinmiş öğrenciler global filtreyle dışarıda
        var ogrenciIdler = new HashSet<int>(await db.Ogrenciler.AsNoTracking()
            .Select(o => o.OgrenciId)
            .ToListAsync(ct));

        foreach (var x in siradakiler)
        {
            ct.ThrowIfCancellationRequested();
            var ip = x.Cihaz.IpAdresi!;
            var port = x.Cihaz.PortNo!.Value;
            x.Durum.SonDeneme = simdi;

            // Okuma, yazma ve (ay sonunda) silme aynı kilit oturumunda: arada başka işlem cihaza bağlanmaz
            using (await ZkCihazKilidi.AlAsync(ip, port, ct))
            {
                OkumaSonucu okuma;
                var (baglanti, kod) = await _zk.CihazBaglantisiAcAsync(ip, port, ct);
                if (baglanti is null)
                {
                    _logger.LogWarning("Yemekhane cihazına bağlanılamadı: {Ip}:{Port} Kod={Code}", ip, port, kod);
                    continue;
                }
                // DB yazımı sürerken cihaz okutma alabilsin diye bağlantı okuma bitince bırakılır
                using (baglanti)
                    okuma = CihazdanLogCek(baglanti.Zk, x.Cihaz.CihazId, ip, today, ogrenciIdler);
                if (!okuma.Ok) continue;

                if (!await LoglariYaz(sp, db, okuma, ip, today, ct)) continue;

                x.Durum.BasariliGun = today;

                if (x.Temizlik && await CihazLogTemizle(ip, port, okuma.ToplamLog, ct))
                    x.Durum.TemizlikGunu = today;
            }
        }
    }

    /// <summary>
    /// Okunan logları öğrenci-gün bazında tekil GİRİŞ olarak yazar. Hepsi hatasız yazıldıysa true döner;
    /// false ise cihaz temizlenmez ve okuma tekrar denenir.
    /// </summary>
    private async Task<bool> LoglariYaz(
        IServiceProvider sp, AppDbContext db, OkumaSonucu okuma, string ip, DateTime today, CancellationToken ct)
    {
        // Öğrenci+Gün bazında tekilleştir, günün ilk geçişi alınır
        var dedup = okuma.Loglar
            .GroupBy(x => (x.OgrenciId, x.OgrenciGTarih!.Value.Date))
            .Select(g => g.OrderBy(z => z.OgrenciGTarih).First())
            .ToList();

        var mevcut = new HashSet<(int, DateTime)>();
        if (dedup.Count > 0)
        {
            var ilkGun = dedup.Min(x => x.OgrenciGTarih!.Value.Date);
            var yarin = today.AddDays(1);
            var kayitlar = await db.OgrenciDetaylar.AsNoTracking()
                .Where(x => x.IstasyonTipi == IstasyonTipi.Yemekhane
                            && x.OgrenciGecisTipi == "GİRİŞ"
                            && x.OgrenciGTarih >= ilkGun
                            && x.OgrenciGTarih < yarin)
                .Select(x => new { x.OgrenciId, x.OgrenciGTarih })
                .ToListAsync(ct);
            foreach (var k in kayitlar)
                mevcut.Add((k.OgrenciId, k.OgrenciGTarih!.Value.Date));
        }

        // Cihaz kapıyı açtığı için geçiş kaydedilir; o ay yemek hakkı olmayan öğrenciye aitse ayrıca sayılır
        var hakkiOlanlar = new HashSet<(int OgrenciId, int Yil, int Ay)>();
        foreach (var (yil, ay) in dedup.Select(x => (x.OgrenciGTarih!.Value.Year, x.OgrenciGTarih!.Value.Month)).Distinct())
        {
            foreach (var id in await YemekHakki.HakkiOlanlar(db, yil, ay).ToListAsync(ct))
                hakkiOlanlar.Add((id, yil, ay));
        }

        int zatenVar = 0, yeniKayit = 0, bugunYeni = 0, haksizGecis = 0;
        bool hataVar = false;

        // sp_getapplock ile tekil ekleme
        foreach (var log in dedup)
        {
            ct.ThrowIfCancellationRequested();
            var gun = log.OgrenciGTarih!.Value.Date;
            if (mevcut.Contains((log.OgrenciId, gun))) { zatenVar++; continue; }

            try
            {
                if (await TekilYemekhaneEkle(db, log, gun, ct))
                {
                    yeniKayit++;
                    if (gun == today) bugunYeni++;
                    if (!hakkiOlanlar.Contains((log.OgrenciId, gun.Year, gun.Month))) haksizGecis++;
                }
                else
                {
                    zatenVar++;
                }
            }
            catch (Exception ex)
            {
                hataVar = true;
                _logger.LogError(ex, "Yemekhane tekil ekleme hatası: OgrId={O}, Gün={G:yyyy-MM-dd}", log.OgrenciId, gun);
            }
        }

        _logger.LogInformation(
            "Cihaz özet {Ip}: ToplamLog={ToplamLog}, Gecersiz={Gecersiz}, OgrenciDegil={OgrenciDegil}, ZatenVar={ZatenVar}, YeniKayıt={YeniKayit}, BugünYeni={BugunYeni}, HaksizGecis={HaksizGecis}, Hata={Hata}",
            ip, okuma.ToplamLog, okuma.Gecersiz, okuma.OgrenciDegil, zatenVar, yeniKayit, bugunYeni, haksizGecis, hataVar);

        if (haksizGecis > 0)
            _logger.LogWarning("Yemekhane {Ip}: {Adet} yeni geçiş o ay yemek hakkı olmayan öğrencilere ait; kaydedildi. Cihazı Yemekhane sayfasından eşitleyin.",
                ip, haksizGecis);

        // Bugünkü yeni girişlerin SMS'leri kuyruğa alınır (geçmiş günler için SMS gitmez)
        if (bugunYeni > 0)
        {
            try
            {
                await sp.GetRequiredService<IGonderimKuyruguService>().GecisSmsleriniKuyrugaAlAsync(null, ct);
                _tetikleyici.Tetikle();
            }
            catch (Exception ex)
            {
                // Kayıtlar yazıldı; gönderim işleyicisi işaretsiz geçişleri kendi turunda da kuyruğa alır
                _logger.LogError(ex, "Yemekhane SMS kuyruğa alma hatası.");
            }
        }

        return !hataVar;
    }

    /// <summary>
    /// Tek bir ZKTeco cihazındaki tüm logları okur (ay sonu temizliğine kadar biriken).
    /// Bağlantıyı çağıran açar ve kapatır (Dispose cihazı tekrar etkinleştirir).
    /// </summary>
    private OkumaSonucu CihazdanLogCek(CZKEM zkem, int cihazId, string ip, DateTime today, HashSet<int> ogrenciIdler)
    {
        var loglar = new List<OgrenciDetayModel>();
        int toplamLog = 0, ogrenciDegil = 0, gecersiz = 0;

        try
        {
            zkem.EnableDevice(MACHINE_NUMBER, false);

            // Boş cihaz ile okuma hatası henüz ayırt edilmiyor: sahada kod görülene kadar hepsi hata sayılır,
            // TEKRAR_ARALIGI sonra yeniden denenir ve cihaz temizlenmez
            if (!zkem.ReadGeneralLogData(MACHINE_NUMBER))
            {
                int code = 0; zkem.GetLastError(ref code);
                _logger.LogWarning("Yemekhane cihazından log okunamadı (boş veya hata): {Ip} Kod={Code}", ip, code);
                return new OkumaSonucu(false, loglar, 0, 0, 0);
            }

            int dwTMachineNumber = 0, dwEnrollNumber = 0, dwEMachineNumber = 0;
            int dwVerifyMode = 0, dwInOutMode = 0;
            int dwYear = 0, dwMonth = 0, dwDay = 0, dwHour = 0, dwMinute = 0;

            while (zkem.GetGeneralLogData(
                MACHINE_NUMBER,
                ref dwTMachineNumber, ref dwEnrollNumber, ref dwEMachineNumber,
                ref dwVerifyMode, ref dwInOutMode,
                ref dwYear, ref dwMonth, ref dwDay, ref dwHour, ref dwMinute))
            {
                toplamLog++;

                if (dwYear < 2000 ||
                    dwMonth is < 1 or > 12 ||
                    dwDay < 1 || dwDay > DateTime.DaysInMonth(dwYear, dwMonth) ||
                    dwHour is < 0 or > 23 || dwMinute is < 0 or > 59)
                { gecersiz++; continue; }

                var ts = new DateTime(dwYear, dwMonth, dwDay, dwHour, dwMinute, 0);
                // Saat kontrolü yok: cihaz saati biraz ileri olsa bile bugünkü geçiş bugüne yazılmalı
                if (ts.Date > today) { gecersiz++; continue; }

                int ogrenciId = dwEnrollNumber;
                if (!ogrenciIdler.Contains(ogrenciId)) { ogrenciDegil++; continue; }

                loglar.Add(new OgrenciDetayModel
                {
                    OgrenciId = ogrenciId,
                    IstasyonTipi = IstasyonTipi.Yemekhane,
                    OgrenciGTarih = ts,
                    OgrenciCTarih = null,
                    OgrenciGecisTipi = "GİRİŞ",
                    CihazId = cihazId,
                    OgrenciSmsGonderildi = false
                });
            }

            return new OkumaSonucu(true, loglar, toplamLog, ogrenciDegil, gecersiz);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Yemekhane cihazından log çekme hatası: {Ip}", ip);
            return new OkumaSonucu(false, new List<OgrenciDetayModel>(), toplamLog, ogrenciDegil, gecersiz);
        }
    }

    /// <summary>
    /// sp_getapplock ile öğrenci-gün bazında tekil yemekhane giriş kaydı ekler.
    /// </summary>
    private static async Task<bool> TekilYemekhaneEkle(
        AppDbContext db, OgrenciDetayModel log, DateTime gun, CancellationToken ct)
    {
        var strategy = db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            var resource = $"YEMEKHANE:{log.OgrenciId}:{gun:yyyyMMdd}";

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
                        x.OgrenciGTarih.Value.Date == gun, ct);

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
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        });
    }

    /// <summary>
    /// Cihaz loglarını temizler (ay sonu, başarılı okuma + yazmadan sonra). Başarılıysa true.
    /// Cihaz kapalıyken log sayısı, okunup yazılan sayıyla eşit değilse (arada okutma olduysa) silmez.
    /// </summary>
    private async Task<bool> CihazLogTemizle(string ip, int port, int okunanLog, CancellationToken ct)
    {
        var (baglanti, kod) = await _zk.CihazBaglantisiAcAsync(ip, port, ct);
        if (baglanti is null)
        {
            _logger.LogWarning("Yemekhane log temizliği için cihaza bağlanılamadı: {Ip} Kod={Code}", ip, kod);
            return false;
        }

        using var oturum = baglanti;
        try
        {
            var z = oturum.Zk;
            z.EnableDevice(MACHINE_NUMBER, false);

            // 6 = cihazdaki geçiş (attendance) log sayısı
            int cihazdakiLog = 0;
            if (!z.GetDeviceStatus(MACHINE_NUMBER, 6, ref cihazdakiLog))
            {
                int code = 0; z.GetLastError(ref code);
                _logger.LogWarning("Yemekhane log sayısı okunamadı, temizlik ertelendi: {Ip} Kod={Code}", ip, code);
                return false;
            }

            if (cihazdakiLog != okunanLog)
            {
                _logger.LogWarning(
                    "Yemekhane log temizliği ertelendi: cihazda {Cihazda} log var, okunan {Okunan} ({Ip}). Sonraki turda yeniden okunacak.",
                    cihazdakiLog, okunanLog, ip);
                return false;
            }

            var ok = z.ClearGLog(MACHINE_NUMBER);
            if (ok)
                _logger.LogInformation("Yemekhane cihaz logları temizlendi: {Ip}", ip);
            else
                _logger.LogWarning("Yemekhane cihaz logları temizlenemedi: {Ip}", ip);

            z.RefreshData(MACHINE_NUMBER);
            return ok;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Yemekhane log temizleme hatası: {Ip}", ip);
            return false;
        }
    }
}
