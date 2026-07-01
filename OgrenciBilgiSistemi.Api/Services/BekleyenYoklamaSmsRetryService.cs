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
    private const int MAX_DENEME = 10;
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
                            await ServisRetry(okul, smsService, smsAyar.MaxParalelGonderim, stoppingToken);
                            await SinifRetry(okul, smsService, smsAyar.MaxParalelGonderim, stoppingToken);
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
    private async Task ServisRetry(OkulBilgiAyari okul, ISmsService smsService, int maxParalel, CancellationToken ct)
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
              AND o.IsDeleted = 0
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

        // Bugünün toplam deneme sayılarını öğrenci başına çek
        var ogrIdler = bekleyenler.Select(b => b.OgrenciId).Distinct().ToList();
        var oncekiDenemeler = await DenemeSayilariniGetir(conn, "ServisYoklamasi", ogrIdler, ct);

        const string updateSql = @"UPDATE ServisYoklamalar SET SmsGonderildi = 1 WHERE ServisYoklamaId = @id";

        // Önce eligible kayıtları topla; giveup edilenleri SQL UPDATE ile işaretle
        var gonderilecekler = new List<(int Id, int OgrenciId, string Telefon, string Mesaj, int OncekiSayi)>();

        foreach (var b in bekleyenler)
        {
            ct.ThrowIfCancellationRequested();

            var oncekiSayi = oncekiDenemeler.GetValueOrDefault(b.OgrenciId, 0);
            if (oncekiSayi >= MAX_DENEME)
            {
                await using var giveup = new SqlCommand(updateSql, conn);
                giveup.Parameters.AddWithValue("@id", b.Id);
                await giveup.ExecuteNonQueryAsync(ct);
                _logger.LogWarning("[SMS RETRY GIVEUP][ServisYoklama] Okul:{Okul} OgrId:{OgrId} {Sayi} deneme sonrası vazgeçildi.",
                    okul.OkulKodu, b.OgrenciId, oncekiSayi);
                continue;
            }

            var mesaj = SmsMesajSablonlari.ServisYoklamasi(b.AdSoyad, b.Periyot, b.DurumId);
            gonderilecekler.Add((b.Id, b.OgrenciId, b.Telefon, mesaj, oncekiSayi));
        }

        if (gonderilecekler.Count == 0) return;

        // Paralel SMS gönder
        var sonuclar = await SmsParalelGonderici.GonderHerBiri(
            smsService,
            gonderilecekler,
            g => (g.Telefon, g.Mesaj),
            maxParalel,
            ct);

        // Sıralı işle: log + UPDATE (tek connection paylaşımı için)
        foreach (var (g, sonuc) in sonuclar)
        {
            await LogYaz(conn, g.OgrenciId, g.Telefon, g.Mesaj, "ServisYoklamasi", sonuc, g.OncekiSayi + 1, ct);

            if (sonuc.Basarili || sonuc.HataKategorisi == SmsHataKategorisi.Kalici)
            {
                await using var upd = new SqlCommand(updateSql, conn);
                upd.Parameters.AddWithValue("@id", g.Id);
                await upd.ExecuteNonQueryAsync(ct);

                if (sonuc.Basarili)
                    _logger.LogInformation("[SMS RETRY OK][ServisYoklama] Okul:{Okul} OgrId:{OgrId} Deneme:{D}",
                        okul.OkulKodu, g.OgrenciId, g.OncekiSayi + 1);
                else
                    _logger.LogWarning("[SMS RETRY KALICI][ServisYoklama] Okul:{Okul} OgrId:{OgrId} Hata:{Hata}",
                        okul.OkulKodu, g.OgrenciId, sonuc.Hata);
            }
            else
            {
                _logger.LogWarning("[SMS RETRY FAIL][ServisYoklama] Okul:{Okul} OgrId:{OgrId} Hata:{Hata} Deneme:{D}",
                    okul.OkulKodu, g.OgrenciId, sonuc.Hata, g.OncekiSayi + 1);
            }
        }
    }

    // ----------------------------------------------------------------
    // Sınıf yoklaması: bugün için her ders bit'i ayrı kontrol edilir.
    // DurumId=2 (Yok) ve ilgili bit set edilmemiş kayıtlar yeniden gönderilir.
    // ----------------------------------------------------------------
    private async Task SinifRetry(OkulBilgiAyari okul, ISmsService smsService, int maxParalel, CancellationToken ct)
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
                  AND o.IsDeleted = 0
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

            // Bugünün toplam deneme sayılarını öğrenci başına çek (sınıf yoklaması)
            var ogrIdler = bekleyenler.Select(b => b.OgrenciId).Distinct().ToList();
            var oncekiDenemeler = await DenemeSayilariniGetir(conn, "SinifYoklamasi", ogrIdler, ct);

            const string updateSql = @"
                UPDATE SinifYoklamalar
                SET SmsDurumu = SmsDurumu | @dersBit
                WHERE SinifYoklamaId = @id";

            // Önce eligible kayıtları topla; giveup edilenleri SQL UPDATE ile işaretle
            var gonderilecekler = new List<(int Id, int OgrenciId, string Telefon, string Mesaj, int OncekiSayi)>();

            foreach (var b in bekleyenler)
            {
                ct.ThrowIfCancellationRequested();

                var oncekiSayi = oncekiDenemeler.GetValueOrDefault(b.OgrenciId, 0);
                if (oncekiSayi >= MAX_DENEME)
                {
                    await using var giveup = new SqlCommand(updateSql, conn);
                    giveup.Parameters.AddWithValue("@dersBit", dersBit);
                    giveup.Parameters.AddWithValue("@id", b.Id);
                    await giveup.ExecuteNonQueryAsync(ct);
                    _logger.LogWarning("[SMS RETRY GIVEUP][SinifYoklama] Okul:{Okul} OgrId:{OgrId} Ders:{Ders} {Sayi} deneme sonrası vazgeçildi.",
                        okul.OkulKodu, b.OgrenciId, dersNo, oncekiSayi);
                    continue;
                }

                var mesaj = SmsMesajSablonlari.SinifYoklamasiDevamsiz(b.AdSoyad, dersNo);
                gonderilecekler.Add((b.Id, b.OgrenciId, b.Telefon, mesaj, oncekiSayi));
            }

            if (gonderilecekler.Count == 0) continue;

            // Paralel SMS gönder
            var sonuclar = await SmsParalelGonderici.GonderHerBiri(
                smsService,
                gonderilecekler,
                g => (g.Telefon, g.Mesaj),
                maxParalel,
                ct);

            // Sıralı işle: log + UPDATE
            foreach (var (g, sonuc) in sonuclar)
            {
                await LogYaz(conn, g.OgrenciId, g.Telefon, g.Mesaj, "SinifYoklamasi", sonuc, g.OncekiSayi + 1, ct);

                if (sonuc.Basarili || sonuc.HataKategorisi == SmsHataKategorisi.Kalici)
                {
                    await using var upd = new SqlCommand(updateSql, conn);
                    upd.Parameters.AddWithValue("@dersBit", dersBit);
                    upd.Parameters.AddWithValue("@id", g.Id);
                    await upd.ExecuteNonQueryAsync(ct);

                    if (sonuc.Basarili)
                        _logger.LogInformation("[SMS RETRY OK][SinifYoklama] Okul:{Okul} OgrId:{OgrId} Ders:{Ders} Deneme:{D}",
                            okul.OkulKodu, g.OgrenciId, dersNo, g.OncekiSayi + 1);
                    else
                        _logger.LogWarning("[SMS RETRY KALICI][SinifYoklama] Okul:{Okul} OgrId:{OgrId} Ders:{Ders} Hata:{Hata}",
                            okul.OkulKodu, g.OgrenciId, dersNo, sonuc.Hata);
                }
                else
                {
                    _logger.LogWarning("[SMS RETRY FAIL][SinifYoklama] Okul:{Okul} OgrId:{OgrId} Ders:{Ders} Hata:{Hata} Deneme:{D}",
                        okul.OkulKodu, g.OgrenciId, dersNo, sonuc.Hata, g.OncekiSayi + 1);
                }
            }
        }
    }

    /// <summary>
    /// Bugünün belirtilen tip ve öğrenciler için deneme sayılarını topluca getirir.
    /// </summary>
    private static async Task<Dictionary<int, int>> DenemeSayilariniGetir(
        SqlConnection conn, string tip, List<int> ogrIdler, CancellationToken ct)
    {
        var sonuc = new Dictionary<int, int>();
        if (ogrIdler.Count == 0) return sonuc;

        var parametreAdlari = new string[ogrIdler.Count];
        for (int i = 0; i < ogrIdler.Count; i++)
            parametreAdlari[i] = $"@id{i}";

        var sql = $@"
            SELECT OgrenciId, COUNT(*) AS Sayi
            FROM SmsGonderimGecmisleri
            WHERE Tip = @tip
              AND CAST(GonderimZamani AS DATE) = CAST(GETDATE() AS DATE)
              AND OgrenciId IN ({string.Join(", ", parametreAdlari)})
            GROUP BY OgrenciId";

        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@tip", tip);
        for (int i = 0; i < ogrIdler.Count; i++)
            cmd.Parameters.AddWithValue(parametreAdlari[i], ogrIdler[i]);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            sonuc[(int)reader["OgrenciId"]] = (int)reader["Sayi"];
        }
        return sonuc;
    }

    /// <summary>
    /// SmsGonderimGecmisi tablosuna gönderim sonucunu kaydeder. Açık connection kullanır.
    /// </summary>
    private async Task LogYaz(
        SqlConnection conn, int ogrenciId, string telefon, string mesaj, string tip,
        SmsGonderimSonucu sonuc, int denemeNumarasi, CancellationToken ct)
    {
        const string sql = @"
            INSERT INTO SmsGonderimGecmisleri
            (OgrenciId, Telefon, Mesaj, Tip, GonderimZamani, Basarili,
             HataKategorisi, Hata, HamCevap, HttpDurumKodu, DenemeNumarasi)
            VALUES
            (@ogrId, @tel, @msj, @tip, @zaman, @basarili,
             @kategori, @hata, @ham, @http, @deneme)";

        try
        {
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ogrId", ogrenciId);
            cmd.Parameters.AddWithValue("@tel", telefon);
            cmd.Parameters.AddWithValue("@msj", mesaj);
            cmd.Parameters.AddWithValue("@tip", tip);
            cmd.Parameters.AddWithValue("@zaman", DateTime.Now);
            cmd.Parameters.AddWithValue("@basarili", sonuc.Basarili);
            cmd.Parameters.AddWithValue("@kategori", (int)sonuc.HataKategorisi);
            cmd.Parameters.AddWithValue("@hata", (object?)sonuc.Hata ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ham", (object?)sonuc.HamCevap ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@http", (object?)sonuc.HttpDurumKodu ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@deneme", denemeNumarasi);
            await cmd.ExecuteNonQueryAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMS gönderim geçmişi yazılamadı. OgrId:{OgrId}, Tip:{Tip}", ogrenciId, tip);
        }
    }
}
