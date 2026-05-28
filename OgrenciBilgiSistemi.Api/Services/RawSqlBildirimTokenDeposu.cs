using Microsoft.Data.SqlClient;
using OgrenciBilgiSistemi.Push;
using OgrenciBilgiSistemi.Shared.Services;

namespace OgrenciBilgiSistemi.Api.Services
{
    public class RawSqlBildirimTokenDeposu : IBildirimTokenDeposu
    {
        private readonly TenantBaglami _tenantBaglami;
        private string ConnectionString => _tenantBaglami.ConnectionString;

        public RawSqlBildirimTokenDeposu(TenantBaglami tenantBaglami)
        {
            _tenantBaglami = tenantBaglami;
        }

        public async Task UpsertAsync(BildirimCihazKaydi kayit, CancellationToken ct = default)
        {
            const string sql = @"
                MERGE BildirimCihazlari AS hedef
                USING (SELECT @fcmToken AS FcmToken) AS kaynak
                ON hedef.FcmToken = kaynak.FcmToken AND hedef.IsDeleted = 0
                WHEN MATCHED THEN
                    UPDATE SET KullaniciId = @kullaniciId,
                               Platform = @platform,
                               UygulamaSurumu = @uygulamaSurumu,
                               CihazModeli = @cihazModeli,
                               SonGuncelleme = GETDATE()
                WHEN NOT MATCHED THEN
                    INSERT (KullaniciId, FcmToken, Platform, UygulamaSurumu, CihazModeli, OlusturulmaTarihi, SonGuncelleme, IsDeleted)
                    VALUES (@kullaniciId, @fcmToken, @platform, @uygulamaSurumu, @cihazModeli, GETDATE(), GETDATE(), 0);";

            await using var conn = new SqlConnection(ConnectionString);
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@kullaniciId", kayit.KullaniciId);
            cmd.Parameters.AddWithValue("@fcmToken", kayit.FcmToken);
            cmd.Parameters.AddWithValue("@platform", (byte)kayit.Platform);
            cmd.Parameters.AddWithValue("@uygulamaSurumu", (object?)kayit.UygulamaSurumu ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cihazModeli", (object?)kayit.CihazModeli ?? DBNull.Value);

            await conn.OpenAsync(ct);
            await cmd.ExecuteNonQueryAsync(ct);
        }

        public async Task IptalAsync(IEnumerable<string> tokenlar, CancellationToken ct = default)
        {
            var liste = tokenlar.ToList();
            if (liste.Count == 0) return;

            // Parametrize edilmiş IN listesi
            var parametreAdlari = liste.Select((_, i) => "@t" + i).ToList();
            var sql = $@"
                UPDATE BildirimCihazlari
                SET IsDeleted = 1, SonGuncelleme = GETDATE()
                WHERE IsDeleted = 0 AND FcmToken IN ({string.Join(",", parametreAdlari)})";

            await using var conn = new SqlConnection(ConnectionString);
            await using var cmd = new SqlCommand(sql, conn);
            for (var i = 0; i < liste.Count; i++)
                cmd.Parameters.AddWithValue(parametreAdlari[i], liste[i]);

            await conn.OpenAsync(ct);
            await cmd.ExecuteNonQueryAsync(ct);
        }

        public async Task<IReadOnlyList<BildirimTokenKaydi>> AktifTokenleriGetirAsync(int kullaniciId, CancellationToken ct = default)
        {
            const string sql = "SELECT FcmToken, Platform FROM BildirimCihazlari WHERE KullaniciId = @id AND IsDeleted = 0";

            var sonuc = new List<BildirimTokenKaydi>();
            await using var conn = new SqlConnection(ConnectionString);
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", kullaniciId);

            await conn.OpenAsync(ct);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                sonuc.Add(new BildirimTokenKaydi(reader.GetString(0), (PushPlatformu)reader.GetByte(1)));

            return sonuc;
        }
    }
}
