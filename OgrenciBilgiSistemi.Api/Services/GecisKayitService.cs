using Microsoft.Data.SqlClient;
using OgrenciBilgiSistemi.Api.Models;

namespace OgrenciBilgiSistemi.Api.Services
{
    public class GecisKayitService
    {
        private readonly string _connectionString;

        public GecisKayitService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection bağlantı dizesi eksik.");
        }

        /// <summary>
        /// OgrenciDetaylar tablosundan filtrelenmiş giriş/çıkış kayıtlarını getirir.
        /// Tüm parametreler isteğe bağlıdır.
        /// </summary>
        public async Task<List<GecisKayitModel>> GetListAsync(
            DateTime? baslangic,
            DateTime? bitis,
            string?   arama,
            int?      sinifId,
            int?      veliId = null,
            int?      servisId = null)
        {
            var kayitlar = new List<GecisKayitModel>();

            // Dinamik WHERE koşulları (soft-delete: sadece aktif öğrenciler)
            var kosullar = new List<string> { "o.OgrenciDurum = 1" };
            if (baslangic.HasValue) kosullar.Add("COALESCE(od.OgrenciGTarih, od.OgrenciCTarih) >= @baslangic");
            if (bitis.HasValue)    kosullar.Add("COALESCE(od.OgrenciGTarih, od.OgrenciCTarih) <= @bitis");
            if (!string.IsNullOrWhiteSpace(arama))
                kosullar.Add("(o.OgrenciAdSoyad LIKE @arama OR o.OgrenciKartNo LIKE @arama)");
            if (sinifId.HasValue)  kosullar.Add("o.BirimId = @sinifId");
            if (veliId.HasValue)   kosullar.Add("o.VeliId = @veliId");
            if (servisId.HasValue) kosullar.Add("o.ServisId = @servisId");

            string where = kosullar.Count > 0
                ? "WHERE " + string.Join(" AND ", kosullar)
                : string.Empty;

            string query = $@"
                SELECT TOP 500
                    od.OgrenciDetayId, od.OgrenciId, o.OgrenciAdSoyad, o.OgrenciKartNo,
                    b.BirimAd, od.OgrenciGTarih, od.OgrenciCTarih,
                    od.OgrenciGecisTipi, od.IstasyonTipi, c.CihazAdi
                FROM OgrenciDetaylar od
                INNER JOIN Ogrenciler  o ON od.OgrenciId = o.OgrenciId
                LEFT  JOIN Birimler    b ON o.BirimId    = b.BirimId
                LEFT  JOIN Cihazlar    c ON od.CihazId   = c.CihazId
                {where}
                ORDER BY COALESCE(od.OgrenciGTarih, od.OgrenciCTarih) DESC";

            try
            {
                await using var conn = new SqlConnection(_connectionString);
                await using var cmd  = new SqlCommand(query, conn);

                if (baslangic.HasValue) cmd.Parameters.AddWithValue("@baslangic", baslangic.Value.Date);
                if (bitis.HasValue)    cmd.Parameters.AddWithValue("@bitis",     bitis.Value.Date.AddDays(1).AddTicks(-1));
                if (!string.IsNullOrWhiteSpace(arama))
                    cmd.Parameters.AddWithValue("@arama", $"%{arama}%");
                if (sinifId.HasValue)  cmd.Parameters.AddWithValue("@sinifId", sinifId.Value);
                if (veliId.HasValue)   cmd.Parameters.AddWithValue("@veliId", veliId.Value);
                if (servisId.HasValue) cmd.Parameters.AddWithValue("@servisId", servisId.Value);

                await conn.OpenAsync();
                await using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                    kayitlar.Add(MapRow(reader));
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException("Giriş/çıkış kayıtları alınamadı.", ex);
            }

            return kayitlar;
        }

        /// <summary>
        /// Tek bir öğrenciye ait tüm giriş/çıkış kayıtlarını yeniden eskiye döner.
        /// </summary>
        public async Task<List<GecisKayitModel>> GetByOgrenciIdAsync(int ogrenciId)
        {
            var kayitlar = new List<GecisKayitModel>();

            const string query = @"
                SELECT
                    od.OgrenciDetayId, od.OgrenciId, o.OgrenciAdSoyad, o.OgrenciKartNo,
                    b.BirimAd, od.OgrenciGTarih, od.OgrenciCTarih,
                    od.OgrenciGecisTipi, od.IstasyonTipi, c.CihazAdi
                FROM OgrenciDetaylar od
                INNER JOIN Ogrenciler  o ON od.OgrenciId = o.OgrenciId
                LEFT  JOIN Birimler    b ON o.BirimId    = b.BirimId
                LEFT  JOIN Cihazlar    c ON od.CihazId   = c.CihazId
                WHERE od.OgrenciId = @ogrenciId AND o.OgrenciDurum = 1
                ORDER BY COALESCE(od.OgrenciGTarih, od.OgrenciCTarih) DESC";

            try
            {
                await using var conn = new SqlConnection(_connectionString);
                await using var cmd  = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ogrenciId", ogrenciId);

                await conn.OpenAsync();
                await using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                    kayitlar.Add(MapRow(reader));
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException("Öğrenci giriş/çıkış kayıtları alınamadı.", ex);
            }

            return kayitlar;
        }

        private static GecisKayitModel MapRow(SqlDataReader reader) => new()
        {
            OgrenciDetayId  = (int)reader["OgrenciDetayId"],
            OgrenciId       = (int)reader["OgrenciId"],
            OgrenciAdSoyad  = reader["OgrenciAdSoyad"]?.ToString() ?? string.Empty,
            OgrenciKartNo   = reader["OgrenciKartNo"] as string,
            BirimAd         = reader["BirimAd"] as string,
            OgrenciGTarih   = reader["OgrenciGTarih"] as DateTime?,
            OgrenciCTarih   = reader["OgrenciCTarih"] as DateTime?,
            OgrenciGecisTipi = reader["OgrenciGecisTipi"] as string,
            IstasyonTipi    = (Shared.Enums.IstasyonTipi)(short)reader["IstasyonTipi"],
            CihazAdi        = reader["CihazAdi"] as string
        };
    }
}
