using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using OgrenciBilgiSistemi.Shared.Services;
using OgrenciBilgiSistemi.Sms;

namespace OgrenciBilgiSistemi.Api.Services;

public sealed class YoklamaSmsBildirimService
{
    private readonly TenantBaglami _tenantBaglami;
    private readonly ISmsService _smsService;
    private readonly SmsAyarlari _ayar;
    private readonly ILogger<YoklamaSmsBildirimService> _logger;

    public YoklamaSmsBildirimService(
        TenantBaglami tenantBaglami,
        ISmsService smsService,
        IOptions<SmsAyarlari> ayar,
        ILogger<YoklamaSmsBildirimService> logger)
    {
        _tenantBaglami = tenantBaglami;
        _smsService = smsService;
        _ayar = ayar.Value;
        _logger = logger;
    }

    private string ConnectionString => _tenantBaglami.ConnectionString;

    /// <summary>
    /// Servis yoklamasında tüm öğrencilerin (Bindi/Binmedi) velilerine SMS gönderir.
    /// Atomik claim ile tekrar gönderimi engeller.
    /// </summary>
    public async Task ServisYoklamaBildir(
        IReadOnlyList<(int OgrenciId, int DurumId)> yoklamaVerisi,
        int periyot,
        CancellationToken ct = default)
    {
        if (!_ayar.Aktif) return;
        if (yoklamaVerisi.Count == 0) return;

        var tumOgrenciIdler = yoklamaVerisi.Select(x => x.OgrenciId).ToList();
        var durumMap = yoklamaVerisi.ToDictionary(x => x.OgrenciId, x => x.DurumId);

        // Atomik olarak SmsGonderildi=0 olan kayıtları claim et
        var claimedIdler = await ServisSmsClaim(tumOgrenciIdler, periyot, ct);
        if (claimedIdler.Count == 0) return;

        var ogrenciBilgileri = await VeliTelefonlariGetir(claimedIdler, ct);

        foreach (var (ogrenciId, adSoyad, veliTelefon) in ogrenciBilgileri)
        {
            var durum = durumMap.GetValueOrDefault(ogrenciId, 0);
            var mesaj = SmsMesajSablonlari.ServisYoklamasi(adSoyad, periyot, durum);

            var sonuc = await _smsService.Gonder(veliTelefon, mesaj, ct);
            if (sonuc.Basarili)
                _logger.LogInformation("[SMS OK][ServisYoklama] OgrId:{OgrId}, Periyot:{Periyot}, Durum:{Durum}", ogrenciId, periyot, durum);
            else
                _logger.LogWarning("[SMS FAIL][ServisYoklama] OgrId:{OgrId}, Hata:{Hata}", ogrenciId, sonuc.Hata);
        }
    }

    /// <summary>
    /// Sınıf yoklamasında "Yok" (DurumId=2) olan öğrencilerin velilerine SMS gönderir.
    /// Atomik claim ile tekrar gönderimi engeller.
    /// </summary>
    public async Task SinifYoklamaBildir(
        IReadOnlyList<(int OgrenciId, int DurumId)> yoklamaVerisi,
        int dersNumarasi,
        CancellationToken ct = default)
    {
        if (!_ayar.Aktif) return;

        var devamsizlar = yoklamaVerisi.Where(x => x.DurumId == 2).Select(x => x.OgrenciId).ToList();
        if (devamsizlar.Count == 0) return;

        // Atomik olarak ilgili ders bit'i set edilmemiş kayıtları claim et
        var claimedIdler = await SinifSmsClaim(devamsizlar, dersNumarasi, ct);
        if (claimedIdler.Count == 0) return;

        var ogrenciBilgileri = await VeliTelefonlariGetir(claimedIdler, ct);

        foreach (var (ogrenciId, adSoyad, veliTelefon) in ogrenciBilgileri)
        {
            var mesaj = SmsMesajSablonlari.SinifYoklamasiDevamsiz(adSoyad, dersNumarasi);

            var sonuc = await _smsService.Gonder(veliTelefon, mesaj, ct);
            if (sonuc.Basarili)
                _logger.LogInformation("[SMS OK][SinifYoklama] OgrId:{OgrId}, Ders:{Ders}", ogrenciId, dersNumarasi);
            else
                _logger.LogWarning("[SMS FAIL][SinifYoklama] OgrId:{OgrId}, Hata:{Hata}", ogrenciId, sonuc.Hata);
        }
    }

    /// <summary>
    /// ServisYoklamalar tablosunda SmsGonderildi=0 olan kayıtları atomik olarak 1'e çevirir
    /// ve claim edilen öğrenci ID'lerini döndürür.
    /// </summary>
    private async Task<List<int>> ServisSmsClaim(List<int> ogrenciIdler, int periyot, CancellationToken ct)
    {
        var sonuc = new List<int>();

        await using var conn = new SqlConnection(ConnectionString);

        var parametreAdlari = new string[ogrenciIdler.Count];
        for (int i = 0; i < ogrenciIdler.Count; i++)
            parametreAdlari[i] = $"@id{i}";

        var query = $@"
            UPDATE ServisYoklamalar
            SET SmsGonderildi = 1
            OUTPUT inserted.OgrenciId
            WHERE CAST(OlusturulmaTarihi AS DATE) = CAST(GETDATE() AS DATE)
              AND Periyot = @periyot
              AND SmsGonderildi = 0
              AND OgrenciId IN ({string.Join(", ", parametreAdlari)})";

        await using var cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@periyot", periyot);
        for (int i = 0; i < ogrenciIdler.Count; i++)
            cmd.Parameters.AddWithValue(parametreAdlari[i], ogrenciIdler[i]);

        await conn.OpenAsync(ct);
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        while (await reader.ReadAsync(ct))
            sonuc.Add((int)reader["OgrenciId"]);

        return sonuc;
    }

    /// <summary>
    /// SinifYoklamalar tablosunda ilgili ders bit'i set edilmemiş kayıtları atomik olarak set eder
    /// ve claim edilen öğrenci ID'lerini döndürür.
    /// </summary>
    private async Task<List<int>> SinifSmsClaim(List<int> ogrenciIdler, int dersNumarasi, CancellationToken ct)
    {
        var sonuc = new List<int>();
        var dersBit = 1 << (dersNumarasi - 1);

        await using var conn = new SqlConnection(ConnectionString);

        var parametreAdlari = new string[ogrenciIdler.Count];
        for (int i = 0; i < ogrenciIdler.Count; i++)
            parametreAdlari[i] = $"@id{i}";

        var query = $@"
            UPDATE SinifYoklamalar
            SET SmsDurumu = SmsDurumu | @dersBit
            OUTPUT inserted.OgrenciId
            WHERE CAST(OlusturulmaTarihi AS DATE) = CAST(GETDATE() AS DATE)
              AND (SmsDurumu & @dersBit) = 0
              AND OgrenciId IN ({string.Join(", ", parametreAdlari)})";

        await using var cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@dersBit", dersBit);
        for (int i = 0; i < ogrenciIdler.Count; i++)
            cmd.Parameters.AddWithValue(parametreAdlari[i], ogrenciIdler[i]);

        await conn.OpenAsync(ct);
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        while (await reader.ReadAsync(ct))
            sonuc.Add((int)reader["OgrenciId"]);

        return sonuc;
    }

    /// <summary>
    /// Verilen öğrenci ID'leri için ad soyad ve veli telefon numaralarını toplu olarak getirir.
    /// </summary>
    private async Task<List<(int OgrenciId, string AdSoyad, string VeliTelefon)>> VeliTelefonlariGetir(
        List<int> ogrenciIdler, CancellationToken ct)
    {
        var sonuc = new List<(int, string, string)>();

        await using var conn = new SqlConnection(ConnectionString);

        var parametreAdlari = new string[ogrenciIdler.Count];
        for (int i = 0; i < ogrenciIdler.Count; i++)
            parametreAdlari[i] = $"@id{i}";

        var query = $@"
            SELECT O.OgrenciId, O.OgrenciAdSoyad, K.Telefon
            FROM Ogrenciler O
            INNER JOIN Kullanicilar K ON O.VeliId = K.KullaniciId
            WHERE O.OgrenciId IN ({string.Join(", ", parametreAdlari)})
              AND O.OgrenciDurum = 1
              AND K.Telefon IS NOT NULL AND K.Telefon <> ''";

        await using var cmd = new SqlCommand(query, conn);
        for (int i = 0; i < ogrenciIdler.Count; i++)
            cmd.Parameters.AddWithValue(parametreAdlari[i], ogrenciIdler[i]);

        await conn.OpenAsync(ct);
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        while (await reader.ReadAsync(ct))
        {
            sonuc.Add((
                (int)reader["OgrenciId"],
                reader["OgrenciAdSoyad"]?.ToString() ?? "",
                reader["Telefon"]?.ToString() ?? ""
            ));
        }

        return sonuc;
    }
}
