using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Microsoft.EntityFrameworkCore;
using OgrenciBilgiSistemi.Data;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Services.Implementations;
using OgrenciBilgiSistemi.Shared.Helpers;
using OgrenciBilgiSistemi.Shared.Kurallar;
using OgrenciBilgiSistemi.Shared.Services;
using zkemkeeper;

namespace OgrenciBilgiSistemi.Services.BackgroundServices;

/// <summary>
/// CihazSenkronKuyrugu'ndaki "öğrenciyi cihazla eşitle" işlerini yapar: öğrencinin o anki durumuna göre
/// (CihazSenkronKurallari.CihazdaOlmaliMi) cihaza yazar veya cihazdan siler.
/// Satırlar cihaz başına gruplanır; her cihaza tek bağlantıyla (ZkCihazKilidi altında) gidilir.
/// Yazmadan önce aynı kart cihazda başka bir kullanıcıdaysa o kullanıcının kartı boşaltılır (kullanıcı silinmez).
/// Cihaza ulaşılamazsa satırlar artan beklemeyle tekrar denenir.
/// </summary>
public sealed class CihazSenkronIsleyici : BackgroundService
{
    private const int MakineNo = 1;
    private static readonly TimeSpan Periyot = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan TemizlikPeriyodu = TimeSpan.FromHours(1);

    // Bekleyen + zamanı gelmiş, ya da kilidi düşmüş (yarım kalmış) satırları üstlenir.
    private const string UstlenSql = @"
UPDATE TOP ({0}) K
SET Durum = 1, KilitBitis = {1}, DenemeSayisi = DenemeSayisi + 1
OUTPUT inserted.*
FROM CihazSenkronKuyrugu K WITH (ROWLOCK, READPAST)
WHERE (K.Durum = 0 AND K.SonrakiDeneme <= {2}) OR (K.Durum = 1 AND K.KilitBitis < {2})";

    private readonly TenantKapsamFabrikasi _tenantKapsam;
    private readonly CihazSenkronTetikleyici _tetikleyici;
    private readonly TimeProvider _saat;
    private readonly ILogger<CihazSenkronIsleyici> _logger;
    private readonly ConcurrentDictionary<string, DateTime> _sonTemizlik = new();

    public CihazSenkronIsleyici(TenantKapsamFabrikasi tenantKapsam, CihazSenkronTetikleyici tetikleyici,
        TimeProvider saat, ILogger<CihazSenkronIsleyici> logger)
    {
        _tenantKapsam = tenantKapsam;
        _tetikleyici = tetikleyici;
        _saat = saat;
        _logger = logger;
    }

    private DateTime Simdi => _saat.GetLocalNow().DateTime;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Cihaz senkron işleyicisi başlatıldı.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _tenantKapsam.HerOkulIcinAsync("Cihaz senkronu", OkulIcinIsleAsync, stoppingToken);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cihaz senkron turu sırasında hata.");
            }

            try { await _tetikleyici.BekleAsync(Periyot, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task OkulIcinIsleAsync(IServiceProvider sp, CancellationToken ct)
    {
        var okulKodu = sp.GetRequiredService<TenantBaglami>().OkulKodu;
        var db = sp.GetRequiredService<AppDbContext>();
        var ulasilamayan = new HashSet<int>(); // bu turda bağlanılamayan cihazlar: kalan partilerde tekrar denenmez

        for (var parti = 0; parti < 10; parti++)
        {
            var alinan = await db.CihazSenkronKuyrugu
                .FromSqlRaw(UstlenSql, CihazSenkronKurallari.TopluAlimBoyutu,
                    Simdi + CihazSenkronKurallari.KilitSuresi, Simdi)
                .AsNoTracking()
                .ToListAsync(ct);
            if (alinan.Count == 0) break;

            await PartiyiIsleAsync(db, alinan, okulKodu, ulasilamayan, ct);
            if (alinan.Count < CihazSenkronKurallari.TopluAlimBoyutu) break;
        }

        if (!_sonTemizlik.TryGetValue(okulKodu, out var son) || Simdi - son >= TemizlikPeriyodu)
        {
            _sonTemizlik[okulKodu] = Simdi;
            var sinir = Simdi - CihazSenkronKurallari.SaklamaSuresi;
            await db.CihazSenkronKuyrugu
                .Where(k => k.Durum == CihazSenkronDurumu.Tamamlandi && k.OlusturmaZamani < sinir)
                .ExecuteDeleteAsync(ct);
        }
    }

    private async Task PartiyiIsleAsync(AppDbContext db, List<CihazSenkronKuyruguModel> satirlar, string okulKodu,
        HashSet<int> ulasilamayan, CancellationToken ct)
    {
        var cihazIdler = satirlar.Select(s => s.CihazId).Distinct().ToList();
        var ogrenciIdler = satirlar.Select(s => s.OgrenciId).Distinct().ToList();
        var bugun = Simdi;

        var cihazlar = await db.Cihazlar.IgnoreQueryFilters().AsNoTracking()
            .Where(c => cihazIdler.Contains(c.CihazId))
            .ToDictionaryAsync(c => c.CihazId, ct);

        // Pasif öğrenciler de gerekir: cihazdan silinecekler
        var ogrenciler = await db.Ogrenciler.IgnoreQueryFilters().AsNoTracking()
            .Where(o => ogrenciIdler.Contains(o.OgrenciId))
            .Select(o => new OgrenciBilgisi(o.OgrenciId, o.OgrenciAdSoyad, o.OgrenciKartNo, !o.IsDeleted))
            .ToDictionaryAsync(o => o.OgrenciId, ct);

        var yemekVar = (await db.OgrenciYemekler.IgnoreQueryFilters().AsNoTracking()
                .Where(y => ogrenciIdler.Contains(y.OgrenciId) && y.Aktif && y.Yil == bugun.Year && y.Ay == bugun.Month)
                .Select(y => y.OgrenciId)
                .ToListAsync(ct))
            .ToHashSet();

        foreach (var grup in satirlar.GroupBy(s => s.CihazId))
        {
            ct.ThrowIfCancellationRequested();
            var grupSatirlari = grup.ToList();
            List<IslemSonucu> sonuclar;

            if (!cihazlar.TryGetValue(grup.Key, out var cihaz) || cihaz.IsDeleted
                || cihaz.DonanimTipi != DonanimTipi.ZKTeco
                || string.IsNullOrWhiteSpace(cihaz.IpAdresi) || cihaz.PortNo is not > 0)
            {
                // Cihaz silinmiş/pasif veya bağlantı bilgisi yok: yapılacak bir şey kalmadı
                sonuclar = grupSatirlari
                    .Select(s => new IslemSonucu(s, CihazSenkronSonucTuru.Basarili, CihazSenkronIslemi.Yok, "Cihaz pasif veya bağlantı bilgisi yok."))
                    .ToList();
            }
            else if (ulasilamayan.Contains(cihaz.CihazId))
            {
                sonuclar = grupSatirlari
                    .Select(s => new IslemSonucu(s, CihazSenkronSonucTuru.Gecici, CihazSenkronIslemi.Yok, "Cihaza bağlanılamadı."))
                    .ToList();
            }
            else
            {
                using (await ZkCihazKilidi.AlAsync(cihaz.IpAdresi, cihaz.PortNo.Value, ct))
                    sonuclar = CihazaUygula(cihaz, grupSatirlari, ogrenciler, yemekVar);

                if (sonuclar.Count > 0 && sonuclar.All(x => x.Tur == CihazSenkronSonucTuru.Gecici))
                    ulasilamayan.Add(cihaz.CihazId);

                _logger.LogInformation(
                    "Cihaz senkronu: {Cihaz} ({Ip}) yazılan={Yazilan}, silinen={Silinen}, değişmeyen={Degismeyen}, hata={Hata} (okul {OkulKodu}).",
                    cihaz.CihazAdi, cihaz.IpAdresi,
                    sonuclar.Count(s => s.Tur == CihazSenkronSonucTuru.Basarili && s.Islem == CihazSenkronIslemi.Yazildi),
                    sonuclar.Count(s => s.Tur == CihazSenkronSonucTuru.Basarili && s.Islem == CihazSenkronIslemi.Silindi),
                    sonuclar.Count(s => s.Tur == CihazSenkronSonucTuru.Basarili && s.Islem == CihazSenkronIslemi.Yok),
                    sonuclar.Count(s => s.Tur != CihazSenkronSonucTuru.Basarili),
                    okulKodu);
            }

            foreach (var sonuc in sonuclar)
                await SonucuYazAsync(db, sonuc, ct);
        }
    }

    // COM çağrıları senkron; yalnızca ZkCihazKilidi altında çağrılır.
    private List<IslemSonucu> CihazaUygula(CihazModel cihaz, List<CihazSenkronKuyruguModel> satirlar,
        Dictionary<int, OgrenciBilgisi> ogrenciler, HashSet<int> yemekVar)
    {
        var sonuclar = new List<IslemSonucu>(satirlar.Count);
        CZKEM? zk = null;
        var bagli = false;

        try
        {
            zk = new CZKEM();
            bagli = zk.Connect_Net(cihaz.IpAdresi, cihaz.PortNo!.Value);
            if (!bagli)
            {
                var kod = 0;
                zk.GetLastError(ref kod);
                var hata = $"Cihaza bağlanılamadı (kod {kod}).";
                _logger.LogWarning("Cihaz senkronu: {Cihaz} ({Ip}) cihazına bağlanılamadı, kod={Kod}.", cihaz.CihazAdi, cihaz.IpAdresi, kod);
                return satirlar.Select(s => new IslemSonucu(s, CihazSenkronSonucTuru.Gecici, CihazSenkronIslemi.Yok, hata)).ToList();
            }

            zk.EnableDevice(MakineNo, false);
            var kullanicilar = KullanicilariOku(zk);

            foreach (var satir in satirlar)
            {
                try
                {
                    sonuclar.Add(SatiriUygula(zk, cihaz, satir, ogrenciler, yemekVar, kullanicilar));
                }
                catch (COMException ex)
                {
                    sonuclar.Add(new IslemSonucu(satir, CihazSenkronSonucTuru.Gecici, CihazSenkronIslemi.Yok, $"COM hatası: {ex.Message}"));
                }
            }

            zk.RefreshData(MakineNo);
        }
        catch (Exception ex)
        {
            // Bağlantı ortada koptu: işlenmemiş satırlar tekrar denenir
            _logger.LogWarning(ex, "Cihaz senkronu sırasında hata: {Cihaz} ({Ip}).", cihaz.CihazAdi, cihaz.IpAdresi);
            var islenen = sonuclar.Select(s => s.Satir.CihazSenkronKuyruguId).ToHashSet();
            sonuclar.AddRange(satirlar
                .Where(s => !islenen.Contains(s.CihazSenkronKuyruguId))
                .Select(s => new IslemSonucu(s, CihazSenkronSonucTuru.Gecici, CihazSenkronIslemi.Yok, $"Bağlantı hatası: {ex.GetType().Name}")));
        }
        finally
        {
            if (zk != null)
            {
                try
                {
                    if (bagli)
                    {
                        zk.EnableDevice(MakineNo, true);
                        zk.Disconnect();
                    }
                }
                catch { }
                finally
                {
                    try { Marshal.FinalReleaseComObject(zk); } catch { }
                }
            }
        }

        return sonuclar;
    }

    private IslemSonucu SatiriUygula(CZKEM zk, CihazModel cihaz, CihazSenkronKuyruguModel satir,
        Dictionary<int, OgrenciBilgisi> ogrenciler, HashSet<int> yemekVar, CihazKullanicilari kullanicilar)
    {
        var id = satir.OgrenciId;
        ogrenciler.TryGetValue(id, out var ogrenci);
        var olmali = ogrenci is not null
                     && CihazSenkronKurallari.CihazdaOlmaliMi(cihaz.IstasyonTipi, ogrenci.Aktif, yemekVar.Contains(id));

        // Cihazdaki kullanıcı numaraları elle eklenmiş kayıtlarla (personel vb.) çakışabilir:
        // adı öğrenciyle uyuşmayan kullanıcıya yazılmaz ve o silinmez, panelde hata olarak gösterilir
        if (kullanicilar.Kayitlar.TryGetValue(id, out var mevcut) && !AyniKisiMi(mevcut.Ad, ogrenci?.AdSoyad ?? string.Empty))
        {
            _logger.LogWarning("Cihaz senkronu: {Cihaz} cihazındaki kullanıcı {Id} ({Ad}) öğrenciyle eşleşmiyor; dokunulmadı.",
                cihaz.CihazAdi, id, mevcut.Ad);
            return new IslemSonucu(satir, CihazSenkronSonucTuru.Cakisma, CihazSenkronIslemi.Yok,
                $"Cihazdaki kullanıcı {id} ({mevcut.Ad}) bu öğrenciyle eşleşmiyor; elle eklenmiş olabilir, dokunulmadı.");
        }

        if (!olmali)
        {
            if (mevcut is null)
                return new IslemSonucu(satir, CihazSenkronSonucTuru.Basarili, CihazSenkronIslemi.Yok, null);

            // Kullanıcı + kart + parmak izi (12 = tüm kayıtlı veriler)
            var silindi = zk.DeleteEnrollData(MakineNo, id, MakineNo, 12)
                          || zk.SSR_DeleteEnrollData(MakineNo, id.ToString(), 12);
            if (!silindi)
                return new IslemSonucu(satir, CihazSenkronSonucTuru.Kalici, CihazSenkronIslemi.Yok, $"Cihaz silmeyi reddetti (kod {SonHata(zk)}).");

            kullanicilar.Kaldir(id);
            return new IslemSonucu(satir, CihazSenkronSonucTuru.Basarili, CihazSenkronIslemi.Silindi, null);
        }

        var kart = KartNo.Normalize(ogrenci!.KartNo) ?? string.Empty;
        var ad = ogrenci.AdSoyad ?? string.Empty;

        // Aynı kart cihazda başka bir kullanıcıdaysa cihaz okutmayı o kullanıcıya yazar: kartı oradan alınır
        if (kart.Length > 0 && kullanicilar.KartSahipleri.TryGetValue(kart, out var sahip) && sahip != id
            && kullanicilar.Kayitlar.TryGetValue(sahip, out var eski))
        {
            zk.SetStrCardNumber("0");
            if (zk.SetUserInfo(MakineNo, sahip, eski.Ad, eski.Sifre, eski.Yetki, eski.Etkin))
            {
                kullanicilar.KartSahipleri.Remove(kart);
                _logger.LogWarning("Cihaz senkronu: {Cihaz} cihazında öğrenci {OgrId}'nin kartı kullanıcı {Sahip} ({Ad}) üzerindeydi; kart o kullanıcıdan kaldırıldı.",
                    cihaz.CihazAdi, id, sahip, eski.Ad);
            }
        }

        // Kart tamponu her yazımda açıkça ayarlanır; aksi halde aynı bağlantıdaki önceki öğrencinin kartı yazılır
        zk.SetStrCardNumber(kart.Length > 0 ? kart : "0");
        if (!zk.SetUserInfo(MakineNo, id, ad, string.Empty, 0, true))
            return new IslemSonucu(satir, CihazSenkronSonucTuru.Kalici, CihazSenkronIslemi.Yok, $"Cihaz yazmayı reddetti (kod {SonHata(zk)}).");

        kullanicilar.Ekle(id, new CihazKullanicisi(ad, string.Empty, 0, true), kart);
        return new IslemSonucu(satir, CihazSenkronSonucTuru.Basarili, CihazSenkronIslemi.Yazildi, null);
    }

    private static CihazKullanicilari KullanicilariOku(CZKEM zk)
    {
        var sonuc = new CihazKullanicilari();
        zk.ReadAllUserID(MakineNo);

        int id = 0, yetki = 0;
        string ad = string.Empty, sifre = string.Empty, kart = string.Empty;
        bool etkin = false;
        while (zk.GetAllUserInfo(MakineNo, ref id, ref ad, ref sifre, ref yetki, ref etkin))
        {
            kart = string.Empty;
            zk.GetStrCardNumber(out kart);
            sonuc.Ekle(id, new CihazKullanicisi(ad, sifre, yetki, etkin), KartNo.Normalize(kart) is { } k && k != "0" ? k : string.Empty);
        }
        return sonuc;
    }

    // Cihaz adı 8 karakterle kısaltabiliyor: biri diğerinin başıysa aynı kişi sayılır
    private static bool AyniKisiMi(string cihazdaki, string ogrenci)
    {
        var a = cihazdaki.Trim();
        var b = ogrenci.Trim();
        return a.Length == 0 || b.StartsWith(a, StringComparison.OrdinalIgnoreCase) || a.StartsWith(b, StringComparison.OrdinalIgnoreCase);
    }

    private static int SonHata(CZKEM zk)
    {
        var kod = 0;
        try { zk.GetLastError(ref kod); } catch { }
        return kod;
    }

    private async Task SonucuYazAsync(AppDbContext db, IslemSonucu sonuc, CancellationToken ct)
    {
        var simdi = Simdi;
        var satirId = sonuc.Satir.CihazSenkronKuyruguId;
        var hata = sonuc.Hata is { Length: > 500 } h ? h[..500] : sonuc.Hata;
        var (durum, sonraki) = CihazSenkronKurallari.SonrakiAdim(sonuc.Satir.DenemeSayisi, sonuc.Tur, simdi);

        if (durum != CihazSenkronDurumu.Bekliyor)
        {
            await db.CihazSenkronKuyrugu
                .Where(k => k.CihazSenkronKuyruguId == satirId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(k => k.Durum, durum)
                    .SetProperty(k => k.SonIslem, sonuc.Islem)
                    .SetProperty(k => k.KilitBitis, (DateTime?)null)
                    .SetProperty(k => k.TamamlanmaZamani, simdi)
                    .SetProperty(k => k.SonHata, hata), ct);
            return;
        }

        // Tekrar denenecek: bu arada aynı öğrenci için yeni bekleyen satır açıldıysa iş ona bırakılır
        var dondu = await db.Database.ExecuteSqlInterpolatedAsync($@"
UPDATE K SET Durum = 0, SonrakiDeneme = {sonraki!.Value}, KilitBitis = NULL, SonHata = {hata}
FROM CihazSenkronKuyrugu K
WHERE K.CihazSenkronKuyruguId = {satirId}
  AND NOT EXISTS (SELECT 1 FROM CihazSenkronKuyrugu X WITH (UPDLOCK, HOLDLOCK)
                  WHERE X.CihazId = K.CihazId AND X.OgrenciId = K.OgrenciId AND X.Durum = 0)", ct);

        if (dondu == 0)
            await db.CihazSenkronKuyrugu
                .Where(k => k.CihazSenkronKuyruguId == satirId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(k => k.Durum, CihazSenkronDurumu.Tamamlandi)
                    .SetProperty(k => k.KilitBitis, (DateTime?)null)
                    .SetProperty(k => k.TamamlanmaZamani, simdi)
                    .SetProperty(k => k.SonHata, "Sonraki senkron kaydıyla birleştirildi."), ct);
    }

    private sealed record OgrenciBilgisi(int OgrenciId, string? AdSoyad, string? KartNo, bool Aktif);

    private sealed record IslemSonucu(CihazSenkronKuyruguModel Satir, CihazSenkronSonucTuru Tur, CihazSenkronIslemi Islem, string? Hata);

    private sealed record CihazKullanicisi(string Ad, string Sifre, int Yetki, bool Etkin);

    private sealed class CihazKullanicilari
    {
        public Dictionary<int, CihazKullanicisi> Kayitlar { get; } = new();
        public Dictionary<string, int> KartSahipleri { get; } = new();

        public void Ekle(int id, CihazKullanicisi kullanici, string kart)
        {
            Kaldir(id);
            Kayitlar[id] = kullanici;
            if (kart.Length > 0) KartSahipleri[kart] = id;
        }

        public void Kaldir(int id)
        {
            Kayitlar.Remove(id);
            foreach (var k in KartSahipleri.Where(x => x.Value == id).Select(x => x.Key).ToList())
                KartSahipleri.Remove(k);
        }
    }
}
