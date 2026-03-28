using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using OgrenciBilgiSistemi.Api.Models;
using OgrenciBilgiSistemi.Shared.Enums;

namespace OgrenciBilgiSistemi.Api.Services
{
    public class GirisService
    {
        private readonly string _connectionString;

        public GirisService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection bağlantı dizesi eksik.");
        }

        /// <summary>
        /// Kullanıcı adıyla kullanıcıyı bulur, ardından PasswordHasher ile şifreyi doğrular.
        /// Düz metin SQL karşılaştırması yapılmaz; hash doğrulaması C# katmanında gerçekleşir.
        /// </summary>
        public async Task<KullaniciModel?> KimlikDogrulaAsync(string kullaniciAdi, string sifre)
        {
            // 1) Kullanıcıyı sadece KullaniciAdi ile sorgula; şifre SQL'de karşılaştırılmaz.
            const string query = @"
                SELECT
                    K.KullaniciId,
                    K.KullaniciAdi,
                    K.KullaniciDurum,
                    K.Rol,
                    K.Sifre,
                    K.KullaniciAdi AS AdSoyad,
                    CASE WHEN V.KullaniciId IS NOT NULL THEN 1 ELSE 0 END AS VeliProfilVar,
                    CASE WHEN SP.KullaniciId IS NOT NULL THEN 1 ELSE 0 END AS ServisProfilVar
                FROM Kullanicilar K
                LEFT JOIN VeliProfiller V ON V.KullaniciId = K.KullaniciId
                LEFT JOIN ServisProfiller SP ON SP.KullaniciId = K.KullaniciId
                WHERE K.KullaniciAdi = @kullaniciAdi
                  AND K.KullaniciDurum = 1";

            string?        storedHash = null;
            KullaniciModel?  found      = null;

            try
            {
                await using var conn = new SqlConnection(_connectionString);
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

            // 2) Hash doğrulaması — MVC uygulaması ile aynı PasswordHasher kullanılır.
            var hasher = new PasswordHasher<KullaniciModel>();
            var result = hasher.VerifyHashedPassword(found, storedHash, sifre);

            if (result == PasswordVerificationResult.Failed)
                return null;

            return found;
        }

        /// <summary>
        /// Kullanıcı ID'si ile aktif kullanıcıyı getirir (refresh token doğrulaması için).
        /// </summary>
        public async Task<KullaniciModel?> KullaniciIdIleGetirAsync(int kullaniciId)
        {
            const string query = @"
                SELECT
                    K.KullaniciId,
                    K.KullaniciAdi,
                    K.KullaniciDurum,
                    K.Rol,
                    K.KullaniciAdi AS AdSoyad,
                    CASE WHEN V.KullaniciId IS NOT NULL THEN 1 ELSE 0 END AS VeliProfilVar,
                    CASE WHEN SP.KullaniciId IS NOT NULL THEN 1 ELSE 0 END AS ServisProfilVar
                FROM Kullanicilar K
                LEFT JOIN VeliProfiller V ON V.KullaniciId = K.KullaniciId
                LEFT JOIN ServisProfiller SP ON SP.KullaniciId = K.KullaniciId
                WHERE K.KullaniciId = @kullaniciId
                  AND K.KullaniciDurum = 1";

            try
            {
                await using var conn = new SqlConnection(_connectionString);
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
        /// Kullanıcının şifresini günceller. Yeni şifre PasswordHasher ile hash'lenir.
        /// </summary>
        public async Task<bool> SifreDegistirAsync(int kullaniciId, string yeniSifre)
        {
            var hasher = new PasswordHasher<KullaniciModel>();
            var dummy = new KullaniciModel { KullaniciId = kullaniciId };
            var hash = hasher.HashPassword(dummy, yeniSifre);

            const string query = "UPDATE Kullanicilar SET Sifre = @sifre WHERE KullaniciId = @id AND KullaniciDurum = 1";

            try
            {
                await using var conn = new SqlConnection(_connectionString);
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
        /// Admin kullanıcıları hariç tutulur (mobilde giriş yapamıyorlar).
        /// </summary>
        public async Task<List<string>> KullaniciAdiAraAsync(string aranan)
        {
            const string query = @"
                SELECT TOP 10 KullaniciAdi
                FROM Kullanicilar
                WHERE KullaniciDurum = 1
                  AND Rol <> @adminRol
                  AND KullaniciAdi COLLATE Turkish_CI_AI LIKE @aranan + '%'
                ORDER BY KullaniciAdi";

            var sonuclar = new List<string>();

            try
            {
                await using var conn = new SqlConnection(_connectionString);
                await using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@aranan", aranan);
                cmd.Parameters.AddWithValue("@adminRol", (int)KullaniciRolu.Admin);

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
