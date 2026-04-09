using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using OgrenciBilgiSistemi.Shared.Models;
using OgrenciBilgiSistemi.Sms;

namespace OgrenciBilgiSistemi.Api.Services;

/// <summary>
/// Bugün gönderilemeyen sınıf/servis yoklama SMS'lerini periyodik olarak yeniden dener.
/// Tüm tenant (okul) DB'lerini sırayla tarar. İnternet/SMS sağlayıcı geçici hatalarına
/// karşı dayanıklılık sağlar.
/// </summary>
public sealed class BekleyenYoklamaSmsRetryService : BackgroundService
{
    private static readonly TimeSpan POLL_INTERVAL = TimeSpan.FromMinutes(2);

    // SQL injection'a karşı güvenli kolon adı whitelist'i
    private static readonly Dictionary<int, string> _dersKolonlari = new()
    {
        { 1, "Ders1" }, { 2, "Ders2" }, { 3, "Ders3" }, { 4, "Ders4" },
        { 5, "Ders5" }, { 6, "Ders6" }, { 7, "Ders7" }, { 8, "Ders8" }
    };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<List<OkulBilgiAyari>> _okullar;
    private readonly ILogger<BekleyenYoklamaSmsRetryService> _logger;

    public BekleyenYoklamaSmsRetryService(
        IServiceScopeFactory scopeFactory,
        IOptions<List<OkulBilgiAyari>> okullar,
        ILogger<BekleyenYoklamaSmsRetryService> logger)
    {
        _scopeFactory = scopeFactory;
        _okullar = okullar;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Bekleyen yoklama SMS retry servisi başlatıldı (multi-tenant).");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var smsService = scope.ServiceProvider.GetRequiredService<ISmsService>();
                var smsAyar = scope.ServiceProvider.GetRequiredService<IOptions<SmsAyarlari>>().Value;

                if (smsAyar.Aktif)
                {
                    foreach (var okul in _okullar.Value)
                    {
                        if (stoppingToken.IsCancellationRequested) break;
                        if (string.IsNullOrWhiteSpace(okul.ConnectionString)) continue;

                        try
                        {
                            await ServisRetry(okul, smsService, stoppingToken);
                            await SinifRetry(okul, smsService, stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Tenant {Okul} için yoklama SMS retry hatası.", okul.OkulKodu);
                        }
                    }
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Yoklama SMS retry turu sırasında hata.");
            }

            try { await Task.Delay(POLL_INTERVAL, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    // ----------------------------------------------------------------
    // Servis yoklaması: bugün, SmsGonderildi=0 olan kayıtları yeniden gönder
    // ----------------------------------------------------------------
    private async Task ServisRetry(OkulBilgiAyari okul, ISmsService smsService, CancellationToken ct)
    {
        await using var conn = new SqlConnection(okul.ConnectionString);
        await conn.OpenAsync(ct);

        const string selectSql = @"
            SELECT sy.ServisYoklamaId, sy.OgrenciId, sy.Periyot, sy.DurumId,
                   o.OgrenciAdSoyad, k.Telefon
            FROM ServisYoklamalar sy
            INNER JOIN Ogrenciler o ON sy.OgrenciId = o.OgrenciId
            LEFT JOIN Kullanicilar k ON o.VeliId = k.KullaniciId
            WHERE CAST(sy.OlusturulmaTarihi AS DATE) = CAST(GETDATE() AS DATE)
              AND sy.SmsGonderildi = 0
              AND o.OgrenciDurum = 1
              AND k.Telefon IS NOT NULL AND k.Telefon <> ''";

        var bekleyenler = new List<(int Id, int OgrenciId, int Periyot, int DurumId, string AdSoyad, string Telefon)>();

        await using (var cmd = new SqlCommand(selectSql, conn))
        await using (var reader = await cmd.ExecuteReaderAsync(ct))
        {
            while (await reader.ReadAsync(ct))
            {
                bekleyenler.Add((
                    reader.GetInt32(reader.GetOrdinal("ServisYoklamaId")),
                    reader.GetInt32(reader.GetOrdinal("OgrenciId")),
                    reader.GetInt32(reader.GetOrdinal("Periyot")),
                    reader.GetInt32(reader.GetOrdinal("DurumId")),
                    reader["OgrenciAdSoyad"]?.ToString() ?? "",
                    reader["Telefon"]?.ToString() ?? ""
                ));
            }
        }

        if (bekleyenler.Count == 0) return;

        const string updateSql = @"UPDATE ServisYoklamalar SET SmsGonderildi = 1 WHERE ServisYoklamaId = @id";

        foreach (var b in bekleyenler)
        {
            ct.ThrowIfCancellationRequested();

            var mesaj = SmsMesajSablonlari.ServisYoklamasi(b.AdSoyad, b.Periyot, b.DurumId);
            try
            {
                var sonuc = await smsService.Gonder(b.Telefon, mesaj, ct);
                if (sonuc.Basarili)
                {
                    await using var upd = new SqlCommand(updateSql, conn);
                    upd.Parameters.AddWithValue("@id", b.Id);
                    await upd.ExecuteNonQueryAsync(ct);
                    _logger.LogInformation("[SMS RETRY OK][ServisYoklama] Okul:{Okul} OgrId:{OgrId}", okul.OkulKodu, b.OgrenciId);
                }
                else
                {
                    _logger.LogWarning("[SMS RETRY FAIL][ServisYoklama] Okul:{Okul} OgrId:{OgrId} Hata:{Hata}", okul.OkulKodu, b.OgrenciId, sonuc.Hata);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SMS RETRY EX][ServisYoklama] Okul:{Okul} OgrId:{OgrId}", okul.OkulKodu, b.OgrenciId);
            }
        }
    }

    // ----------------------------------------------------------------
    // Sınıf yoklaması: bugün için her ders bit'i ayrı kontrol edilir.
    // DurumId=2 (Yok) ve ilgili bit set edilmemiş kayıtlar yeniden gönderilir.
    // ----------------------------------------------------------------
    private async Task SinifRetry(OkulBilgiAyari okul, ISmsService smsService, CancellationToken ct)
    {
        await using var conn = new SqlConnection(okul.ConnectionString);
        await conn.OpenAsync(ct);

        for (int dersNo = 1; dersNo <= 8; dersNo++)
        {
            ct.ThrowIfCancellationRequested();

            int dersBit = 1 << (dersNo - 1);
            string dersKolonu = _dersKolonlari[dersNo];

            string selectSql = $@"
                SELECT sy.SinifYoklamaId, sy.OgrenciId, o.OgrenciAdSoyad, k.Telefon
                FROM SinifYoklamalar sy
                INNER JOIN Ogrenciler o ON sy.OgrenciId = o.OgrenciId
                LEFT JOIN Kullanicilar k ON o.VeliId = k.KullaniciId
                WHERE CAST(sy.OlusturulmaTarihi AS DATE) = CAST(GETDATE() AS DATE)
                  AND sy.{dersKolonu} = 2
                  AND (sy.SmsDurumu & @dersBit) = 0
                  AND o.OgrenciDurum = 1
                  AND k.Telefon IS NOT NULL AND k.Telefon <> ''";

            var bekleyenler = new List<(int Id, int OgrenciId, string AdSoyad, string Telefon)>();

            await using (var cmd = new SqlCommand(selectSql, conn))
            {
                cmd.Parameters.AddWithValue("@dersBit", dersBit);
                await using var reader = await cmd.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    bekleyenler.Add((
                        reader.GetInt32(reader.GetOrdinal("SinifYoklamaId")),
                        reader.GetInt32(reader.GetOrdinal("OgrenciId")),
                        reader["OgrenciAdSoyad"]?.ToString() ?? "",
                        reader["Telefon"]?.ToString() ?? ""
                    ));
                }
            }

            if (bekleyenler.Count == 0) continue;

            const string updateSql = @"
                UPDATE SinifYoklamalar
                SET SmsDurumu = SmsDurumu | @dersBit
                WHERE SinifYoklamaId = @id";

            foreach (var b in bekleyenler)
            {
                ct.ThrowIfCancellationRequested();

                var mesaj = SmsMesajSablonlari.SinifYoklamasiDevamsiz(b.AdSoyad, dersNo);
                try
                {
                    var sonuc = await smsService.Gonder(b.Telefon, mesaj, ct);
                    if (sonuc.Basarili)
                    {
                        await using var upd = new SqlCommand(updateSql, conn);
                        upd.Parameters.AddWithValue("@dersBit", dersBit);
                        upd.Parameters.AddWithValue("@id", b.Id);
                        await upd.ExecuteNonQueryAsync(ct);
                        _logger.LogInformation("[SMS RETRY OK][SinifYoklama] Okul:{Okul} OgrId:{OgrId} Ders:{Ders}", okul.OkulKodu, b.OgrenciId, dersNo);
                    }
                    else
                    {
                        _logger.LogWarning("[SMS RETRY FAIL][SinifYoklama] Okul:{Okul} OgrId:{OgrId} Ders:{Ders} Hata:{Hata}", okul.OkulKodu, b.OgrenciId, dersNo, sonuc.Hata);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[SMS RETRY EX][SinifYoklama] Okul:{Okul} OgrId:{OgrId} Ders:{Ders}", okul.OkulKodu, b.OgrenciId, dersNo);
                }
            }
        }
    }
}
