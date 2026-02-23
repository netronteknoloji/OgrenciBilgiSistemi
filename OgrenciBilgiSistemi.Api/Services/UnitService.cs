using Microsoft.Data.SqlClient;
using OgrenciBilgiSistemi.Api.Models;

namespace OgrenciBilgiSistemi.Api.Services
{
    public class UnitService
    {
        private readonly string _connectionString;

        public UnitService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection bağlantı dizesi eksik.");
        }

        /// <summary>
        /// Verilen ID'ye göre birimi getirir. Bulunamazsa null döner.
        /// </summary>
        public async Task<Unit?> GetUnitByIdAsync(int unitId)
        {
            try
            {
                await using var conn = new SqlConnection(_connectionString);
                const string query = "SELECT BirimId, BirimAd, BirimSinifMi FROM Birimler WHERE BirimId = @unitId";

                await using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@unitId", unitId);
                await conn.OpenAsync();

                await using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new Unit
                    {
                        Id      = (int)reader["BirimId"],
                        Name    = reader["BirimAd"]?.ToString() ?? string.Empty,
                        IsClass = (bool)reader["BirimSinifMi"]
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
