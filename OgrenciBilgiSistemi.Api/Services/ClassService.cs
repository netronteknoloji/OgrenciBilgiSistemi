using Microsoft.Data.SqlClient;
using OgrenciBilgiSistemi.Api.Models;

namespace OgrenciBilgiSistemi.Api.Services
{
    public class ClassService
    {
        private readonly string _connectionString;

        public ClassService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection bağlantı dizesi eksik.");
        }

        public async Task<List<UnitWithCountDto>> GetAllClassesWithStudentCountAsync()
        {
            var resultList = new List<UnitWithCountDto>();
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
                    resultList.Add(new UnitWithCountDto
                    {
                        UnitData = new Unit
                        {
                            Id       = (int)reader["BirimId"],
                            Name     = reader["BirimAd"]?.ToString() ?? string.Empty,
                            IsClass  = (bool)reader["BirimSinifMi"],
                            IsActive = (bool)reader["BirimDurum"]
                        },
                        StudentCount = (int)reader["OgrenciSayisi"]
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
