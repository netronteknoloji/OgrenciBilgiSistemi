using Microsoft.Data.SqlClient;
using OgrenciBilgiSistemi.Api.Dtos;
using OgrenciBilgiSistemi.Api.Models;

namespace OgrenciBilgiSistemi.Api.Services
{
    public class OgrenciService
    {
        private readonly string _connectionString;

        // SQL injection'a karşı whitelist: sadece bu sabit değerler SQL'e girer
        private static readonly Dictionary<int, string> _dersKolonlari = new()
        {
            [1] = "Ders1", [2] = "Ders2", [3] = "Ders3", [4] = "Ders4",
            [5] = "Ders5", [6] = "Ders6", [7] = "Ders7", [8] = "Ders8"
        };

        public OgrenciService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection bağlantı dizesi eksik.");
        }

        public async Task<List<OgrenciModel>> SinifaGoreOgrencileriGetirAsync(int sinifId)
        {
            var ogrenciler = new List<OgrenciModel>();
            try
            {
                await using var conn = new SqlConnection(_connectionString);
                const string query = @"
                    SELECT OgrenciId, OgrenciAdSoyad, OgrenciGorsel
                    FROM Ogrenciler
                    WHERE BirimId = @sinifId AND OgrenciDurum = 1";

                await using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@sinifId", sinifId);
                await conn.OpenAsync();

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    string rawFileName = reader["OgrenciGorsel"]?.ToString() ?? string.Empty;
                    ogrenciler.Add(new OgrenciModel
                    {
                        OgrenciId     = (int)reader["OgrenciId"],
                        OgrenciAdSoyad = reader["OgrenciAdSoyad"]?.ToString() ?? string.Empty,
                        OgrenciGorsel = string.IsNullOrEmpty(rawFileName) ? "user_icon.png" : rawFileName
                    });
                }
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException("Öğrenci listesi alınamadı.", ex);
            }
            return ogrenciler;
        }

        public async Task<List<OgrenciModel>> VeliyeGoreOgrencileriGetirAsync(int veliId)
        {
            var ogrenciler = new List<OgrenciModel>();
            try
            {
                await using var conn = new SqlConnection(_connectionString);
                const string query = @"
                    SELECT O.OgrenciId, O.OgrenciAdSoyad, O.OgrenciGorsel, O.OgrenciNo,
                           O.BirimId, O.ServisId, B.BirimAd AS SinifAdi
                    FROM Ogrenciler O
                    LEFT JOIN Birimler B ON O.BirimId = B.BirimId
                    WHERE O.VeliId = @veliId AND O.OgrenciDurum = 1";

                await using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@veliId", veliId);
                await conn.OpenAsync();

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    string rawFileName = reader["OgrenciGorsel"]?.ToString() ?? string.Empty;
                    ogrenciler.Add(new OgrenciModel
                    {
                        OgrenciId      = (int)reader["OgrenciId"],
                        OgrenciAdSoyad = reader["OgrenciAdSoyad"]?.ToString() ?? string.Empty,
                        OgrenciNo      = (int)reader["OgrenciNo"],
                        OgrenciGorsel  = string.IsNullOrEmpty(rawFileName) ? "user_icon.png" : rawFileName,
                        BirimId        = reader["BirimId"] as int?,
                        ServisId       = reader["ServisId"] as int?,
                        SinifAdi       = reader["SinifAdi"]?.ToString()
                    });
                }
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException("Veliye ait öğrenci listesi alınamadı.", ex);
            }
            return ogrenciler;
        }

        public async Task<OgrenciModel?> OgrenciGetirAsync(int ogrenciId)
        {
            try
            {
                await using var conn = new SqlConnection(_connectionString);
                const string query = @"
                    SELECT OgrenciId, OgrenciAdSoyad, OgrenciGorsel, BirimId, VeliId, ServisId
                    FROM Ogrenciler
                    WHERE OgrenciId = @ogrenciId AND OgrenciDurum = 1";

                await using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ogrenciId", ogrenciId);
                await conn.OpenAsync();

                await using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new OgrenciModel
                    {
                        OgrenciId      = (int)reader["OgrenciId"],
                        OgrenciAdSoyad = reader["OgrenciAdSoyad"]?.ToString() ?? string.Empty,
                        OgrenciGorsel  = reader["OgrenciGorsel"]?.ToString(),
                        BirimId        = reader["BirimId"]       as int?,
                        VeliId         = reader["VeliId"] as int?,
                        ServisId       = reader["ServisId"] as int?
                    };
                }
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException("Öğrenci bilgisi alınamadı.", ex);
            }
            return null;
        }

        public async Task<Dictionary<int, int>> MevcutYoklamaGetirAsync(int sinifId, int dersNumarasi)
        {
            if (!_dersKolonlari.TryGetValue(dersNumarasi, out var dersKolonu))
                throw new ArgumentOutOfRangeException(nameof(dersNumarasi), "Ders numarası 1-8 arasında olmalıdır.");

            var yoklamaDict = new Dictionary<int, int>();

            try
            {
                await using var conn = new SqlConnection(_connectionString);
                string query = $@"
                    SELECT SY.OgrenciId, SY.{dersKolonu}
                    FROM SinifYoklamalar SY
                    INNER JOIN Ogrenciler O ON SY.OgrenciId = O.OgrenciId
                    WHERE O.BirimId = @sinifId
                      AND CAST(SY.OlusturulmaTarihi AS DATE) = CAST(GETDATE() AS DATE)";

                await using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@sinifId", sinifId);
                await conn.OpenAsync();

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    if (reader[dersKolonu] != DBNull.Value)
                        yoklamaDict[(int)reader["OgrenciId"]] = (int)reader[dersKolonu];
                }
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException("Yoklama bilgisi alınamadı.", ex);
            }
            return yoklamaDict;
        }

        public async Task TopluYoklamaKaydetAsync(
            IEnumerable<(int OgrenciId, int DurumId)> yoklamaVerisi,
            int sinifId,
            int ogretmenId,
            int dersNumarasi)
        {
            if (!_dersKolonlari.TryGetValue(dersNumarasi, out var dersKolonu))
                throw new ArgumentOutOfRangeException(nameof(dersNumarasi), "Ders numarası 1-8 arasında olmalıdır.");

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var transaction = (SqlTransaction)await conn.BeginTransactionAsync();

            try
            {
                string query = $@"
                    DECLARE @Bugun DATE = CAST(GETDATE() AS DATE);
                    MERGE INTO SinifYoklamalar AS target
                    USING (SELECT @ogrenciId AS OgrenciId) AS source
                    ON (target.OgrenciId = source.OgrenciId AND CAST(target.OlusturulmaTarihi AS DATE) = @Bugun)
                    WHEN MATCHED THEN
                        UPDATE SET {dersKolonu} = @durumId,
                                   GuncellenmeTarihi = GETDATE(),
                                   SmsDurumu = CASE WHEN ISNULL(target.{dersKolonu}, 0) <> @durumId
                                               THEN target.SmsDurumu & ~@dersBit
                                               ELSE target.SmsDurumu END
                    WHEN NOT MATCHED THEN
                        INSERT (OgrenciId, KullaniciId, {dersKolonu}, SmsDurumu, OlusturulmaTarihi)
                        VALUES (@ogrenciId, @kullaniciId, @durumId, 0, GETDATE());";

                var dersBit = 1 << (dersNumarasi - 1);

                foreach (var kayit in yoklamaVerisi)
                {
                    await using var cmd = new SqlCommand(query, conn, transaction);
                    cmd.Parameters.AddWithValue("@ogrenciId", kayit.OgrenciId);
                    cmd.Parameters.AddWithValue("@durumId",   kayit.DurumId);
                    cmd.Parameters.AddWithValue("@kullaniciId", ogretmenId);
                    cmd.Parameters.AddWithValue("@dersBit", dersBit);
                    await cmd.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new InvalidOperationException("Yoklama kaydedilemedi.", ex);
            }
        }

        public async Task<List<SinifYoklamaModel>> HaftalikYoklamaGetirAsync(int ogrenciId, DateTime baslangic, DateTime bitis)
        {
            var sonuc = new List<SinifYoklamaModel>();
            try
            {
                await using var conn = new SqlConnection(_connectionString);
                const string query = @"
                    SELECT SinifYoklamaId, OgrenciId, KullaniciId,
                           Ders1, Ders2, Ders3, Ders4, Ders5, Ders6, Ders7, Ders8,
                           OlusturulmaTarihi, GuncellenmeTarihi
                    FROM SinifYoklamalar
                    WHERE OgrenciId = @ogrenciId
                      AND CAST(OlusturulmaTarihi AS DATE) BETWEEN @baslangic AND @bitis
                    ORDER BY OlusturulmaTarihi";

                await using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ogrenciId", ogrenciId);
                cmd.Parameters.AddWithValue("@baslangic", baslangic.Date);
                cmd.Parameters.AddWithValue("@bitis", bitis.Date);
                await conn.OpenAsync();

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    sonuc.Add(new SinifYoklamaModel
                    {
                        SinifYoklamaId = (int)reader["SinifYoklamaId"],
                        OgrenciId = (int)reader["OgrenciId"],
                        KullaniciId = (int)reader["KullaniciId"],
                        Ders1 = reader["Ders1"] != DBNull.Value ? (int)reader["Ders1"] : null,
                        Ders2 = reader["Ders2"] != DBNull.Value ? (int)reader["Ders2"] : null,
                        Ders3 = reader["Ders3"] != DBNull.Value ? (int)reader["Ders3"] : null,
                        Ders4 = reader["Ders4"] != DBNull.Value ? (int)reader["Ders4"] : null,
                        Ders5 = reader["Ders5"] != DBNull.Value ? (int)reader["Ders5"] : null,
                        Ders6 = reader["Ders6"] != DBNull.Value ? (int)reader["Ders6"] : null,
                        Ders7 = reader["Ders7"] != DBNull.Value ? (int)reader["Ders7"] : null,
                        Ders8 = reader["Ders8"] != DBNull.Value ? (int)reader["Ders8"] : null,
                        OlusturulmaTarihi = (DateTime)reader["OlusturulmaTarihi"],
                        GuncellenmeTarihi = reader["GuncellenmeTarihi"] != DBNull.Value ? (DateTime)reader["GuncellenmeTarihi"] : null
                    });
                }
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException("Haftalık yoklama bilgisi alınamadı.", ex);
            }
            return sonuc;
        }

        public async Task<Dictionary<string, string>> OgrenciDetayGetirAsync(int ogrenciId)
        {
            var detaylar = new Dictionary<string, string>();
            try
            {
                await using var conn = new SqlConnection(_connectionString);
                const string query = @"
                    SELECT
                        s.OgrenciAdSoyad, s.OgrenciNo, s.OgrenciKartNo, s.OgrenciGorsel,
                        u.BirimAd,
                        vk.KullaniciAdi AS VeliAdSoyad, vk.Telefon AS VeliTelefon,
                        p.VeliEmail, p.VeliMeslek, p.VeliIsYeri, p.VeliAdres,
                        t.KullaniciAdi AS OgretmenAdSoyad, srv.Plaka
                    FROM Ogrenciler s
                    LEFT JOIN Birimler          u   ON s.BirimId        = u.BirimId
                    LEFT JOIN Kullanicilar      vk  ON s.VeliId         = vk.KullaniciId
                    LEFT JOIN VeliProfiller      p   ON s.VeliId         = p.KullaniciId
                    LEFT JOIN Kullanicilar      t   ON s.OgretmenId     = t.KullaniciId
                    LEFT JOIN ServisProfiller    srv ON s.ServisId       = srv.KullaniciId
                    WHERE s.OgrenciId = @ogrenciId AND s.OgrenciDurum = 1";

                await using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ogrenciId", ogrenciId);
                await conn.OpenAsync();

                await using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    string rawFileName = reader["OgrenciGorsel"]?.ToString() ?? string.Empty;

                    detaylar["OgrenciAdSoyad"]  = reader["OgrenciAdSoyad"]?.ToString()   ?? "Bilinmiyor";
                    detaylar["OgrenciNo"]        = reader["OgrenciNo"]?.ToString()         ?? "-";
                    detaylar["OgrenciKartNo"]    = reader["OgrenciKartNo"]?.ToString()     ?? "-";
                    detaylar["OgrenciGorsel"]    = string.IsNullOrEmpty(rawFileName)
                                                    ? "user_icon.png"
                                                    : rawFileName.Trim().ToLower();
                    detaylar["BirimAd"]          = reader["BirimAd"]?.ToString()           ?? "Atanmamış";
                    detaylar["VeliAdSoyad"]      = reader["VeliAdSoyad"]?.ToString()       ?? "Belirtilmemiş";
                    detaylar["VeliTelefon"]      = reader["VeliTelefon"]?.ToString()       ?? "-";
                    detaylar["VeliEmail"]        = reader["VeliEmail"]?.ToString()         ?? "-";
                    detaylar["VeliMeslek"]       = reader["VeliMeslek"]?.ToString()        ?? "-";
                    detaylar["VeliIsYeri"]       = reader["VeliIsYeri"]?.ToString()        ?? "-";
                    detaylar["VeliAdres"]        = reader["VeliAdres"]?.ToString()         ?? "-";
                    detaylar["OgretmenAdSoyad"]  = reader["OgretmenAdSoyad"]?.ToString()  ?? "Atanmamış";
                    detaylar["Plaka"]            = reader["Plaka"]?.ToString()             ?? "Kullanmıyor";
                }
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException("Öğrenci detayları alınamadı.", ex);
            }
            return detaylar;
        }

        /// <summary>
        /// Yeni öğrenci oluşturur ve oluşturulan kaydın OgrenciId'sini döner.
        /// </summary>
        public async Task<int> EkleAsync(OgrenciKaydetDto dto)
        {
            const string query = @"
                INSERT INTO Ogrenciler
                    (OgrenciAdSoyad, OgrenciNo, OgrenciKartNo, OgrenciCikisDurumu,
                     OgrenciDurum, BirimId, OgretmenId, VeliId, ServisId, OgrenciGorsel)
                OUTPUT INSERTED.OgrenciId
                VALUES
                    (@adSoyad, @no, @kartNo, @cikisDurumu,
                     1, @birimId, @ogretmenId, @veliId, @servisId, @gorsel)";

            try
            {
                await using var conn = new SqlConnection(_connectionString);
                await using var cmd  = new SqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@adSoyad",      dto.OgrenciAdSoyad.Trim().ToUpperInvariant());
                cmd.Parameters.AddWithValue("@no",           dto.OgrenciNo);
                cmd.Parameters.AddWithValue("@kartNo",       (object?)dto.OgrenciKartNo ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@cikisDurumu",  dto.OgrenciCikisDurumu);
                cmd.Parameters.AddWithValue("@birimId",      (object?)dto.BirimId    ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ogretmenId",   (object?)dto.OgretmenId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@veliId",       (object?)dto.VeliId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@servisId",    (object?)dto.ServisId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@gorsel",       (object?)dto.OgrenciGorsel ?? DBNull.Value);

                await conn.OpenAsync();
                var result = await cmd.ExecuteScalarAsync();
                return (int)result!;
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException("Öğrenci eklenemedi.", ex);
            }
        }

        /// <summary>
        /// Mevcut öğrenciyi günceller. Öğrenci bulunamazsa false döner.
        /// </summary>
        public async Task<bool> GuncelleAsync(int ogrenciId, OgrenciKaydetDto dto)
        {
            const string query = @"
                UPDATE Ogrenciler
                SET OgrenciAdSoyad   = @adSoyad,
                    OgrenciNo        = @no,
                    OgrenciKartNo    = @kartNo,
                    OgrenciCikisDurumu = @cikisDurumu,
                    OgrenciDurum     = @durum,
                    BirimId          = @birimId,
                    OgretmenId       = @ogretmenId,
                    VeliId           = @veliId,
                    ServisId         = @servisId,
                    OgrenciGorsel    = @gorsel
                WHERE OgrenciId = @id";

            try
            {
                await using var conn = new SqlConnection(_connectionString);
                await using var cmd  = new SqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@id",           ogrenciId);
                cmd.Parameters.AddWithValue("@adSoyad",      dto.OgrenciAdSoyad.Trim().ToUpperInvariant());
                cmd.Parameters.AddWithValue("@no",           dto.OgrenciNo);
                cmd.Parameters.AddWithValue("@kartNo",       (object?)dto.OgrenciKartNo ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@cikisDurumu",  dto.OgrenciCikisDurumu);
                cmd.Parameters.AddWithValue("@durum",        dto.OgrenciDurum);
                cmd.Parameters.AddWithValue("@birimId",      (object?)dto.BirimId    ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ogretmenId",   (object?)dto.OgretmenId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@veliId",       (object?)dto.VeliId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@servisId",    (object?)dto.ServisId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@gorsel",       (object?)dto.OgrenciGorsel ?? DBNull.Value);

                await conn.OpenAsync();
                int etkilenen = await cmd.ExecuteNonQueryAsync();
                return etkilenen > 0;
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException("Öğrenci güncellenemedi.", ex);
            }
        }

        /// <summary>
        /// Öğrenciyi pasif yapar (soft-delete). Bulunamazsa false döner.
        /// </summary>
        public async Task<bool> SilAsync(int ogrenciId)
        {
            const string query = "UPDATE Ogrenciler SET OgrenciDurum = 0 WHERE OgrenciId = @id";

            try
            {
                await using var conn = new SqlConnection(_connectionString);
                await using var cmd  = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", ogrenciId);

                await conn.OpenAsync();
                int etkilenen = await cmd.ExecuteNonQueryAsync();
                return etkilenen > 0;
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException("Öğrenci silinemedi.", ex);
            }
        }

    }
}
