using Microsoft.Data.SqlClient;
using OgrenciBilgiSistemi.Api.Models;
using OgrenciBilgiSistemi.Api.Services.Interfaces;
using OgrenciBilgiSistemi.Shared.Services;

namespace OgrenciBilgiSistemi.Api.Services
{
    public class OgretmenRandevuService : IOgretmenRandevuService
    {
        private readonly TenantBaglami _tenantBaglami;
        private string ConnectionString => _tenantBaglami.ConnectionString;

        public OgretmenRandevuService(TenantBaglami tenantBaglami)
        {
            _tenantBaglami = tenantBaglami;
        }

        public async Task<List<OgretmenRandevuTakvimModel>> OgretmeninRandevuTakviminiGetir(int ogretmenId)
        {
            var liste = new List<OgretmenRandevuTakvimModel>();
            const string query = @"
                SELECT OgretmenRandevuId, OgretmenKullaniciId, Tarih, BaslangicSaati, BitisSaati
                FROM OgretmenRandevular
                WHERE OgretmenKullaniciId = @ogretmenId AND IsDeleted = 0
                ORDER BY Tarih, BaslangicSaati";

            await using var conn = new SqlConnection(ConnectionString);
            await using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@ogretmenId", ogretmenId);
            await conn.OpenAsync();

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                liste.Add(new OgretmenRandevuTakvimModel
                {
                    OgretmenRandevuId = reader.GetInt32(reader.GetOrdinal("OgretmenRandevuId")),
                    OgretmenKullaniciId = reader.GetInt32(reader.GetOrdinal("OgretmenKullaniciId")),
                    Tarih = reader.GetDateTime(reader.GetOrdinal("Tarih")),
                    BaslangicSaati = reader.GetTimeSpan(reader.GetOrdinal("BaslangicSaati")).ToString(@"hh\:mm"),
                    BitisSaati = reader.GetTimeSpan(reader.GetOrdinal("BitisSaati")).ToString(@"hh\:mm")
                });
            }
            return liste;
        }

        public async Task<int> Ekle(int ogretmenId, DateTime tarih, TimeSpan baslangic, TimeSpan bitis)
        {
            // Aynı öğretmenin aynı tarihte zaman aralığı çakışan başka bir slot'u var mı?
            const string cakismaQuery = @"
                SELECT COUNT(*) FROM OgretmenRandevular
                WHERE OgretmenKullaniciId = @ogretmenId AND IsDeleted = 0
                  AND Tarih = @tarih
                  AND @baslangic < BitisSaati AND @bitis > BaslangicSaati";

            await using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            await using (var cakismaCmd = new SqlCommand(cakismaQuery, conn))
            {
                cakismaCmd.Parameters.AddWithValue("@ogretmenId", ogretmenId);
                cakismaCmd.Parameters.AddWithValue("@tarih", tarih.Date);
                cakismaCmd.Parameters.AddWithValue("@baslangic", baslangic);
                cakismaCmd.Parameters.AddWithValue("@bitis", bitis);
                if ((int)(await cakismaCmd.ExecuteScalarAsync())! > 0)
                    throw new InvalidOperationException("Bu zaman aralığında zaten bir slot tanımlanmış.");
            }

            const string query = @"
                INSERT INTO OgretmenRandevular (OgretmenKullaniciId, Tarih, BaslangicSaati, BitisSaati, IsDeleted)
                OUTPUT INSERTED.OgretmenRandevuId
                VALUES (@ogretmenId, @tarih, @baslangic, @bitis, 0)";

            await using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@ogretmenId", ogretmenId);
            cmd.Parameters.AddWithValue("@tarih", tarih.Date);
            cmd.Parameters.AddWithValue("@baslangic", baslangic);
            cmd.Parameters.AddWithValue("@bitis", bitis);
            return (int)(await cmd.ExecuteScalarAsync())!;
        }

        public async Task<bool> Sil(int ogretmenRandevuId, int ogretmenId)
        {
            const string query = @"
                UPDATE OgretmenRandevular SET IsDeleted = 1
                WHERE OgretmenRandevuId = @id AND OgretmenKullaniciId = @ogretmenId";

            await using var conn = new SqlConnection(ConnectionString);
            await using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", ogretmenRandevuId);
            cmd.Parameters.AddWithValue("@ogretmenId", ogretmenId);
            await conn.OpenAsync();
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<List<RandevuSlotModel>> RandevuSlotlariGetir(int ogretmenId, DateTime baslangicTarih, DateTime bitisTarih)
        {
            try
            {
                const string query = @"
                    SELECT t.Tarih, t.BaslangicSaati, t.BitisSaati, t.OgretmenKullaniciId
                    FROM OgretmenRandevular t
                    INNER JOIN Kullanicilar      k  ON k.KullaniciId  = t.OgretmenKullaniciId
                    INNER JOIN OgretmenProfiller op ON op.KullaniciId = k.KullaniciId
                    WHERE t.OgretmenKullaniciId = @ogretmenId AND t.IsDeleted = 0
                      AND k.KullaniciDurum = 1 AND op.OgretmenDurum = 1
                      AND t.Tarih BETWEEN @baslangic AND @bitis
                      AND NOT EXISTS (
                          SELECT 1 FROM Randevular r
                          WHERE r.OgretmenKullaniciId = t.OgretmenKullaniciId
                            AND r.IsDeleted = 0 AND r.Durum IN (0, 1)
                            AND CAST(r.RandevuTarihi AS DATE) = t.Tarih
                            AND CAST(r.RandevuTarihi AS TIME) < t.BitisSaati
                            AND DATEADD(MINUTE, r.SureDakika, r.RandevuTarihi) >
                                DATEADD(MINUTE, DATEDIFF(MINUTE, 0, t.BaslangicSaati), CAST(t.Tarih AS datetime))
                      )
                    ORDER BY t.Tarih, t.BaslangicSaati";

                await using var conn = new SqlConnection(ConnectionString);
                await using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ogretmenId", ogretmenId);
                cmd.Parameters.AddWithValue("@baslangic", baslangicTarih.Date);
                cmd.Parameters.AddWithValue("@bitis", bitisTarih.Date);
                await conn.OpenAsync();

                var sonuc = new List<RandevuSlotModel>();
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var tarih = reader.GetDateTime(reader.GetOrdinal("Tarih"));
                    var baslangic = reader.GetTimeSpan(reader.GetOrdinal("BaslangicSaati"));

                    if (tarih.Date + baslangic <= DateTime.Now) continue;

                    sonuc.Add(new RandevuSlotModel
                    {
                        Tarih = tarih.Date + baslangic,
                        BaslangicSaati = baslangic.ToString(@"hh\:mm"),
                        BitisSaati = reader.GetTimeSpan(reader.GetOrdinal("BitisSaati")).ToString(@"hh\:mm"),
                        OgretmenKullaniciId = reader.GetInt32(reader.GetOrdinal("OgretmenKullaniciId"))
                    });
                }

                return sonuc;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SLOT HATASI]: {ex.Message}");
                return new();
            }
        }
    }
}
