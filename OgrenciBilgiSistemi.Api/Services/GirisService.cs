using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using OgrenciBilgiSistemi.Api.Models;
using OgrenciBilgiSistemi.Shared.Enums;
using OgrenciBilgiSistemi.Shared.Services;

namespace OgrenciBilgiSistemi.Api.Services
{
    public class GirisService
    {
        private readonly TenantBaglami _tenantBaglami;

        public GirisService(TenantBaglami tenantBaglami)
        {
            _tenantBaglami = tenantBaglami;
        }

        private string ConnectionString => _tenantBaglami.ConnectionString;

        /// <summary>
        /// Kullanıcı adıyla kullanıcıyı bulur, ardından PasswordHasher ile şifreyi doğrular.
        /// Login akışında explicit connectionString kullanılır (TenantBaglami henüz dolu değil).
        /// </summary>
        public async Task<KullaniciModel?> KimlikDogrulaAsync(string kullaniciAdi, string sifre, string connectionString)
        {
            const string query = @"
                SELECT
                    K.KullaniciId,
                    K.KullaniciAdi,
                    K.KullaniciDurum,
                    K.Rol,
                    K.Sifre,
                    K.KullaniciAdi AS AdSoyad,
                    OP.BirimId,
                    CASE WHEN V.KullaniciId IS NOT NULL THEN 1 ELSE 0 END AS VeliProfilVar,
                    CASE WHEN SP.KullaniciId IS NOT NULL THEN 1 ELSE 0 END AS ServisProfilVar
                FROM Kullanicilar K
                LEFT JOIN VeliProfiller V ON V.KullaniciId = K.KullaniciId
                LEFT JOIN ServisProfiller SP ON SP.KullaniciId = K.KullaniciId
                LEFT JOIN OgretmenProfiller OP ON OP.KullaniciId = K.KullaniciId
                WHERE K.KullaniciAdi = @kullaniciAdi
                  AND K.KullaniciDurum = 1";

            string?        storedHash = null;
            KullaniciModel?  found      = null;

            try
            {
                await using var conn = new SqlConnection(connectionString);
                await using var cmd  = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@kullaniciAdi", kullaniciAdi);

                await conn.OpenAsync();
                await using var reader = await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    storedHash = reader["Sifre"]?.ToString();
                    found = new KullaniciModel
                    {
                        KullaniciId    = (int)reader["KullaniciId"],
                        KullaniciAdi   = reader["KullaniciAdi"].ToString() ?? string.Empty,
                        KullaniciDurum = Convert.ToBoolean(reader["KullaniciDurum"]),
                        Rol            = reader["Rol"] != DBNull.Value ? (KullaniciRolu)Convert.ToInt32(reader["Rol"]) : KullaniciRolu.Ogretmen,
                        AdSoyad        = reader["AdSoyad"]?.ToString(),
                        BirimId        = reader["BirimId"] != DBNull.Value ? (int?)Convert.ToInt32(reader["BirimId"]) : null,
                        VeliProfilVar  = Convert.ToBoolean(reader["VeliProfilVar"]),
                        ServisProfilVar = Convert.ToBoolean(reader["ServisProfilVar"])
                    };
                }
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException("Veritabanı sorgusunda hata oluştu.", ex);
            }

            if (found is null || string.IsNullOrEmpty(storedHash))
                return null;

            var hasher = new PasswordHasher<KullaniciModel>();
            var result = hasher.VerifyHashedPassword(found, storedHash, sifre);

            if (result == PasswordVerificationResult.Failed)
                return null;

            return found;
        }

        /// <summary>
        /// Kullanıcı ID'si ile aktif kullanıcıyı getirir (refresh token doğrulaması için).
        /// Explicit connectionString kabul eder (refresh endpoint anonymous olduğu için).
        /// </summary>
        public async Task<KullaniciModel?> KimlikDogrulaAsync_IdIle(int kullaniciId, string connectionString)
        {
            const string query = @"
                SELECT
                    K.KullaniciId,
                    K.KullaniciAdi,
                    K.KullaniciDurum,
                    K.Rol,
                    K.KullaniciAdi AS AdSoyad,
                    OP.BirimId,
                    CASE WHEN V.KullaniciId IS NOT NULL THEN 1 ELSE 0 END AS VeliProfilVar,
                    CASE WHEN SP.KullaniciId IS NOT NULL THEN 1 ELSE 0 END AS ServisProfilVar
                FROM Kullanicilar K
                LEFT JOIN VeliProfiller V ON V.KullaniciId = K.KullaniciId
                LEFT JOIN ServisProfiller SP ON SP.KullaniciId = K.KullaniciId
                LEFT JOIN OgretmenProfiller OP ON OP.KullaniciId = K.KullaniciId
                WHERE K.KullaniciId = @kullaniciId
                  AND K.KullaniciDurum = 1";

            try
            {
                await using var conn = new SqlConnection(connectionString);
                await using var cmd  = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@kullaniciId", kullaniciId);

                await conn.OpenAsync();
                await using var reader = await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    return new KullaniciModel
                    {
                        KullaniciId    = (int)reader["KullaniciId"],
                        KullaniciAdi   = reader["KullaniciAdi"].ToString() ?? string.Empty,
                        KullaniciDurum = Convert.ToBoolean(reader["KullaniciDurum"]),
                        Rol            = reader["Rol"] != DBNull.Value ? (KullaniciRolu)Convert.ToInt32(reader["Rol"]) : KullaniciRolu.Ogretmen,
                        AdSoyad        = reader["AdSoyad"]?.ToString(),
                        BirimId        = reader["BirimId"] != DBNull.Value ? (int?)Convert.ToInt32(reader["BirimId"]) : null,
                        VeliProfilVar  = Convert.ToBoolean(reader["VeliProfilVar"]),
                        ServisProfilVar = Convert.ToBoolean(reader["ServisProfilVar"])
                    };
                }
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException("Veritabanı sorgusunda hata oluştu.", ex);
            }

            return null;
        }

        /// <summary>
        /// Kullanıcının şifresini günceller. TenantBaglami'dan connection string kullanır.
        /// </summary>
        public async Task<bool> SifreDegistirAsync(int kullaniciId, string yeniSifre)
        {
            var hasher = new PasswordHasher<KullaniciModel>();
            var dummy = new KullaniciModel { KullaniciId = kullaniciId };
            var hash = hasher.HashPassword(dummy, yeniSifre);

            const string query = "UPDATE Kullanicilar SET Sifre = @sifre WHERE KullaniciId = @id AND KullaniciDurum = 1";

            try
            {
                await using var conn = new SqlConnection(ConnectionString);
                await using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@sifre", hash);
                cmd.Parameters.AddWithValue("@id", kullaniciId);

                await conn.OpenAsync();
                int etkilenen = await cmd.ExecuteNonQueryAsync();
                return etkilenen > 0;
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException("Şifre güncellenemedi.", ex);
            }
        }

        /// <summary>
        /// Kullanıcı adının ilk harflerine göre eşleşen aktif kullanıcıları arar.
        /// Login akışında explicit connectionString kullanılır.
        /// </summary>
        public async Task<List<string>> KullaniciAdiAraAsync(string aranan, string connectionString)
        {
            const string query = @"
                SELECT TOP 10 KullaniciAdi
                FROM Kullanicilar
                WHERE KullaniciDurum = 1
                  AND KullaniciAdi COLLATE Turkish_CI_AI LIKE @aranan + '%'
                ORDER BY KullaniciAdi";

            var sonuclar = new List<string>();

            try
            {
                await using var conn = new SqlConnection(connectionString);
                await using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@aranan", aranan);

                await conn.OpenAsync();
                await using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    sonuclar.Add(reader["KullaniciAdi"].ToString() ?? string.Empty);
                }
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException("Kullanıcı arama sorgusunda hata oluştu.", ex);
            }

            return sonuclar;
        }
    }
}
