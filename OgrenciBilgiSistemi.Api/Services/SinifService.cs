using Microsoft.Data.SqlClient;
using OgrenciBilgiSistemi.Api.Models;
using OgrenciBilgiSistemi.Api.Services.Interfaces;
using OgrenciBilgiSistemi.Shared.Services;

namespace OgrenciBilgiSistemi.Api.Services
{
    public class SinifService : ISinifService
    {
        private readonly TenantBaglami _tenantBaglami;

        public SinifService(TenantBaglami tenantBaglami)
        {
            _tenantBaglami = tenantBaglami;
        }

        private string ConnectionString => _tenantBaglami.ConnectionString;

        public async Task<List<BirimOgrenciSayisiModel>> TumSiniflariOgrenciSayisiIleGetirAsync()
        {
            var resultList = new List<BirimOgrenciSayisiModel>();
            try
            {
                await using var conn = new SqlConnection(ConnectionString);
                const string query = @"
                    SELECT BirimId, BirimAd, BirimSinifMi, IsDeleted,
                           (SELECT COUNT(*) FROM Ogrenciler
                            WHERE BirimId = B.BirimId AND IsDeleted = 0) AS OgrenciSayisi
                    FROM Birimler B
                    WHERE BirimSinifMi = 1 AND IsDeleted = 0";

                await using var cmd = new SqlCommand(query, conn);
                await conn.OpenAsync();

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    resultList.Add(new BirimOgrenciSayisiModel
                    {
                        Birim = new BirimModel
                        {
                            BirimId     = (int)reader["BirimId"],
                            BirimAd     = reader["BirimAd"]?.ToString() ?? string.Empty,
                            BirimSinifMi = (bool)reader["BirimSinifMi"],
                            IsDeleted   = (bool)reader["IsDeleted"]
                        },
                        OgrenciSayisi = (int)reader["OgrenciSayisi"]
                    });
                }
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException("Sınıf listesi alınamadı.", ex);
            }
            return resultList;
        }
    }
}
