using Microsoft.Data.SqlClient;
using OgrenciBilgiSistemi.Api.Dtos;
using OgrenciBilgiSistemi.Api.Models;
using OgrenciBilgiSistemi.Api.Services.Interfaces;
using OgrenciBilgiSistemi.Shared.Dtos;
using OgrenciBilgiSistemi.Shared.Services;

namespace OgrenciBilgiSistemi.Api.Services
{
    public class OgrenciService : IOgrenciService
    {
        private readonly TenantBaglami _tenantBaglami;

        // SQL injection'a karşı whitelist: sadece bu sabit değerler SQL'e girer
        private static readonly Dictionary<int, string> _dersKolonlari = new()
        {
            [1] = "Ders1", [2] = "Ders2", [3] = "Ders3", [4] = "Ders4",
            [5] = "Ders5", [6] = "Ders6", [7] = "Ders7", [8] = "Ders8"
        };

        public OgrenciService(TenantBaglami tenantBaglami)
        {
            _tenantBaglami = tenantBaglami;
        }

        private string ConnectionString => _tenantBaglami.ConnectionString;

        private async Task<int?> BirimdenOgretmenBulAsync(int? birimId)
        {
            if (birimId is null) return null;
            const string sql = @"
                SELECT TOP 1 KullaniciId FROM OgretmenProfiller
                WHERE BirimId = @birimId AND IsDeleted = 0
                ORDER BY KullaniciId";
            await using var conn = new SqlConnection(ConnectionString);
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@birimId", birimId.Value);
            await conn.OpenAsync();
            var result = await cmd.ExecuteScalarAsync();
            return result is int id ? id : null;
        }

        public async Task<List<OgrenciModel>> SinifaGoreOgrencileriGetirAsync(int sinifId)
        {
            var ogrenciler = new List<OgrenciModel>();
            try
            {
                await using var conn = new SqlConnection(ConnectionString);
                const string query = @"
                    SELECT OgrenciId, OgrenciAdSoyad, OgrenciNo, OgrenciGorsel
                    FROM Ogrenciler
                    WHERE BirimId = @sinifId AND IsDeleted = 0
                    ORDER BY OgrenciNo";

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
                        OgrenciNo     = (int)reader["OgrenciNo"],
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

        public async Task<List<OgrenciModel>> TumAktifOgrencileriGetirAsync()
        {
            var ogrenciler = new List<OgrenciModel>();
            try
            {
                await using var conn = new SqlConnection(ConnectionString);
                const string query = @"
                    SELECT O.OgrenciId, O.OgrenciAdSoyad, O.OgrenciNo, O.OgrenciGorsel,
                           O.BirimId, O.VeliId, B.BirimAd AS SinifAdi
                    FROM Ogrenciler O
                    LEFT JOIN Birimler B ON O.BirimId = B.BirimId
                    WHERE O.IsDeleted = 0
                    ORDER BY O.BirimId, O.OgrenciNo";

                await using var cmd = new SqlCommand(query, conn);
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
                        VeliId         = reader["VeliId"] as int?,
                        SinifAdi       = reader["SinifAdi"]?.ToString()
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
                await using var conn = new SqlConnection(ConnectionString);
                const string query = @"
                    SELECT O.OgrenciId, O.OgrenciAdSoyad, O.OgrenciGorsel, O.OgrenciNo,
                           O.BirimId, O.ServisId, O.VeliId, B.BirimAd AS SinifAdi,
                           sinifOgretmen.KullaniciId AS OgretmenId
                    FROM Ogrenciler O
                    LEFT JOIN Birimler B ON O.BirimId = B.BirimId
                    OUTER APPLY (
                        SELECT TOP 1 op.KullaniciId
                        FROM OgretmenProfiller op
                        WHERE op.BirimId = O.BirimId AND op.IsDeleted = 0
                        ORDER BY op.KullaniciId
                    ) sinifOgretmen
                    WHERE O.VeliId = @veliId AND O.IsDeleted = 0";

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
                        OgretmenId     = reader["OgretmenId"] as int?,
                        VeliId         = reader["VeliId"] as int?,
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
                await using var conn = new SqlConnection(ConnectionString);
                const string query = @"
                    SELECT OgrenciId, OgrenciAdSoyad, OgrenciGorsel, BirimId, VeliId, ServisId
                    FROM Ogrenciler
                    WHERE OgrenciId = @ogrenciId AND IsDeleted = 0";

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

        /// <summary>
        /// Bir sınıfın belirli bir tarihteki tüm öğrenci yoklama özetini getirir.
        /// Her öğrenci için 8 ders durumu ve yoklamayı yapan öğretmen bilgisi döner.
        /// Yoklama hiç alınmamışsa Ders1-8 alanları null olur.
        /// </summary>
        public async Task<List<SinifYoklamaOzetModel>> SinifYoklamaOzetiGetirAsync(int sinifId, DateTime tarih)
        {
            var liste = new List<SinifYoklamaOzetModel>();

            // Tarih aralığı: tarih günü başlangıcı (00:00) - ertesi gün başlangıcı
            var gunBaslangic = tarih.Date;
            var gunSonu = gunBaslangic.AddDays(1);

            const string query = @"
                SELECT o.OgrenciId, o.OgrenciAdSoyad, o.OgrenciNo,
                       sy.Ders1, sy.Ders2, sy.Ders3, sy.Ders4,
                       sy.Ders5, sy.Ders6, sy.Ders7, sy.Ders8,
                       sy.KullaniciId, k.KullaniciAdi
                FROM Ogrenciler o
                LEFT JOIN SinifYoklamalar sy
                    ON sy.OgrenciId = o.OgrenciId
                    AND sy.OlusturulmaTarihi >= @gunBaslangic
                    AND sy.OlusturulmaTarihi < @gunSonu
                LEFT JOIN Kullanicilar k ON k.KullaniciId = sy.KullaniciId
                WHERE o.BirimId = @sinifId AND o.IsDeleted = 0
                ORDER BY o.OgrenciNo";

            try
            {
                await using var conn = new SqlConnection(ConnectionString);
                await using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@sinifId", sinifId);
                cmd.Parameters.AddWithValue("@gunBaslangic", gunBaslangic);
                cmd.Parameters.AddWithValue("@gunSonu", gunSonu);

                await conn.OpenAsync();
                await using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    liste.Add(new SinifYoklamaOzetModel
                    {
                        OgrenciId = (int)reader["OgrenciId"],
                        OgrenciAdSoyad = reader["OgrenciAdSoyad"]?.ToString() ?? string.Empty,
                        OgrenciNo = (int)reader["OgrenciNo"],
                        Ders1 = reader["Ders1"] as int?,
                        Ders2 = reader["Ders2"] as int?,
                        Ders3 = reader["Ders3"] as int?,
                        Ders4 = reader["Ders4"] as int?,
                        Ders5 = reader["Ders5"] as int?,
                        Ders6 = reader["Ders6"] as int?,
                        Ders7 = reader["Ders7"] as int?,
                        Ders8 = reader["Ders8"] as int?,
                        KullaniciId = reader["KullaniciId"] as int?,
                        KullaniciAdi = reader["KullaniciAdi"] as string
                    });
                }
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException("Sınıf yoklama özeti alınamadı.", ex);
            }

            return liste;
        }

        public async Task<Dictionary<int, int>> MevcutYoklamaGetirAsync(int sinifId, int dersNumarasi)
        {
            if (!_dersKolonlari.TryGetValue(dersNumarasi, out var dersKolonu))
                throw new ArgumentOutOfRangeException(nameof(dersNumarasi), "Ders numarası 1-8 arasında olmalıdır.");

            var yoklamaDict = new Dictionary<int, int>();

            try
            {
                await using var conn = new SqlConnection(ConnectionString);
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

            await using var conn = new SqlConnection(ConnectionString);
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

        public async Task<List<SinifYoklamaDto>> HaftalikYoklamaGetirAsync(int ogrenciId, DateTime baslangic, DateTime bitis)
        {
            var sonuc = new List<SinifYoklamaDto>();
            try
            {
                await using var conn = new SqlConnection(ConnectionString);
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
                    sonuc.Add(new SinifYoklamaDto
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

        public async Task<OgrenciDetayDto?> OgrenciDetayGetirAsync(int ogrenciId)
        {
            try
            {
                await using var conn = new SqlConnection(ConnectionString);
                const string query = @"
                    SELECT
                        s.OgrenciAdSoyad, s.OgrenciNo, s.OgrenciKartNo, s.OgrenciGorsel,
                        s.OgrenciCikisDurumu, s.VeliId, s.ServisId,
                        u.BirimAd,
                        vk.KullaniciAdi AS VeliAdSoyad, vk.Telefon AS VeliTelefon,
                        p.VeliEmail, p.VeliMeslek, p.VeliIsYeri, p.VeliAdres,
                        t.KullaniciAdi AS OgretmenAdSoyad, srv.Plaka
                    FROM Ogrenciler s
                    LEFT JOIN Birimler          u   ON s.BirimId        = u.BirimId
                    LEFT JOIN Kullanicilar      vk  ON s.VeliId         = vk.KullaniciId
                    LEFT JOIN VeliProfiller      p   ON s.VeliId         = p.KullaniciId
                    OUTER APPLY (
                        SELECT TOP 1 op.KullaniciId
                        FROM OgretmenProfiller op
                        WHERE op.BirimId = s.BirimId AND op.IsDeleted = 0
                        ORDER BY op.KullaniciId
                    ) sinifOgretmen
                    LEFT JOIN Kullanicilar      t   ON sinifOgretmen.KullaniciId = t.KullaniciId
                    LEFT JOIN ServisProfiller    srv ON s.ServisId       = srv.KullaniciId
                    WHERE s.OgrenciId = @ogrenciId AND s.IsDeleted = 0";

                await using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ogrenciId", ogrenciId);
                await conn.OpenAsync();

                await using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    string rawFileName = reader["OgrenciGorsel"]?.ToString() ?? string.Empty;

                    return new OgrenciDetayDto
                    {
                        VeliId           = reader["VeliId"] != DBNull.Value ? (int?)reader["VeliId"] : null,
                        ServisId         = reader["ServisId"] != DBNull.Value ? (int?)reader["ServisId"] : null,
                        OgrenciAdSoyad   = reader["OgrenciAdSoyad"]?.ToString()  ?? "Bilinmiyor",
                        OgrenciNo        = reader["OgrenciNo"]?.ToString()       ?? "-",
                        OgrenciKartNo    = reader["OgrenciKartNo"]?.ToString()   ?? "-",
                        OgrenciGorsel    = string.IsNullOrEmpty(rawFileName)
                                             ? "user_icon.png"
                                             : rawFileName.Trim().ToLower(),
                        OgrenciCikisDurumu = reader["OgrenciCikisDurumu"] != DBNull.Value
                                             ? (int)reader["OgrenciCikisDurumu"] : 0,
                        BirimAd          = reader["BirimAd"]?.ToString()         ?? "Atanmamış",
                        VeliAdSoyad      = reader["VeliAdSoyad"]?.ToString()     ?? "Belirtilmemiş",
                        VeliTelefon      = reader["VeliTelefon"]?.ToString()     ?? "-",
                        VeliEmail        = reader["VeliEmail"]?.ToString()       ?? "-",
                        VeliMeslek       = reader["VeliMeslek"]?.ToString()      ?? "-",
                        VeliIsYeri       = reader["VeliIsYeri"]?.ToString()      ?? "-",
                        VeliAdres        = reader["VeliAdres"]?.ToString()       ?? "-",
                        OgretmenAdSoyad  = reader["OgretmenAdSoyad"]?.ToString() ?? "Atanmamış",
                        Plaka            = reader["Plaka"]?.ToString()           ?? "Kullanmıyor"
                    };
                }
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException("Öğrenci detayları alınamadı.", ex);
            }
            return null;
        }

        /// <summary>
        /// Yeni öğrenci oluşturur ve oluşturulan kaydın OgrenciId'sini döner.
        /// </summary>
        public async Task<int> EkleAsync(OgrenciKaydetDto dto)
        {
            const string query = @"
                INSERT INTO Ogrenciler
                    (OgrenciAdSoyad, OgrenciNo, OgrenciKartNo, OgrenciCikisDurumu,
                     IsDeleted, BirimId, OgretmenId, VeliId, ServisId, OgrenciGorsel)
                OUTPUT INSERTED.OgrenciId
                VALUES
                    (@adSoyad, @no, @kartNo, @cikisDurumu,
                     0, @birimId, @ogretmenId, @veliId, @servisId, @gorsel)";

            try
            {
                await using var conn = new SqlConnection(ConnectionString);
                await using var cmd  = new SqlCommand(query, conn);

                var kartNo = string.IsNullOrWhiteSpace(dto.OgrenciKartNo) ? null : dto.OgrenciKartNo.Trim();
                cmd.Parameters.AddWithValue("@adSoyad",      dto.OgrenciAdSoyad.Trim().ToUpperInvariant());
                cmd.Parameters.AddWithValue("@no",           dto.OgrenciNo);
                cmd.Parameters.AddWithValue("@kartNo",       (object?)kartNo ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@cikisDurumu",  dto.OgrenciCikisDurumu);
                var ogretmenId = await BirimdenOgretmenBulAsync(dto.BirimId);
                cmd.Parameters.AddWithValue("@birimId",      (object?)dto.BirimId    ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ogretmenId",   (object?)ogretmenId ?? DBNull.Value);
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
                    IsDeleted        = @durum,
                    BirimId          = @birimId,
                    OgretmenId       = @ogretmenId,
                    VeliId           = @veliId,
                    ServisId         = @servisId,
                    OgrenciGorsel    = @gorsel
                WHERE OgrenciId = @id AND IsDeleted = 0";

            try
            {
                await using var conn = new SqlConnection(ConnectionString);
                await using var cmd  = new SqlCommand(query, conn);

                var kartNo = string.IsNullOrWhiteSpace(dto.OgrenciKartNo) ? null : dto.OgrenciKartNo.Trim();
                cmd.Parameters.AddWithValue("@id",           ogrenciId);
                cmd.Parameters.AddWithValue("@adSoyad",      dto.OgrenciAdSoyad.Trim().ToUpperInvariant());
                cmd.Parameters.AddWithValue("@no",           dto.OgrenciNo);
                cmd.Parameters.AddWithValue("@kartNo",       (object?)kartNo ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@cikisDurumu",  dto.OgrenciCikisDurumu);
                cmd.Parameters.AddWithValue("@durum",        dto.IsDeleted);
                var ogretmenId = await BirimdenOgretmenBulAsync(dto.BirimId);
                cmd.Parameters.AddWithValue("@birimId",      (object?)dto.BirimId    ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ogretmenId",   (object?)ogretmenId ?? DBNull.Value);
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
            const string query = "UPDATE Ogrenciler SET IsDeleted = 1 WHERE OgrenciId = @id";

            try
            {
                await using var conn = new SqlConnection(ConnectionString);
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
