using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using OgrenciBilgiSistemi.Api.Services.Interfaces;
using OgrenciBilgiSistemi.Shared.Enums;
using OgrenciBilgiSistemi.Shared.Kurallar;
using OgrenciBilgiSistemi.Shared.Services;
using OgrenciBilgiSistemi.Sms;

namespace OgrenciBilgiSistemi.Api.Services;

/// <summary>
/// Yoklama SMS'lerini GonderimKuyrugu'na alır; gönderimi MVC'deki GonderimKuyruguIsleyici yapar.
/// Kaynak satırın bayrağı (SmsGonderildi / SmsDurumu biti) "kuyruğa alındı" anlamındadır ve kuyruk
/// satırıyla aynı transaction'da set edilir: gönderim hatası artık SMS'i kaybettirmez, çift alma da olmaz.
/// </summary>
public sealed class YoklamaSmsBildirimService : IYoklamaSmsBildirimService
{
    private static readonly Dictionary<int, string> _dersKolonlari =
        Enumerable.Range(1, 8).ToDictionary(i => i, i => $"Ders{i}");

    private readonly TenantBaglami _tenantBaglami;
    private readonly SmsAyarlari _ayar;
    private readonly TimeProvider _saat;
    private readonly ILogger<YoklamaSmsBildirimService> _logger;

    public YoklamaSmsBildirimService(
        TenantBaglami tenantBaglami,
        IOptions<SmsAyarlari> ayar,
        TimeProvider saat,
        ILogger<YoklamaSmsBildirimService> logger)
    {
        _tenantBaglami = tenantBaglami;
        _ayar = ayar.Value;
        _saat = saat;
        _logger = logger;
    }

    private string ConnectionString => _tenantBaglami.ConnectionString;

    public Task<int> ServisYoklamaBildir(IReadOnlyList<(int OgrenciId, int DurumId)> yoklamaVerisi, int periyot, CancellationToken ct = default)
        => yoklamaVerisi.Count == 0
            ? Task.FromResult(0)
            : ServisKuyrugaAl(periyot, yoklamaVerisi.Select(x => x.OgrenciId).Distinct().ToList(), ct);

    public Task<int> SinifYoklamaBildir(IReadOnlyList<(int OgrenciId, int DurumId)> yoklamaVerisi, int dersNumarasi, CancellationToken ct = default)
    {
        // Yalnızca devamsız (DurumId=2) öğrencilerin velisine SMS gider
        var devamsizlar = yoklamaVerisi.Where(x => x.DurumId == 2).Select(x => x.OgrenciId).Distinct().ToList();
        return devamsizlar.Count == 0 ? Task.FromResult(0) : SinifKuyrugaAl(dersNumarasi, devamsizlar, ct);
    }

    public async Task<int> BekleyenleriKuyrugaAl(CancellationToken ct = default)
    {
        var toplam = await ServisKuyrugaAl(null, null, ct);
        foreach (var ders in _dersKolonlari.Keys)
            toplam += await SinifKuyrugaAl(ders, null, ct);
        return toplam;
    }

    // ---------------------------------------------------------------- Servis

    private async Task<int> ServisKuyrugaAl(int? periyot, IReadOnlyList<int>? ogrenciIdler, CancellationToken ct)
    {
        if (!_ayar.Aktif) return 0;

        var (bugun, yarin) = Bugun();
        var filtre = (periyot is null ? "" : " AND sy.Periyot = @periyot")
                   + (ogrenciIdler is null ? "" : $" AND sy.OgrenciId IN ({ParametreListesi(ogrenciIdler.Count)})");

        var sql = $@"
            SELECT sy.ServisYoklamaId, sy.OgrenciId, sy.Periyot, sy.DurumId, o.OgrenciAdSoyad, k.Telefon
            FROM ServisYoklamalar sy
            INNER JOIN Ogrenciler o ON sy.OgrenciId = o.OgrenciId
            LEFT JOIN Kullanicilar k ON o.VeliId = k.KullaniciId
            WHERE sy.OlusturulmaTarihi >= @bugun AND sy.OlusturulmaTarihi < @yarin
              AND sy.SmsGonderildi = 0
              AND o.IsDeleted = 0
              AND k.Telefon IS NOT NULL AND k.Telefon <> ''{filtre}";

        var adaylar = new List<(int Id, int OgrenciId, int Periyot, int DurumId, string AdSoyad, string Telefon)>();
        await using var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync(ct);
        await using (var cmd = new SqlCommand(sql, conn))
        {
            cmd.Parameters.AddWithValue("@bugun", bugun);
            cmd.Parameters.AddWithValue("@yarin", yarin);
            if (periyot is not null) cmd.Parameters.AddWithValue("@periyot", periyot.Value);
            if (ogrenciIdler is not null) ParametreleriEkle(cmd, ogrenciIdler);

            await using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
                adaylar.Add((r.GetInt32(0), r.GetInt32(1), Convert.ToInt32(r[2]), Convert.ToInt32(r[3]), r.GetString(4), r.GetString(5)));
        }

        var eklenen = 0;
        foreach (var a in adaylar)
        {
            // Bayrak, okuduğumuz durumla hâlâ aynıysa set edilir; değiştiyse (yeni durum) sonraki taramada alınır
            const string bayrakSql = @"
                UPDATE ServisYoklamalar SET SmsGonderildi = 1
                WHERE ServisYoklamaId = @kaynakId AND SmsGonderildi = 0 AND DurumId = @durumId";

            eklenen += await TekilKuyrugaAl(conn, bayrakSql, cmd => cmd.Parameters.AddWithValue("@durumId", a.DurumId),
                a.Id, "ServisYoklamasi", GonderimKuyruguKurallari.ServisYoklamaAnahtari(a.Id, a.DurumId),
                a.OgrenciId, a.Telefon, SmsMesajSablonlari.ServisYoklamasi(a.AdSoyad, a.Periyot, a.DurumId), ct);
        }
        return eklenen;
    }

    // ---------------------------------------------------------------- Sınıf

    private async Task<int> SinifKuyrugaAl(int dersNo, IReadOnlyList<int>? ogrenciIdler, CancellationToken ct)
    {
        if (!_ayar.Aktif) return 0;
        if (!_dersKolonlari.TryGetValue(dersNo, out var dersKolonu))
            throw new ArgumentOutOfRangeException(nameof(dersNo), "Ders numarası 1-8 arasında olmalıdır.");

        var dersBit = 1 << (dersNo - 1);
        var (bugun, yarin) = Bugun();
        var filtre = ogrenciIdler is null ? "" : $" AND sy.OgrenciId IN ({ParametreListesi(ogrenciIdler.Count)})";

        // dersKolonu sabit izin listesinden gelir (Ders1..Ders8)
        var sql = $@"
            SELECT sy.SinifYoklamaId, sy.OgrenciId, o.OgrenciAdSoyad, k.Telefon
            FROM SinifYoklamalar sy
            INNER JOIN Ogrenciler o ON sy.OgrenciId = o.OgrenciId
            LEFT JOIN Kullanicilar k ON o.VeliId = k.KullaniciId
            WHERE sy.OlusturulmaTarihi >= @bugun AND sy.OlusturulmaTarihi < @yarin
              AND sy.{dersKolonu} = 2
              AND (sy.SmsDurumu & @dersBit) = 0
              AND o.IsDeleted = 0
              AND k.Telefon IS NOT NULL AND k.Telefon <> ''{filtre}";

        var adaylar = new List<(int Id, int OgrenciId, string AdSoyad, string Telefon)>();
        await using var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync(ct);
        await using (var cmd = new SqlCommand(sql, conn))
        {
            cmd.Parameters.AddWithValue("@bugun", bugun);
            cmd.Parameters.AddWithValue("@yarin", yarin);
            cmd.Parameters.AddWithValue("@dersBit", dersBit);
            if (ogrenciIdler is not null) ParametreleriEkle(cmd, ogrenciIdler);

            await using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
                adaylar.Add((r.GetInt32(0), r.GetInt32(1), r.GetString(2), r.GetString(3)));
        }

        var bayrakSql = $@"
            UPDATE SinifYoklamalar SET SmsDurumu = SmsDurumu | @dersBit
            WHERE SinifYoklamaId = @kaynakId AND (SmsDurumu & @dersBit) = 0 AND {dersKolonu} = 2";

        var eklenen = 0;
        foreach (var a in adaylar)
        {
            eklenen += await TekilKuyrugaAl(conn, bayrakSql, cmd => cmd.Parameters.AddWithValue("@dersBit", dersBit),
                a.Id, "SinifYoklamasi", GonderimKuyruguKurallari.SinifYoklamaAnahtari(a.Id, dersNo),
                a.OgrenciId, a.Telefon, SmsMesajSablonlari.SinifYoklamasiDevamsiz(a.AdSoyad, dersNo), ct);
        }
        return eklenen;
    }

    // ---------------------------------------------------------------- Ortak

    /// <summary>
    /// Kaynak bayrağını set et + kuyruğa ekle, tek transaction'da. Bayrak UPDATE'i satırı kilitler:
    /// aynı kaydı eşzamanlı alan ikinci çağrı 0 satır günceller ve kuyruğa eklemez.
    /// </summary>
    private async Task<int> TekilKuyrugaAl(SqlConnection conn, string bayrakSql, Action<SqlCommand> bayrakParametreleri,
        int kaynakId, string tip, string tekilAnahtar, int ogrenciId, string telefon, string mesaj, CancellationToken ct)
    {
        var simdi = _saat.GetLocalNow().DateTime;
        var sql = bayrakSql + @";
            IF @@ROWCOUNT = 1 AND NOT EXISTS (SELECT 1 FROM GonderimKuyrugu WITH (UPDLOCK, HOLDLOCK) WHERE TekilAnahtar = @anahtar)
            BEGIN
                INSERT INTO GonderimKuyrugu
                    (Kanal, Tip, TekilAnahtar, OgrenciId, Telefon, Mesaj, Durum, DenemeSayisi, SonrakiDeneme, GecerlilikBitis, OlusturmaZamani)
                VALUES (@kanal, @tip, @anahtar, @ogrenciId, @telefon, @mesaj, @durum, 0, @simdi, @gecerlilik, @simdi);
                SELECT 1;
            END
            ELSE SELECT 0;";

        try
        {
            await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(ct);
            await using var cmd = new SqlCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@kaynakId", kaynakId);
            bayrakParametreleri(cmd);
            cmd.Parameters.AddWithValue("@kanal", (byte)GonderimKanali.Sms);
            cmd.Parameters.AddWithValue("@tip", tip);
            cmd.Parameters.AddWithValue("@anahtar", tekilAnahtar);
            cmd.Parameters.AddWithValue("@ogrenciId", ogrenciId);
            cmd.Parameters.AddWithValue("@telefon", telefon);
            cmd.Parameters.AddWithValue("@mesaj", mesaj);
            cmd.Parameters.AddWithValue("@durum", (byte)GonderimDurumu.Bekliyor);
            cmd.Parameters.AddWithValue("@simdi", simdi);
            cmd.Parameters.AddWithValue("@gecerlilik", GonderimKuyruguKurallari.SmsGecerlilikBitis(simdi));

            var eklendi = Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
            await tx.CommitAsync(ct);
            return eklendi;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // İşaretlenmediği için sonraki taramada tekrar denenir
            _logger.LogError(ex, "{Tip} SMS'i kuyruğa alınamadı. KaynakId: {Id}", tip, kaynakId);
            return 0;
        }
    }

    private (DateTime Bugun, DateTime Yarin) Bugun()
    {
        var bugun = _saat.GetLocalNow().Date;
        return (bugun, bugun.AddDays(1));
    }

    private static string ParametreListesi(int adet) => string.Join(",", Enumerable.Range(0, adet).Select(i => $"@o{i}"));

    private static void ParametreleriEkle(SqlCommand cmd, IReadOnlyList<int> idler)
    {
        for (int i = 0; i < idler.Count; i++)
            cmd.Parameters.AddWithValue($"@o{i}", idler[i]);
    }
}
