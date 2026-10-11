using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OgrenciBilgiSistemi.Data;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Push;
using OgrenciBilgiSistemi.Services.Implementations;
using OgrenciBilgiSistemi.Services.Interfaces;
using OgrenciBilgiSistemi.Shared.Kurallar;
using OgrenciBilgiSistemi.Shared.Services;
using OgrenciBilgiSistemi.Sms;

namespace OgrenciBilgiSistemi.Services.BackgroundServices;

/// <summary>
/// GonderimKuyrugu'ndaki SMS ve push'ları gönderir (MVC ve API'nin eklediği satırlar dahil).
/// Satırlar atomik olarak üstlenilir (Gonderiliyor + kilit süresi): birden çok örnek/tur aynı satırı göndermez.
/// Gönderim ortasında süreç ölürse kilit süresi dolunca satır tekrar alınır.
/// Ayrıca geçişlerden kuyruğa alınmamış SMS'leri periyodik tarar (güvenlik ağı).
/// </summary>
public sealed class GonderimKuyruguIsleyici : BackgroundService
{
    private static readonly TimeSpan Periyot = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan TaramaPeriyodu = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan TemizlikPeriyodu = TimeSpan.FromHours(1);

    // Satırları üstlenir: bekleyen + zamanı gelmiş, ya da kilidi düşmüş (yarım kalmış) satırlar.
    // READPAST: başka bir işleyicinin kilitlediği satırlar atlanır.
    private const string UstlenSql = @"
UPDATE TOP ({0}) K
SET Durum = 1, KilitBitis = {1}, DenemeSayisi = DenemeSayisi + 1
OUTPUT inserted.*
FROM GonderimKuyrugu K WITH (ROWLOCK, READPAST)
WHERE K.GecerlilikBitis > {2}
  AND ({3} = 1 OR K.Kanal <> 1)
  AND ((K.Durum = 0 AND K.SonrakiDeneme <= {2}) OR (K.Durum = 1 AND K.KilitBitis < {2}))";

    private readonly TenantKapsamFabrikasi _tenantKapsam;
    private readonly KuyrukTetikleyici _tetikleyici;
    private readonly TimeProvider _saat;
    private readonly ILogger<GonderimKuyruguIsleyici> _logger;

    private readonly ConcurrentDictionary<string, DateTime> _sonTarama = new();
    private readonly ConcurrentDictionary<string, DateTime> _sonTemizlik = new();

    public GonderimKuyruguIsleyici(TenantKapsamFabrikasi tenantKapsam, KuyrukTetikleyici tetikleyici,
        TimeProvider saat, ILogger<GonderimKuyruguIsleyici> logger)
    {
        _tenantKapsam = tenantKapsam;
        _tetikleyici = tetikleyici;
        _saat = saat;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Gönderim kuyruğu işleyicisi başlatıldı.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _tenantKapsam.HerOkulIcinAsync("Gönderim kuyruğu", OkulIcinIsleAsync, stoppingToken);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gönderim kuyruğu turu sırasında hata.");
            }

            try { await _tetikleyici.BekleAsync(Periyot, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task OkulIcinIsleAsync(IServiceProvider sp, CancellationToken ct)
    {
        var okulKodu = sp.GetRequiredService<TenantBaglami>().OkulKodu;
        var db = sp.GetRequiredService<AppDbContext>();
        var simdi = _saat.GetLocalNow().DateTime;

        // 1) Güvenlik ağı: doğrudan kuyruğa alınamamış geçişler (ör. yemekhane cihaz logları, kesinti)
        if (ZamaniGeldi(_sonTarama, okulKodu, simdi, TaramaPeriyodu))
            await sp.GetRequiredService<IGonderimKuyruguService>().GecisSmsleriniKuyrugaAlAsync(null, ct);

        // 2) Geçerliliği bitmiş, gönderilmemiş satırlar (ör. dünün SMS'leri) kapatılır
        await db.GonderimKuyrugu
            .Where(k => (k.Durum == GonderimDurumu.Bekliyor || k.Durum == GonderimDurumu.Gonderiliyor)
                        && k.GecerlilikBitis <= simdi)
            .ExecuteUpdateAsync(s => s.SetProperty(k => k.Durum, GonderimDurumu.SuresiDoldu)
                                      .SetProperty(k => k.KilitBitis, (DateTime?)null), ct);

        // 3) Gönder: parti parti, kuyruk boşalana kadar (tur başına sınırlı)
        var smsAyar = sp.GetRequiredService<IOptions<SmsAyarlari>>().Value;
        for (var parti = 0; parti < 20; parti++)
        {
            var alinan = await UstlenAsync(db, smsAyar.Aktif, ct);
            if (alinan.Count == 0) break;

            await GonderVeKaydetAsync(sp, db, alinan, smsAyar.MaxParalelGonderim, okulKodu, ct);
            if (alinan.Count < GonderimKuyruguKurallari.TopluAlimBoyutu) break;
        }

        // 4) Saklama süresini aşan tamamlanmış satırlar silinir (telefon/mesaj içerir, KVKK)
        if (ZamaniGeldi(_sonTemizlik, okulKodu, simdi, TemizlikPeriyodu))
        {
            var sinir = simdi - GonderimKuyruguKurallari.SaklamaSuresi;
            var silinen = await db.GonderimKuyrugu
                .Where(k => k.OlusturmaZamani < sinir
                            && k.Durum != GonderimDurumu.Bekliyor && k.Durum != GonderimDurumu.Gonderiliyor)
                .ExecuteDeleteAsync(ct);
            if (silinen > 0)
                _logger.LogInformation("Gönderim kuyruğu temizliği: {Adet} eski satır silindi (okul {OkulKodu}).", silinen, okulKodu);
        }
    }

    private static bool ZamaniGeldi(ConcurrentDictionary<string, DateTime> son, string okulKodu, DateTime simdi, TimeSpan aralik)
    {
        if (son.TryGetValue(okulKodu, out var t) && simdi - t < aralik)
            return false;
        son[okulKodu] = simdi;
        return true;
    }

    private async Task<List<GonderimKuyruguModel>> UstlenAsync(AppDbContext db, bool smsAktif, CancellationToken ct)
    {
        var simdi = _saat.GetLocalNow().DateTime;
        return await db.GonderimKuyrugu
            .FromSqlRaw(UstlenSql,
                GonderimKuyruguKurallari.TopluAlimBoyutu,
                simdi + GonderimKuyruguKurallari.KilitSuresi,
                simdi,
                smsAktif ? 1 : 0)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    private async Task GonderVeKaydetAsync(IServiceProvider sp, AppDbContext db, List<GonderimKuyruguModel> satirlar,
        int maxParalel, string okulKodu, CancellationToken ct)
    {
        var sonuclar = new List<(GonderimKuyruguModel Satir, GonderimSonucTuru Sonuc, string? Hata)>();

        // SMS: paralel (sağlayıcı çağrıları); DB yazımı aşağıda tek thread'de
        var smsler = satirlar.Where(s => s.Kanal == GonderimKanali.Sms).ToList();
        if (smsler.Count > 0)
        {
            var smsService = sp.GetRequiredService<ISmsService>();
            var smsSonuclari = await SmsParalelGonderici.GonderHerBiri(
                smsService, smsler, s => (s.Telefon ?? string.Empty, s.Mesaj ?? string.Empty), maxParalel, ct);

            foreach (var (satir, sonuc) in smsSonuclari)
            {
                var tur = sonuc.Basarili ? GonderimSonucTuru.Basarili
                    : sonuc.HataKategorisi == SmsHataKategorisi.Kalici ? GonderimSonucTuru.Kalici
                    : GonderimSonucTuru.Gecici;
                sonuclar.Add((satir, tur, sonuc.Hata));

                db.SmsGonderimGecmisleri.Add(new SmsGonderimGecmisiModel
                {
                    OgrenciId = satir.OgrenciId,
                    Telefon = satir.Telefon ?? string.Empty,
                    Mesaj = satir.Mesaj ?? string.Empty,
                    Tip = satir.Tip,
                    GonderimZamani = _saat.GetLocalNow().DateTime,
                    Basarili = sonuc.Basarili,
                    HataKategorisi = (int)sonuc.HataKategorisi,
                    Hata = Kisalt(sonuc.Hata),
                    HamCevap = sonuc.HamCevap,
                    HttpDurumKodu = sonuc.HttpDurumKodu,
                    DenemeNumarasi = satir.DenemeSayisi
                });
            }
        }

        // Push: yük, bildirim satırından kurulur
        var pushlar = satirlar.Where(s => s.Kanal == GonderimKanali.Push).ToList();
        if (pushlar.Count > 0)
        {
            var pushGonderici = sp.GetRequiredService<IPushBildirimGonderici>();
            var bildirimIdler = pushlar.Where(p => p.BildirimId != null).Select(p => p.BildirimId!.Value).ToList();
            var bildirimler = await db.Bildirimler.IgnoreQueryFilters().AsNoTracking()
                .Where(b => bildirimIdler.Contains(b.BildirimId))
                .ToDictionaryAsync(b => b.BildirimId, ct);

            foreach (var satir in pushlar)
            {
                if (satir.BildirimId is not { } bid || !bildirimler.TryGetValue(bid, out var b) || b.IsDeleted)
                {
                    sonuclar.Add((satir, GonderimSonucTuru.Kalici, "Bildirim bulunamadı veya silinmiş."));
                    continue;
                }

                try
                {
                    var yuk = new PushBildirimYuku(
                        BildirimTuruBaslikHelper.BasligaCevir((int)b.Tur),
                        b.Mesaj,
                        new Dictionary<string, string>
                        {
                            ["bildirimId"] = b.BildirimId.ToString(),
                            ["tur"] = ((int)b.Tur).ToString(),
                            ["randevuId"] = b.RandevuId?.ToString() ?? string.Empty,
                            ["okulKodu"] = okulKodu
                        });

                    var sonuc = await pushGonderici.GonderAsync(b.AliciKullaniciId, yuk, ct);

                    // Token yoksa (0/0) gönderilecek cihaz yoktur: tamamlandı sayılır.
                    // Hiçbir cihaza gidemediyse (ör. FCM erişilemiyor) geçici hata.
                    sonuclar.Add(sonuc.BasarisizSayisi > 0 && sonuc.BasariliSayisi == 0
                        ? (satir, GonderimSonucTuru.Gecici, "Push hiçbir cihaza iletilemedi.")
                        : (satir, GonderimSonucTuru.Basarili, null));
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    sonuclar.Add((satir, GonderimSonucTuru.Gecici, ex.GetType().Name));
                    _logger.LogWarning(ex, "Push gönderilemedi. BildirimId: {Id}", bid);
                }
            }
        }

        await db.SaveChangesAsync(ct); // SMS geçmişi

        foreach (var (satir, sonuc, hata) in sonuclar)
            await SonucuYazAsync(db, satir, sonuc, hata, ct);

        var basarisiz = sonuclar.Count(s => s.Sonuc != GonderimSonucTuru.Basarili);
        _logger.LogInformation("Gönderim kuyruğu: {Toplam} gönderim, {Basarisiz} başarısız (okul {OkulKodu}).",
            sonuclar.Count, basarisiz, okulKodu);
    }

    private async Task SonucuYazAsync(AppDbContext db, GonderimKuyruguModel satir, GonderimSonucTuru sonuc, string? hata,
        CancellationToken ct)
    {
        var simdi = _saat.GetLocalNow().DateTime;
        var (durum, sonraki) = GonderimKuyruguKurallari.SonrakiAdim(satir.DenemeSayisi, sonuc, simdi, satir.GecerlilikBitis);
        var gonderildi = durum == GonderimDurumu.Gonderildi;

        await db.GonderimKuyrugu
            .Where(k => k.GonderimKuyruguId == satir.GonderimKuyruguId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(k => k.Durum, durum)
                .SetProperty(k => k.SonrakiDeneme, k => sonraki ?? k.SonrakiDeneme)
                .SetProperty(k => k.KilitBitis, (DateTime?)null)
                .SetProperty(k => k.GonderimZamani, k => gonderildi ? simdi : k.GonderimZamani)
                .SetProperty(k => k.SonHata, Kisalt(hata)), ct);
    }

    private static string? Kisalt(string? s) => s is { Length: > 500 } ? s[..500] : s;
}
