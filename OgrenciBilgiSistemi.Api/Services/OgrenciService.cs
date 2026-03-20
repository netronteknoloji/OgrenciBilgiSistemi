using Microsoft.Data.SqlClient;
using OgrenciBilgiSistemi.Api.Dtos;
using OgrenciBilgiSistemi.Api.Models;

namespace OgrenciBilgiSistemi.Api.Services
{
    public class OgrenciService
    {
        private readonly string _connectionString;

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

        public async Task<OgrenciModel?> OgrenciGetirAsync(int ogrenciId)
        {
            try
            {
                await using var conn = new SqlConnection(_connectionString);
                const string query = @"
                    SELECT OgrenciId, OgrenciAdSoyad, OgrenciGorsel, BirimId, OgrenciVeliId
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
                        OgrenciVeliId  = reader["OgrenciVeliId"] as int?
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
            if (dersNumarasi < 1 || dersNumarasi > 8)
                throw new ArgumentOutOfRangeException(nameof(dersNumarasi), "Ders numarası 1-8 arasında olmalıdır.");

            var yoklamaDict = new Dictionary<int, int>();
            string dersKolonu = $"Ders{dersNumarasi}";

            try
            {
                await using var conn = new SqlConnection(_connectionString);
                string query = $@"
                    SELECT SY.OgrenciId, SY.{dersKolonu}
                    FROM SinifYoklama SY
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
            if (dersNumarasi < 1 || dersNumarasi > 8)
                throw new ArgumentOutOfRangeException(nameof(dersNumarasi), "Ders numarası 1-8 arasında olmalıdır.");

            string dersKolonu = $"Ders{dersNumarasi}";

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var transaction = (SqlTransaction)await conn.BeginTransactionAsync();

            try
            {
                string query = $@"
                    DECLARE @Bugun DATE = CAST(GETDATE() AS DATE);
                    MERGE INTO SinifYoklama AS target
                    USING (SELECT @ogrenciId AS OgrenciId) AS source
                    ON (target.OgrenciId = source.OgrenciId AND CAST(target.OlusturulmaTarihi AS DATE) = @Bugun)
                    WHEN MATCHED THEN
                        UPDATE SET {dersKolonu} = @durumId, GuncellenmeTarihi = GETDATE()
                    WHEN NOT MATCHED THEN
                        INSERT (OgrenciId, PersonelId, {dersKolonu}, OlusturulmaTarihi)
                        VALUES (@ogrenciId, @personelId, @durumId, GETDATE());";

                foreach (var kayit in yoklamaVerisi)
                {
                    await using var cmd = new SqlCommand(query, conn, transaction);
                    cmd.Parameters.AddWithValue("@ogrenciId", kayit.OgrenciId);
                    cmd.Parameters.AddWithValue("@durumId",   kayit.DurumId);
                    cmd.Parameters.AddWithValue("@personelId", ogretmenId);
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
                        p.VeliAdSoyad, p.VeliTelefon, p.VeliEmail, p.VeliMeslek, p.VeliIsYeri, p.VeliAdres,
                        t.PersonelAdSoyad AS OgretmenAdSoyad, srv.Plaka
                    FROM Ogrenciler s
                    LEFT JOIN Birimler          u   ON s.BirimId        = u.BirimId
                    LEFT JOIN OgrenciVeliler    p   ON s.OgrenciVeliId  = p.OgrenciVeliId
                    LEFT JOIN Personeller       t   ON s.PersonelId     = t.PersonelId
                    LEFT JOIN Servisler         srv ON s.ServisId       = srv.ServisId
                    WHERE s.OgrenciId = @ogrenciId";

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
                     OgrenciDurum, BirimId, PersonelId, OgrenciVeliId, OgrenciGorsel)
                OUTPUT INSERTED.OgrenciId
                VALUES
                    (@adSoyad, @no, @kartNo, @cikisDurumu,
                     1, @birimId, @personelId, @veliId, @gorsel)";

            try
            {
                await using var conn = new SqlConnection(_connectionString);
                await using var cmd  = new SqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@adSoyad",      dto.OgrenciAdSoyad.Trim().ToUpperInvariant());
                cmd.Parameters.AddWithValue("@no",           dto.OgrenciNo);
                cmd.Parameters.AddWithValue("@kartNo",       (object?)dto.OgrenciKartNo ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@cikisDurumu",  dto.OgrenciCikisDurumu);
                cmd.Parameters.AddWithValue("@birimId",      (object?)dto.BirimId    ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@personelId",   (object?)dto.PersonelId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@veliId",       (object?)dto.OgrenciVeliId ?? DBNull.Value);
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
                    PersonelId       = @personelId,
                    OgrenciVeliId    = @veliId,
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
                cmd.Parameters.AddWithValue("@personelId",   (object?)dto.PersonelId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@veliId",       (object?)dto.OgrenciVeliId ?? DBNull.Value);
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

        public async Task<List<SinifYoklamaModel>> HaftalikYoklamaGetirAsync(
            int ogrenciId, DateTime baslangic, DateTime bitis)
        {
            var kayitlar = new List<SinifYoklamaModel>();
            try
            {
                await using var conn = new SqlConnection(_connectionString);
                const string query = @"
                    SELECT
                        SinifYoklamaId, OgrenciId, PersonelId,
                        Ders1, Ders2, Ders3, Ders4, Ders5, Ders6, Ders7, Ders8,
                        OlusturulmaTarihi
                    FROM SinifYoklama
                    WHERE OgrenciId = @ogrenciId
                      AND CAST(OlusturulmaTarihi AS DATE) >= @baslangic
                      AND CAST(OlusturulmaTarihi AS DATE) <= @bitis
                    ORDER BY OlusturulmaTarihi ASC";

                await using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ogrenciId", ogrenciId);
                cmd.Parameters.AddWithValue("@baslangic", baslangic.Date);
                cmd.Parameters.AddWithValue("@bitis",     bitis.Date);
                await conn.OpenAsync();

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    kayitlar.Add(new SinifYoklamaModel
                    {
                        SinifYoklamaId    = (int)reader["SinifYoklamaId"],
                        OgrenciId         = (int)reader["OgrenciId"],
                        PersonelId        = (int)reader["PersonelId"],
                        Ders1             = reader["Ders1"] as int?,
                        Ders2             = reader["Ders2"] as int?,
                        Ders3             = reader["Ders3"] as int?,
                        Ders4             = reader["Ders4"] as int?,
                        Ders5             = reader["Ders5"] as int?,
                        Ders6             = reader["Ders6"] as int?,
                        Ders7             = reader["Ders7"] as int?,
                        Ders8             = reader["Ders8"] as int?,
                        OlusturulmaTarihi = Convert.ToDateTime(reader["OlusturulmaTarihi"])
                    });
                }
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException("Haftalık yoklama alınamadı.", ex);
            }
            return kayitlar;
        }
    }
}
