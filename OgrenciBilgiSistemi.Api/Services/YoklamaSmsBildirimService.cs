using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using OgrenciBilgiSistemi.Api.Models;

namespace OgrenciBilgiSistemi.Api.Services;

public sealed class YoklamaSmsBildirimService
{
    private readonly string _connectionString;
    private readonly SmsService _smsService;
    private readonly SmsAyarlari _ayar;
    private readonly ILogger<YoklamaSmsBildirimService> _logger;

    public YoklamaSmsBildirimService(
        IConfiguration configuration,
        SmsService smsService,
        IOptions<SmsAyarlari> ayar,
        ILogger<YoklamaSmsBildirimService> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection bağlantı dizesi eksik.");
        _smsService = smsService;
        _ayar = ayar.Value;
        _logger = logger;
    }

    /// <summary>
    /// Servis yoklamasında "Binmedi" (DurumId=2) olan öğrencilerin velilerine SMS gönderir.
    /// </summary>
    public async Task ServisYoklamaBildir(
        IReadOnlyList<(int OgrenciId, int DurumId)> yoklamaVerisi,
        int periyot,
        CancellationToken ct = default)
    {
        if (!_ayar.Aktif) return;

        var binmeyenler = yoklamaVerisi.Where(x => x.DurumId == 2).Select(x => x.OgrenciId).ToList();
        if (binmeyenler.Count == 0) return;

        var ogrenciBilgileri = await VeliTelefonlariGetir(binmeyenler, ct);

        var periyotMetni = periyot == 1 ? "sabah" : "akşam";

        foreach (var (ogrenciId, adSoyad, veliTelefon) in ogrenciBilgileri)
        {
            var mesaj = $"Sayın Veli, {adSoyad} bugün {periyotMetni} servisine binmemiştir.";

            var (basarili, hata) = await _smsService.Gonder(veliTelefon, mesaj, ct);
            if (basarili)
                _logger.LogInformation("[SMS OK][ServisYoklama] OgrId:{OgrId}, Periyot:{Periyot}", ogrenciId, periyotMetni);
            else
                _logger.LogWarning("[SMS FAIL][ServisYoklama] OgrId:{OgrId}, Hata:{Hata}", ogrenciId, hata);
        }
    }

    /// <summary>
    /// Sınıf yoklamasında "Yok" (DurumId=2) olan öğrencilerin velilerine SMS gönderir.
    /// </summary>
    public async Task SinifYoklamaBildir(
        IReadOnlyList<(int OgrenciId, int DurumId)> yoklamaVerisi,
        int dersNumarasi,
        CancellationToken ct = default)
    {
        if (!_ayar.Aktif) return;

        var devamsizlar = yoklamaVerisi.Where(x => x.DurumId == 2).Select(x => x.OgrenciId).ToList();
        if (devamsizlar.Count == 0) return;

        var ogrenciBilgileri = await VeliTelefonlariGetir(devamsizlar, ct);

        foreach (var (ogrenciId, adSoyad, veliTelefon) in ogrenciBilgileri)
        {
            var mesaj = $"Sayın Veli, {adSoyad} bugün {dersNumarasi}. ders saatinde devamsız olarak işaretlenmiştir.";

            var (basarili, hata) = await _smsService.Gonder(veliTelefon, mesaj, ct);
            if (basarili)
                _logger.LogInformation("[SMS OK][SinifYoklama] OgrId:{OgrId}, Ders:{Ders}", ogrenciId, dersNumarasi);
            else
                _logger.LogWarning("[SMS FAIL][SinifYoklama] OgrId:{OgrId}, Hata:{Hata}", ogrenciId, hata);
        }
    }

    /// <summary>
    /// Verilen öğrenci ID'leri için ad soyad ve veli telefon numaralarını toplu olarak getirir.
    /// </summary>
    private async Task<List<(int OgrenciId, string AdSoyad, string VeliTelefon)>> VeliTelefonlariGetir(
        List<int> ogrenciIdler, CancellationToken ct)
    {
        var sonuc = new List<(int, string, string)>();

        await using var conn = new SqlConnection(_connectionString);

        // Parametre listesi oluştur: @id0, @id1, @id2 ...
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
