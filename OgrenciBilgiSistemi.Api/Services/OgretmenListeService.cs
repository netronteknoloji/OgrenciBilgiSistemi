using Microsoft.Data.SqlClient;
using OgrenciBilgiSistemi.Api.Models;
using OgrenciBilgiSistemi.Api.Services.Interfaces;
using OgrenciBilgiSistemi.Shared.Services;

namespace OgrenciBilgiSistemi.Api.Services
{
    public class OgretmenListeService : IOgretmenListeService
    {
        private readonly TenantBaglami _tenantBaglami;
        private string ConnectionString => _tenantBaglami.ConnectionString;

        public OgretmenListeService(TenantBaglami tenantBaglami)
        {
            _tenantBaglami = tenantBaglami;
        }

        public async Task<List<OgretmenListeModel>> AktifOgretmenleriGetir()
        {
            var liste = new List<OgretmenListeModel>();
            const string query = @"
                SELECT k.KullaniciId, k.KullaniciAdi
                FROM Kullanicilar k
                INNER JOIN OgretmenProfiller op ON k.KullaniciId = op.KullaniciId
                WHERE op.IsDeleted = 0
                ORDER BY k.KullaniciAdi";

            await using var conn = new SqlConnection(ConnectionString);
            await using var cmd = new SqlCommand(query, conn);
            await conn.OpenAsync();

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                liste.Add(new OgretmenListeModel
                {
                    KullaniciId = reader.GetInt32(reader.GetOrdinal("KullaniciId")),
                    KullaniciAdi = reader.GetString(reader.GetOrdinal("KullaniciAdi"))
                });
            }
            return liste;
        }

        public async Task<OgretmenDetayModel?> OgretmenDetayGetirAsync(int kullaniciId)
        {
            const string query = @"
                SELECT k.KullaniciId, k.KullaniciAdi, k.Telefon,
                       op.Email, op.GorselPath, op.BirimId,
                       op.IsDeleted,
                       b.BirimAd
                FROM Kullanicilar k
                INNER JOIN OgretmenProfiller op ON k.KullaniciId = op.KullaniciId
                LEFT JOIN Birimler b ON op.BirimId = b.BirimId
                WHERE k.KullaniciId = @id";

            await using var conn = new SqlConnection(ConnectionString);
            await using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", kullaniciId);
            await conn.OpenAsync();

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            return new OgretmenDetayModel
            {
                KullaniciId = (int)reader["KullaniciId"],
                KullaniciAdi = reader["KullaniciAdi"]?.ToString() ?? string.Empty,
                Telefon = reader["Telefon"] as string,
                Email = reader["Email"] as string,
                GorselPath = reader["GorselPath"] as string,
                BirimId = reader["BirimId"] as int?,
                BirimAd = reader["BirimAd"] as string,
                IsDeleted = reader["IsDeleted"] != DBNull.Value && (bool)reader["IsDeleted"]
            };
        }
    }
}
