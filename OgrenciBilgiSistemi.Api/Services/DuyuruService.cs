using Microsoft.Data.SqlClient;
using OgrenciBilgiSistemi.Api.Models;
using OgrenciBilgiSistemi.Shared.Enums;
using OgrenciBilgiSistemi.Shared.Services;

namespace OgrenciBilgiSistemi.Api.Services
{
    public class DuyuruService
    {
        private readonly TenantBaglami _tenantBaglami;
        private readonly BildirimService _bildirimService;
        private string ConnectionString => _tenantBaglami.ConnectionString;

        public DuyuruService(TenantBaglami tenantBaglami, BildirimService bildirimService)
        {
            _tenantBaglami = tenantBaglami;
            _bildirimService = bildirimService;
        }

        // Öğretmen kendi öğrencilerinin velilerine duyuru yayınlar.
        // Aktif öğretmen kontrolü, INSERT, hedef veliler bulma ve her birine bildirim üretme.
        public async Task<int> OgretmenDuyuruOlustur(int ogretmenId, string baslik, string icerik)
        {
            const string ogretmenAktifMi = @"
                SELECT COUNT(*) FROM Kullanicilar k
                INNER JOIN OgretmenProfiller op ON op.KullaniciId = k.KullaniciId
                WHERE k.KullaniciId = @id AND k.Rol = 2
                  AND k.KullaniciDurum = 1 AND op.OgretmenDurum = 1";

            const string insert = @"
                INSERT INTO Duyurular (OlusturanKullaniciId, Hedef, Baslik, Icerik, OlusturulmaTarihi, IsDeleted)
                OUTPUT INSERTED.DuyuruId
                VALUES (@olusturanId, @hedef, @baslik, @icerik, GETDATE(), 0)";

            const string hedefVeliler = @"
                SELECT DISTINCT o.VeliId
                FROM Ogrenciler o
                INNER JOIN Kullanicilar v       ON v.KullaniciId  = o.VeliId
                INNER JOIN VeliProfiller vp     ON vp.KullaniciId = v.KullaniciId
                WHERE o.OgretmenId = @ogretmenId AND o.OgrenciDurum = 1 AND o.VeliId IS NOT NULL
                  AND v.KullaniciDurum = 1 AND vp.VeliDurum = 1";

            await using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            await using (var aktifCmd = new SqlCommand(ogretmenAktifMi, conn))
            {
                aktifCmd.Parameters.AddWithValue("@id", ogretmenId);
                if ((int)(await aktifCmd.ExecuteScalarAsync())! == 0)
                    throw new InvalidOperationException("Öğretmen hesabı şu anda aktif değil.");
            }

            int duyuruId;
            await using (var insertCmd = new SqlCommand(insert, conn))
            {
                insertCmd.Parameters.AddWithValue("@olusturanId", ogretmenId);
                insertCmd.Parameters.AddWithValue("@hedef", (int)DuyuruHedefi.OgretmenKendiOgrencileri);
                insertCmd.Parameters.AddWithValue("@baslik", baslik.Trim());
                insertCmd.Parameters.AddWithValue("@icerik", icerik.Trim());
                duyuruId = (int)(await insertCmd.ExecuteScalarAsync())!;
            }

            var veliIdler = new List<int>();
            await using (var veliCmd = new SqlCommand(hedefVeliler, conn))
            {
                veliCmd.Parameters.AddWithValue("@ogretmenId", ogretmenId);
                await using var reader = await veliCmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    veliIdler.Add(reader.GetInt32(0));
            }

            foreach (var veliId in veliIdler)
            {
                await _bildirimService.Olustur(veliId, (int)BildirimTuru.DuyuruYayinlandi,
                    $"Yeni duyuru: {baslik.Trim()}", null);
            }

            return duyuruId;
        }

        // Veli için kendi velisi olduğu öğrencilerin öğretmenlerinin yayınladığı duyurular
        // ve admin tarafından yayınlanan tüm-veliler duyurularını getirir.
        public async Task<List<DuyuruModel>> VeliDuyurulariGetir(int veliId, int sayfaNo = 1, int sayfaBoyutu = 20)
        {
            const string query = @"
                SELECT d.DuyuruId, d.OlusturanKullaniciId, d.Hedef, d.Baslik, d.Icerik, d.OlusturulmaTarihi,
                       k.KullaniciAdi AS OlusturanAdSoyad
                FROM Duyurular d
                INNER JOIN Kullanicilar k ON k.KullaniciId = d.OlusturanKullaniciId
                WHERE d.IsDeleted = 0
                  AND ( d.Hedef = 2
                     OR (d.Hedef = 1 AND EXISTS (
                            SELECT 1 FROM Ogrenciler o
                            WHERE o.VeliId = @veliId
                              AND o.OgretmenId = d.OlusturanKullaniciId
                              AND o.OgrenciDurum = 1)))
                ORDER BY d.OlusturulmaTarihi DESC
                OFFSET @offset ROWS FETCH NEXT @boyut ROWS ONLY";

            var liste = new List<DuyuruModel>();
            await using var conn = new SqlConnection(ConnectionString);
            await using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@veliId", veliId);
            cmd.Parameters.AddWithValue("@offset", (sayfaNo - 1) * sayfaBoyutu);
            cmd.Parameters.AddWithValue("@boyut", sayfaBoyutu);
            await conn.OpenAsync();

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                liste.Add(new DuyuruModel
                {
                    DuyuruId = reader.GetInt32(reader.GetOrdinal("DuyuruId")),
                    OlusturanKullaniciId = reader.GetInt32(reader.GetOrdinal("OlusturanKullaniciId")),
                    OlusturanAdSoyad = reader.GetString(reader.GetOrdinal("OlusturanAdSoyad")),
                    Hedef = reader.GetInt32(reader.GetOrdinal("Hedef")),
                    Baslik = reader.GetString(reader.GetOrdinal("Baslik")),
                    Icerik = reader.GetString(reader.GetOrdinal("Icerik")),
                    OlusturulmaTarihi = reader.GetDateTime(reader.GetOrdinal("OlusturulmaTarihi"))
                });
            }
            return liste;
        }

        public async Task<DuyuruModel?> DuyuruGetir(int duyuruId)
        {
            const string query = @"
                SELECT d.DuyuruId, d.OlusturanKullaniciId, d.Hedef, d.Baslik, d.Icerik, d.OlusturulmaTarihi,
                       k.KullaniciAdi AS OlusturanAdSoyad
                FROM Duyurular d
                INNER JOIN Kullanicilar k ON k.KullaniciId = d.OlusturanKullaniciId
                WHERE d.DuyuruId = @id AND d.IsDeleted = 0";

            await using var conn = new SqlConnection(ConnectionString);
            await using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", duyuruId);
            await conn.OpenAsync();

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;

            return new DuyuruModel
            {
                DuyuruId = reader.GetInt32(reader.GetOrdinal("DuyuruId")),
                OlusturanKullaniciId = reader.GetInt32(reader.GetOrdinal("OlusturanKullaniciId")),
                OlusturanAdSoyad = reader.GetString(reader.GetOrdinal("OlusturanAdSoyad")),
                Hedef = reader.GetInt32(reader.GetOrdinal("Hedef")),
                Baslik = reader.GetString(reader.GetOrdinal("Baslik")),
                Icerik = reader.GetString(reader.GetOrdinal("Icerik")),
                OlusturulmaTarihi = reader.GetDateTime(reader.GetOrdinal("OlusturulmaTarihi"))
            };
        }
    }
}
