using Microsoft.Data.SqlClient;
using OgrenciBilgiSistemi.Api.Models;

namespace OgrenciBilgiSistemi.Api.Services
{
    public class SinifService
    {
        private readonly string _connectionString;

        public SinifService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection bağlantı dizesi eksik.");
        }

        public async Task<List<BirimOgrenciSayisiModel>> TumSiniflariOgrenciSayisiIleGetirAsync()
        {
            var resultList = new List<BirimOgrenciSayisiModel>();
            try
            {
                await using var conn = new SqlConnection(_connectionString);
                const string query = @"
                    SELECT BirimId, BirimAd, BirimSinifMi, BirimDurum,
                           (SELECT COUNT(*) FROM Ogrenciler
                            WHERE BirimId = B.BirimId AND OgrenciDurum = 1) AS OgrenciSayisi
                    FROM Birimler B
                    WHERE BirimSinifMi = 1 AND BirimDurum = 1";

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
                            BirimDurum  = (bool)reader["BirimDurum"]
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
