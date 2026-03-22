using Microsoft.Data.SqlClient;
using OgrenciBilgiSistemi.Api.Models;

namespace OgrenciBilgiSistemi.Api.Services
{
    public class ServisService
    {
        private readonly string _connectionString;

        public ServisService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection bağlantı dizesi eksik.");
        }

        /// <summary>
        /// Belirtilen servise atanmış aktif öğrencileri getirir.
        /// </summary>
        public async Task<List<OgrenciModel>> ServisOgrencileriGetir(int servisId)
        {
            var ogrenciler = new List<OgrenciModel>();
            try
            {
                await using var conn = new SqlConnection(_connectionString);
                const string query = @"
                    SELECT O.OgrenciId, O.OgrenciAdSoyad, O.OgrenciGorsel, O.BirimId,
                           B.BirimAd AS SinifAdi
                    FROM Ogrenciler O
                    LEFT JOIN Birimler B ON O.BirimId = B.BirimId
                    WHERE O.ServisId = @servisId AND O.OgrenciDurum = 1
                    ORDER BY O.OgrenciAdSoyad";

                await using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@servisId", servisId);
                await conn.OpenAsync();

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    string rawFileName = reader["OgrenciGorsel"]?.ToString() ?? string.Empty;
                    ogrenciler.Add(new OgrenciModel
                    {
                        OgrenciId      = (int)reader["OgrenciId"],
                        OgrenciAdSoyad = reader["OgrenciAdSoyad"]?.ToString() ?? string.Empty,
                        OgrenciGorsel  = string.IsNullOrEmpty(rawFileName) ? "user_icon.png" : rawFileName,
                        BirimId        = reader["BirimId"] as int?,
                        SinifAdi       = reader["SinifAdi"]?.ToString()
                    });
                }
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException("Servis öğrenci listesi alınamadı.", ex);
            }
            return ogrenciler;
        }

        /// <summary>
        /// Belirtilen servisin bilgilerini getirir.
        /// </summary>
        public async Task<ServisModel?> ServisGetir(int servisId)
        {
            try
            {
                await using var conn = new SqlConnection(_connectionString);
                const string query = @"
                    SELECT ServisId, Plaka, KullaniciId
                    FROM Servisler
                    WHERE ServisId = @servisId";

                await using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@servisId", servisId);
                await conn.OpenAsync();

                await using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new ServisModel
                    {
                        ServisId    = (int)reader["ServisId"],
                        Plaka       = reader["Plaka"]?.ToString() ?? string.Empty,
                        KullaniciId = reader["KullaniciId"] as int?
                    };
                }
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException("Servis bilgisi alınamadı.", ex);
            }
            return null;
        }
    }
}
