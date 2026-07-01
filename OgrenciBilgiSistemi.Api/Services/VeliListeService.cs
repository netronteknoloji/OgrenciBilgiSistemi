using Microsoft.Data.SqlClient;
using OgrenciBilgiSistemi.Api.Models;
using OgrenciBilgiSistemi.Api.Services.Interfaces;
using OgrenciBilgiSistemi.Shared.Services;

namespace OgrenciBilgiSistemi.Api.Services
{
    public class VeliListeService : IVeliListeService
    {
        private readonly TenantBaglami _tenantBaglami;

        public VeliListeService(TenantBaglami tenantBaglami)
        {
            _tenantBaglami = tenantBaglami;
        }

        private string ConnectionString => _tenantBaglami.ConnectionString;

        /// <summary>
        /// Aktif velileri (VeliProfiller.IsDeleted=0) listeler.
        /// </summary>
        public async Task<List<VeliListeModel>> AktifVelileriGetirAsync()
        {
            var liste = new List<VeliListeModel>();

            const string query = @"
                SELECT k.KullaniciId, k.KullaniciAdi, k.Telefon
                FROM Kullanicilar k
                INNER JOIN VeliProfiller vp ON k.KullaniciId = vp.KullaniciId
                WHERE vp.IsDeleted = 0
                ORDER BY k.KullaniciAdi";

            try
            {
                await using var conn = new SqlConnection(ConnectionString);
                await using var cmd = new SqlCommand(query, conn);

                await conn.OpenAsync();
                await using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    liste.Add(new VeliListeModel
                    {
                        KullaniciId = (int)reader["KullaniciId"],
                        KullaniciAdi = reader["KullaniciAdi"]?.ToString() ?? string.Empty,
                        Telefon = reader["Telefon"] == DBNull.Value ? null : reader["Telefon"].ToString()
                    });
                }
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException("Veli listesi alınamadı.", ex);
            }

            return liste;
        }

        public async Task<VeliDetayModel?> VeliDetayGetirAsync(int kullaniciId)
        {
            const string veliQuery = @"
                SELECT k.KullaniciId, k.KullaniciAdi, k.Telefon,
                       vp.VeliEmail, vp.VeliAdres, vp.VeliMeslek, vp.VeliIsYeri,
                       vp.VeliYakinlik,
                       vp.IsDeleted
                FROM Kullanicilar k
                INNER JOIN VeliProfiller vp ON k.KullaniciId = vp.KullaniciId
                WHERE k.KullaniciId = @id";

            const string ogrenciQuery = @"
                SELECT o.OgrenciId, o.OgrenciAdSoyad, o.OgrenciNo, b.BirimAd
                FROM Ogrenciler o
                LEFT JOIN Birimler b ON o.BirimId = b.BirimId
                WHERE o.VeliId = @id AND o.IsDeleted = 0
                ORDER BY o.OgrenciNo";

            try
            {
                await using var conn = new SqlConnection(ConnectionString);
                await conn.OpenAsync();

                VeliDetayModel? veli;
                await using (var cmd = new SqlCommand(veliQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@id", kullaniciId);
                    await using var reader = await cmd.ExecuteReaderAsync();
                    if (!await reader.ReadAsync())
                        return null;

                    veli = new VeliDetayModel
                    {
                        KullaniciId = (int)reader["KullaniciId"],
                        KullaniciAdi = reader["KullaniciAdi"]?.ToString() ?? string.Empty,
                        Telefon = reader["Telefon"] as string,
                        VeliEmail = reader["VeliEmail"] as string,
                        VeliAdres = reader["VeliAdres"] as string,
                        VeliMeslek = reader["VeliMeslek"] as string,
                        VeliIsYeri = reader["VeliIsYeri"] as string,
                        VeliYakinlik = reader["VeliYakinlik"] as int?,
                        IsDeleted = reader["IsDeleted"] != DBNull.Value && (bool)reader["IsDeleted"]
                    };
                }

                await using (var cmd = new SqlCommand(ogrenciQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@id", kullaniciId);
                    await using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        veli.Cocuklar.Add(new VeliDetayOgrenciModel
                        {
                            OgrenciId = (int)reader["OgrenciId"],
                            OgrenciAdSoyad = reader["OgrenciAdSoyad"]?.ToString() ?? string.Empty,
                            OgrenciNo = (int)reader["OgrenciNo"],
                            BirimAd = reader["BirimAd"] as string
                        });
                    }
                }

                return veli;
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException("Veli detayı alınamadı.", ex);
            }
        }
    }
}
