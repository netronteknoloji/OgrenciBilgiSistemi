using Microsoft.Data.SqlClient;
using OgrenciBilgiSistemi.Api.Models;

namespace OgrenciBilgiSistemi.Api.Services
{
    public class BirimService
    {
        private readonly string _connectionString;

        public BirimService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection bağlantı dizesi eksik.");
        }

        /// <summary>
        /// Verilen ID'ye göre birimi getirir. Bulunamazsa null döner.
        /// </summary>
        public async Task<BirimModel?> BirimGetirAsync(int birimId)
        {
            try
            {
                await using var conn = new SqlConnection(_connectionString);
                const string query = "SELECT BirimId, BirimAd, BirimSinifMi, BirimDurum FROM Birimler WHERE BirimId = @birimId AND BirimDurum = 1";

                await using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@birimId", birimId);
                await conn.OpenAsync();

                await using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new BirimModel
                    {
                        BirimId      = (int)reader["BirimId"],
                        BirimAd      = reader["BirimAd"]?.ToString() ?? string.Empty,
                        BirimSinifMi = (bool)reader["BirimSinifMi"],
                        BirimDurum   = (bool)reader["BirimDurum"]
                    };
                }
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException("Birim bilgisi alınamadı.", ex);
            }
            return null;
        }
    }
}
