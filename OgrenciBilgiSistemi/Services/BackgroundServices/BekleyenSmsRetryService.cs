using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OgrenciBilgiSistemi.Data;
using OgrenciBilgiSistemi.Models;
using OgrenciBilgiSistemi.Shared.Enums;
using OgrenciBilgiSistemi.Sms;

namespace OgrenciBilgiSistemi.Services.BackgroundServices;

/// <summary>
/// Bugün gönderilemeyen Ana Kapı SMS'lerini periyodik olarak yeniden dener.
/// İnternet/SMS sağlayıcı geçici hatalarına karşı dayanıklılık sağlar.
/// (Yemekhane retry'i YemekhanePollingService içinde 1 dk'da bir yapılır.)
/// </summary>
public sealed class BekleyenSmsRetryService : BackgroundService
{
    private const string TIP = "AnaKapi";
    private const int MAX_DENEME = 10;

    private static readonly TimeSpan POLL_INTERVAL = TimeSpan.FromMinutes(2);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BekleyenSmsRetryService> _logger;

    public BekleyenSmsRetryService(IServiceScopeFactory scopeFactory, ILogger<BekleyenSmsRetryService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Bekleyen SMS retry servisi başlatıldı (Ana Kapı).");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await AnaKapiBekleyenleriGonder(stoppingToken);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ana Kapı SMS retry turu sırasında hata.");
            }

            try { await Task.Delay(POLL_INTERVAL, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task AnaKapiBekleyenleriGonder(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var smsService = scope.ServiceProvider.GetRequiredService<ISmsService>();
        var smsAyar = scope.ServiceProvider.GetRequiredService<IOptions<SmsAyarlari>>().Value;

        if (!smsAyar.Aktif) return;

        var today = DateTime.Now.Date;
        var tomorrow = today.AddDays(1);

        // Bugün, ana kapı, SMS gönderilmemiş kayıtlar
        var bekleyenler = await db.OgrenciDetaylar
            .Where(x => x.IstasyonTipi == IstasyonTipi.AnaKapi
                        && x.OgrenciSmsGonderildi != true
                        && ((x.OgrenciGTarih >= today && x.OgrenciGTarih < tomorrow)
                            || (x.OgrenciCTarih >= today && x.OgrenciCTarih < tomorrow)))
            .ToListAsync(ct);

        if (bekleyenler.Count == 0) return;

        var ogrIdler = bekleyenler.Select(x => x.OgrenciId).Distinct().ToList();
        var bilgiVeDenemeler = await db.Ogrenciler.AsNoTracking()
            .Where(o => ogrIdler.Contains(o.OgrenciId) && o.VeliId != null)
            .Select(o => new
            {
                o.OgrenciId,
                o.OgrenciAdSoyad,
                VeliTelefon = o.Veli!.Telefon,
                OncekiDenemeSayisi = db.SmsGonderimGecmisleri
                    .Count(g => g.Tip == TIP
                             && g.GonderimZamani >= today
                             && g.GonderimZamani < tomorrow
                             && g.OgrenciId == o.OgrenciId)
            })
            .ToDictionaryAsync(o => o.OgrenciId, ct);

        // Önce eligible kayıtları topla; giveup edilenleri burada işaretle
        var gonderilecekler = new List<(OgrenciDetayModel Log, string Telefon, string Mesaj, string GecisTipi, int OncekiSayi)>();

        foreach (var log in bekleyenler)
        {
            ct.ThrowIfCancellationRequested();

            if (!bilgiVeDenemeler.TryGetValue(log.OgrenciId, out var bilgi)) continue;
            if (string.IsNullOrWhiteSpace(bilgi.VeliTelefon)) continue;

            var oncekiSayi = bilgi.OncekiDenemeSayisi;

            // Emniyet: çok fazla denenmişse vazgeç (kategori yanlış belirlenmiş olma ihtimali)
            if (oncekiSayi >= MAX_DENEME)
            {
                log.OgrenciSmsGonderildi = true;
                _logger.LogWarning("[SMS RETRY GIVEUP][AnaKapi] OgrId:{OgrId} {Sayi} deneme sonrası vazgeçildi.",
                    log.OgrenciId, oncekiSayi);
                continue;
            }

            var zaman = log.OgrenciGTarih ?? log.OgrenciCTarih ?? DateTime.Now;
            var gecisTipi = log.OgrenciGTarih.HasValue ? "Giriş" : "Çıkış";
            var mesaj = SmsMesajSablonlari.AnaKapiGecis(bilgi.OgrenciAdSoyad, zaman, gecisTipi);
            gonderilecekler.Add((log, bilgi.VeliTelefon!, mesaj, gecisTipi, oncekiSayi));
        }

        if (gonderilecekler.Count > 0)
        {
            // Paralel SMS gönder (SemaphoreSlim ile sınırlı)
            var sonuclar = await SmsParalelGonderici.GonderHerBiri(
                smsService,
                gonderilecekler,
                g => (g.Telefon, g.Mesaj),
                smsAyar.MaxParalelGonderim,
                ct);

            // Sıralı işle: log yaz + bayrak güncelle (DbContext thread-safe değil)
            foreach (var (g, sonuc) in sonuclar)
            {
                db.SmsGonderimGecmisleri.Add(new SmsGonderimGecmisiModel
                {
                    OgrenciId = g.Log.OgrenciId,
                    Telefon = g.Telefon,
                    Mesaj = g.Mesaj,
                    Tip = TIP,
                    GonderimZamani = DateTime.Now,
                    Basarili = sonuc.Basarili,
                    HataKategorisi = (int)sonuc.HataKategorisi,
                    Hata = sonuc.Hata,
                    HamCevap = sonuc.HamCevap,
                    HttpDurumKodu = sonuc.HttpDurumKodu,
                    DenemeNumarasi = g.OncekiSayi + 1
                });

                if (sonuc.Basarili)
                {
                    g.Log.OgrenciSmsGonderildi = true;
                    _logger.LogInformation("[SMS RETRY OK][AnaKapi] OgrId:{OgrId} Tip:{Tip} Deneme:{D}",
                        g.Log.OgrenciId, g.GecisTipi, g.OncekiSayi + 1);
                }
                else if (sonuc.HataKategorisi == SmsHataKategorisi.Kalici)
                {
                    g.Log.OgrenciSmsGonderildi = true;
                    _logger.LogWarning("[SMS RETRY KALICI][AnaKapi] OgrId:{OgrId} Hata:{Hata}",
                        g.Log.OgrenciId, sonuc.Hata);
                }
                else
                {
                    _logger.LogWarning("[SMS RETRY FAIL][AnaKapi] OgrId:{OgrId} Hata:{Hata} Deneme:{D}",
                        g.Log.OgrenciId, sonuc.Hata, g.OncekiSayi + 1);
                }
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
