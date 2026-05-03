using Microsoft.Data.SqlClient;
using OgrenciBilgiSistemi.Api.Models;
using OgrenciBilgiSistemi.Shared.Enums;
using OgrenciBilgiSistemi.Shared.Services;

namespace OgrenciBilgiSistemi.Api.Services
{
    public class VeliListeService
    {
        private readonly TenantBaglami _tenantBaglami;

        public VeliListeService(TenantBaglami tenantBaglami)
        {
            _tenantBaglami = tenantBaglami;
        }

        private string ConnectionString => _tenantBaglami.ConnectionString;

        /// <summary>
        /// Aktif velileri (Kullanicilar.Rol=Veli + VeliProfiller aktif) listeler.
        /// </summary>
        public async Task<List<VeliListeModel>> AktifVelileriGetirAsync()
        {
            var liste = new List<VeliListeModel>();

            const string query = @"
                SELECT k.KullaniciId, k.KullaniciAdi, k.Telefon
                FROM Kullanicilar k
                INNER JOIN VeliProfiller vp ON k.KullaniciId = vp.KullaniciId
                WHERE k.KullaniciDurum = 1
                  AND k.Rol = @veliRol
                  AND vp.VeliDurum = 1
                ORDER BY k.KullaniciAdi";

            try
            {
                await using var conn = new SqlConnection(ConnectionString);
                await using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@veliRol", (int)KullaniciRolu.Veli);

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
    }
}
