using Microsoft.EntityFrameworkCore;
using OgrenciBilgiSistemi.Data;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Services.Interfaces;
using OgrenciBilgiSistemi.Shared.Services;
using System.Runtime.InteropServices;
using zkemkeeper;

namespace OgrenciBilgiSistemi.Services.Implementations
{
    public class ZKTecoService : IZKTecoService
    {
        private const int MACHINE_NO = 1;

        private readonly ILogger<ZKTecoService> _logger;
        private readonly TenantKapsamFabrikasi _tenantKapsam;

        private CZKEM? _zkemkeeper;
        private readonly SemaphoreSlim _connLock = new(1, 1);

        private event Func<string, Task>? _internalCardHandler;

        // Bağlanılan cihaz bilgisi (tek cihaz senaryosu için)
        public string? BagliOkulKodu { get; private set; }
        public int? CurrentCihazId { get; private set; }
        public IstasyonTipi? CurrentIstasyonTipi { get; private set; }

        public bool IsConnected { get; private set; }

        // Olay bağlantısının cihaz adresi; CihazBaglantisiAcAsync ödünç verip vermeyeceğine buna bakar
        private string? _bagliIp;
        private int? _bagliPort;

        public ZKTecoService(ILogger<ZKTecoService> logger, TenantKapsamFabrikasi tenantKapsam)
        {
            _logger = logger;
            _tenantKapsam = tenantKapsam;
        }

        public event Func<string, Task> OnCardReadAsync
        {
            add => _internalCardHandler += value;
            remove => _internalCardHandler -= value;
        }

        public async Task<bool> ConnectAsync()
        {
            if (IsConnected) return true;

            // Tek gerçek zamanlı bağlantı: okullar sırayla taranır, uygun ZKTeco cihazı olan ilk okul seçilir.
            // Kart olayları bu okulun kapsamında işlenir (BagliOkulKodu).
            CihazModel? cihaz = null;
            string? cihazOkulKodu = null;
            try
            {
                foreach (var okul in _tenantKapsam.Okullar)
                {
                    await using var scope = _tenantKapsam.Olustur(okul.OkulKodu);
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    cihaz = await db.Cihazlar.AsNoTracking()
                        .Where(c => !c.IsDeleted
                                 && c.DonanimTipi == DonanimTipi.ZKTeco
                                 && !string.IsNullOrWhiteSpace(c.IpAdresi)
                                 && c.PortNo.HasValue && c.PortNo.Value > 0)
                        .OrderBy(c => c.CihazId)
                        .FirstOrDefaultAsync();

                    if (cihaz is not null)
                    {
                        cihazOkulKodu = okul.OkulKodu;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ZKTeco ConnectAsync: cihaz aranırken hata.");
                return false;
            }

            if (cihaz is null)
            {
                _logger.LogWarning("Uygun ZKTeco cihazı bulunamadı (Aktif/ZKTeco/IP/Port).");
                return false;
            }

            // Kilit sırası her yerde ZkCihazKilidi → _connLock: cihazda başka işlem varken ikinci oturum açılmaz
            using var cihazKilidi = await ZkCihazKilidi.AlAsync(cihaz.IpAdresi!, cihaz.PortNo!.Value);
            await _connLock.WaitAsync();
            try
            {
                if (IsConnected) return true;

                // Her bağlanışta temiz bir COM nesnesi
                TryDisposeCom();
                _zkemkeeper = new CZKEM();

                if (!_zkemkeeper.Connect_Net(cihaz.IpAdresi, cihaz.PortNo!.Value))
                {
                    _logger.LogWarning("ZKTeco cihazına bağlanılamadı: {Ip}:{Port}", cihaz.IpAdresi, cihaz.PortNo);
                    return false;
                }

                // Tüm eventler
                _zkemkeeper.RegEvent(MACHINE_NO, 0xFFFF);

                // Çift kayıt engeli
                _zkemkeeper.OnAttTransactionEx -= zkem_OnAttTransactionEx;
                _zkemkeeper.OnHIDNum -= zkem_OnHIDNum;

                _zkemkeeper.OnAttTransactionEx += zkem_OnAttTransactionEx;
                _zkemkeeper.OnHIDNum += zkem_OnHIDNum;

                // Bağlanılan cihazı hatırla
                BagliOkulKodu = cihazOkulKodu;
                _bagliIp = cihaz.IpAdresi!.Trim();
                _bagliPort = cihaz.PortNo.Value;
                CurrentCihazId = cihaz.CihazId;
                CurrentIstasyonTipi = cihaz.IstasyonTipi;

                IsConnected = true;
                _logger.LogInformation("ZKTeco bağlandı: {Ip}:{Port} (CihazId:{Id}, Istasyon:{Ist})",
                    cihaz.IpAdresi, cihaz.PortNo, cihaz.CihazId, cihaz.IstasyonTipi);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ZKTeco ConnectAsync hatası.");
                return false;
            }
            finally
            {
                _connLock.Release();
            }
        }

        public async Task DisconnectAsync()
        {
            await _connLock.WaitAsync();
            try
            {
                if (!IsConnected) return;

                try
                {
                    if (_zkemkeeper is not null)
                    {
                        _zkemkeeper.OnAttTransactionEx -= zkem_OnAttTransactionEx;
                        _zkemkeeper.OnHIDNum -= zkem_OnHIDNum;
                    }
                }
                catch { /* yut */ }

                try { _zkemkeeper?.Disconnect(); } catch { /* yut */ }
                TryDisposeCom();
                _zkemkeeper = null;

                IsConnected = false;
                _internalCardHandler = null;
                BagliOkulKodu = null;
                _bagliIp = null;
                _bagliPort = null;
                CurrentCihazId = null;
                CurrentIstasyonTipi = null;

                _logger.LogInformation("ZKTeco bağlantısı kapatıldı.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ZKTeco DisconnectAsync hatası.");
            }
            finally
            {
                _connLock.Release();
            }
        }

        public async Task<(ZkBaglanti? Baglanti, int HataKodu)> CihazBaglantisiAcAsync(
            string ip, int port, CancellationToken ct = default)
        {
            var adres = ip.Trim();

            // Çağıran ZkCihazKilidi'ni tuttuğu için olay bağlantısı bu cihaza bu arada kurulamaz;
            // _connLock da ödünç süresince kopartılmasını/yeniden kurulmasını engeller
            await _connLock.WaitAsync(ct);
            var odunc = false;
            try
            {
                if (IsConnected && _zkemkeeper is not null
                    && _bagliPort == port && string.Equals(_bagliIp, adres, StringComparison.OrdinalIgnoreCase))
                {
                    var zk = _zkemkeeper;
                    odunc = true;
                    return (new ZkBaglanti(zk, olayBaglantisi: true, () =>
                    {
                        try
                        {
                            try { zk.EnableDevice(MACHINE_NO, true); } catch { /* yut */ }
                            // Olayların okuma/silme sonrası da gelmesi için kayıt yenilenir
                            try { zk.RegEvent(MACHINE_NO, 0xFFFF); } catch { /* yut */ }
                        }
                        finally
                        {
                            _connLock.Release();
                        }
                    }), 0);
                }
            }
            finally
            {
                if (!odunc) _connLock.Release();
            }

            CZKEM? gecici = null;
            try
            {
                gecici = new CZKEM();
                if (!gecici.Connect_Net(adres, port))
                {
                    var kod = 0;
                    try { gecici.GetLastError(ref kod); } catch { /* yut */ }
                    try { Marshal.FinalReleaseComObject(gecici); } catch { /* yut */ }
                    return (null, kod);
                }

                var baglanti = gecici;
                return (new ZkBaglanti(baglanti, olayBaglantisi: false, () =>
                {
                    try { baglanti.EnableDevice(MACHINE_NO, true); } catch { /* yut */ }
                    try { baglanti.Disconnect(); } catch { /* yut */ }
                    try { Marshal.FinalReleaseComObject(baglanti); } catch { /* yut */ }
                }), 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ZKTeco geçici bağlantı hatası: {Ip}:{Port}", adres, port);
                if (gecici is not null)
                {
                    try { Marshal.FinalReleaseComObject(gecici); } catch { /* yut */ }
                }
                return (null, 0);
            }
        }

        public async Task MonitorConnectionsAsync(CancellationToken cancellationToken)
        {
            int ardisikBasarisiz = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (!IsConnected)
                    {
                        _logger.LogDebug("ZKTeco bağlı değil. Yeniden bağlanma deneniyor…");
                        var ok = await ConnectAsync();
                        if (ok)
                        {
                            _logger.LogInformation("ZKTeco yeniden bağlandı.");
                            ardisikBasarisiz = 0;
                        }
                        else
                        {
                            ardisikBasarisiz++;
                        }
                    }
                    else
                    {
                        ardisikBasarisiz = 0;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "MonitorConnections döngü hatası.");
                    ardisikBasarisiz++;
                }

                // Exponential backoff: 3s → 6s → 12s → 24s → 48s → max 60s
                int beklemeSaniye = Math.Min(3 * (1 << Math.Min(ardisikBasarisiz, 5)), 60);
                try { await Task.Delay(TimeSpan.FromSeconds(beklemeSaniye), cancellationToken); }
                catch (TaskCanceledException) { /* iptal edildi */ }
            }
        }

        // --------- Event handlers ---------

        // Genel geçiş event’i
        private void zkem_OnAttTransactionEx(
            string EnrollNumber,
            int IsInValid,
            int AttState,
            int VerifyMethod,
            int Year,
            int Month,
            int Day,
            int Hour,
            int Minute,
            int Second,
            int Workcode)
        {
            var kartNo = (EnrollNumber ?? string.Empty).Trim();

            _logger.LogInformation(
                "ZK AttTransaction: Kart={Kart}, Zaman={Y}-{M}-{D} {H}:{Min}:{S}, Verify={Verify}, State={State}, CihazId={CihazId}, Istasyon={Ist}",
                kartNo, Year, Month, Day, Hour, Minute, Second, VerifyMethod, AttState, CurrentCihazId, CurrentIstasyonTipi);

            if (string.IsNullOrWhiteSpace(kartNo) || _internalCardHandler is null) return;

            foreach (var handler in _internalCardHandler.GetInvocationList())
            {
                if (handler is Func<string, Task> asyncHandler)
                {
                    _ = Task.Run(async () =>
                    {
                        try { await asyncHandler(kartNo); }
                        catch (Exception ex) { _logger.LogError(ex, "OnCardReadAsync handler hatası."); }
                    });
                }
            }
        }

        // Bazı cihazlarda kart numarası bu event ile gelir
        private void zkem_OnHIDNum(int CardNumber)
        {
            var kartNo = CardNumber.ToString();
            _logger.LogInformation("ZK HIDNum: Kart={Kart}, CihazId={CihazId}, Istasyon={Ist}", kartNo, CurrentCihazId, CurrentIstasyonTipi);

            if (string.IsNullOrWhiteSpace(kartNo) || _internalCardHandler is null) return;

            foreach (var handler in _internalCardHandler.GetInvocationList())
            {
                if (handler is Func<string, Task> asyncHandler)
                {
                    _ = Task.Run(async () =>
                    {
                        try { await asyncHandler(kartNo); }
                        catch (Exception ex) { _logger.LogError(ex, "OnCardReadAsync handler hatası (HIDNum)."); }
                    });
                }
            }
        }

        // --------- Helpers ---------

        private void TryDisposeCom()
        {
            try
            {
                if (_zkemkeeper is not null && Marshal.IsComObject(_zkemkeeper))
                {
                    Marshal.FinalReleaseComObject(_zkemkeeper);
                }
            }
            catch { /* yut */ }
        }
    }
}
