using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using OgrenciBilgiSistemi.Api.Models;

namespace OgrenciBilgiSistemi.Api.Services
{
    public class LoginService
    {
        private readonly string _connectionString;

        public LoginService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection bağlantı dizesi eksik.");
        }

        /// <summary>
        /// Kullanıcı adıyla kullanıcıyı bulur, ardından PasswordHasher ile şifreyi doğrular.
        /// Düz metin SQL karşılaştırması yapılmaz; hash doğrulaması C# katmanında gerçekleşir.
        /// </summary>
        public async Task<User?> AuthenticateAsync(string username, string password)
        {
            // 1) Kullanıcıyı sadece KullaniciAdi ile sorgula; şifre SQL'de karşılaştırılmaz.
            const string query = @"
                SELECT
                    K.KullaniciId,
                    K.KullaniciAdi,
                    K.BirimId,
                    K.KullaniciDurum,
                    K.AdminMi,
                    K.Sifre
                FROM Kullanicilar K
                WHERE K.KullaniciAdi = @username
                  AND K.KullaniciDurum = 1";

            string? storedHash = null;
            User?   found      = null;

            try
            {
                await using var conn = new SqlConnection(_connectionString);
                await using var cmd  = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@username", username);

                await conn.OpenAsync();
                await using var reader = await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    storedHash = reader["Sifre"]?.ToString();
                    found = new User
                    {
                        Id       = (int)reader["KullaniciId"],
                        Username = reader["KullaniciAdi"].ToString() ?? string.Empty,
                        UnitId   = reader["BirimId"] != DBNull.Value ? (int?)reader["BirimId"] : null,
                        IsActive = Convert.ToBoolean(reader["KullaniciDurum"]),
                        IsAdmin  = reader["AdminMi"] != DBNull.Value && Convert.ToBoolean(reader["AdminMi"])
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
            var hasher = new PasswordHasher<User>();
            var result = hasher.VerifyHashedPassword(found, storedHash, password);

            if (result == PasswordVerificationResult.Failed)
                return null;

            return found;
        }
    }
}
