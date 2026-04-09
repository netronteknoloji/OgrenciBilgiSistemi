using Microsoft.Data.SqlClient;
using OgrenciBilgiSistemi.Api.Models;
using OgrenciBilgiSistemi.Shared.Services;

namespace OgrenciBilgiSistemi.Api.Services
{
    public class BirimService
    {
        private readonly TenantBaglami _tenantBaglami;

        public BirimService(TenantBaglami tenantBaglami)
        {
            _tenantBaglami = tenantBaglami;
        }

        private string ConnectionString => _tenantBaglami.ConnectionString;

        /// <summary>
        /// Verilen ID'ye göre birimi getirir. Bulunamazsa null döner.
        /// </summary>
        public async Task<BirimModel?> BirimGetirAsync(int birimId)
        {
            try
            {
                await using var conn = new SqlConnection(ConnectionString);
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
