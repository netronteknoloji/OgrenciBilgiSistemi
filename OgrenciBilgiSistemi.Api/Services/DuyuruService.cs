using Microsoft.Data.SqlClient;
using OgrenciBilgiSistemi.Api.Services.Interfaces;
using OgrenciBilgiSistemi.Shared.Enums;
using OgrenciBilgiSistemi.Shared.Services;

namespace OgrenciBilgiSistemi.Api.Services
{
    public class DuyuruService : IDuyuruService
    {
        private readonly TenantBaglami _tenantBaglami;
        private readonly IBildirimService _bildirimService;
        private string ConnectionString => _tenantBaglami.ConnectionString;

        public DuyuruService(TenantBaglami tenantBaglami, IBildirimService bildirimService)
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
                  AND k.IsDeleted = 0 AND op.IsDeleted = 0";

            const string insert = @"
                INSERT INTO Duyurular (OlusturanKullaniciId, Hedef, Baslik, Icerik, OlusturulmaTarihi, IsDeleted)
                OUTPUT INSERTED.DuyuruId
                VALUES (@olusturanId, @hedef, @baslik, @icerik, GETDATE(), 0)";

            // Öğretmen-öğrenci eşlemesi OgretmenProfiller.BirimId = Ogrenciler.BirimId üzerinden.
            const string hedefVeliler = @"
                SELECT DISTINCT o.VeliId
                FROM Ogrenciler o
                INNER JOIN OgretmenProfiller op ON op.BirimId = o.BirimId AND op.KullaniciId = @ogretmenId
                INNER JOIN Kullanicilar v       ON v.KullaniciId  = o.VeliId
                INNER JOIN VeliProfiller vp     ON vp.KullaniciId = v.KullaniciId
                WHERE o.IsDeleted = 0 AND o.VeliId IS NOT NULL
                  AND op.IsDeleted = 0
                  AND v.IsDeleted = 0 AND vp.IsDeleted = 0";

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
            // Veli için: kendi çocuğu olduğu öğrencilerin biriminde aktif olan öğretmenin yayınladığı
            // duyurular (Hedef=1) + admin tarafından tüm velilere yayınlanan duyurular (Hedef=2).
            const string query = @"
                SELECT d.DuyuruId, d.OlusturanKullaniciId, d.Hedef, d.Baslik, d.Icerik, d.OlusturulmaTarihi,
                       k.KullaniciAdi AS OlusturanAdSoyad,
                       CAST(CASE WHEN okuma.DuyuruOkumaId IS NULL THEN 0 ELSE 1 END AS BIT) AS Okundu
                FROM Duyurular d
                INNER JOIN Kullanicilar k ON k.KullaniciId = d.OlusturanKullaniciId
                LEFT JOIN DuyuruOkumalari okuma ON okuma.DuyuruId = d.DuyuruId AND okuma.KullaniciId = @veliId
                WHERE d.IsDeleted = 0
                  AND ( d.Hedef = 2
                     OR (d.Hedef = 1 AND EXISTS (
                            SELECT 1 FROM Ogrenciler o
                            INNER JOIN OgretmenProfiller op ON op.BirimId = o.BirimId
                            WHERE o.VeliId = @veliId
                              AND op.KullaniciId = d.OlusturanKullaniciId
                              AND op.IsDeleted = 0
                              AND o.IsDeleted = 0)))
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
                    OlusturulmaTarihi = reader.GetDateTime(reader.GetOrdinal("OlusturulmaTarihi")),
                    Okundu = reader.GetBoolean(reader.GetOrdinal("Okundu"))
                });
            }
            return liste;
        }

        // Veli için bir duyuruyu okundu olarak işaretler. Yetki kontrolü:
        // duyuru, VeliDuyurulariGetir filtreleriyle aynı şekilde veliye erişilebilir olmalı.
        // Idempotent — aynı (DuyuruId, KullaniciId) çifti için tekrar çağrılırsa duplicate satır oluşturmaz.
        public async Task<bool> OkunduIsaretle(int duyuruId, int veliId)
        {
            const string query = @"
                DECLARE @erisim INT = (
                    SELECT CASE WHEN EXISTS (
                        SELECT 1 FROM Duyurular d
                        WHERE d.DuyuruId = @duyuruId AND d.IsDeleted = 0
                          AND ( d.Hedef = 2
                             OR (d.Hedef = 1 AND EXISTS (
                                    SELECT 1 FROM Ogrenciler o
                                    INNER JOIN OgretmenProfiller op ON op.BirimId = o.BirimId
                                    WHERE o.VeliId = @veliId
                                      AND op.KullaniciId = d.OlusturanKullaniciId
                                      AND op.IsDeleted = 0
                                      AND o.IsDeleted = 0)))
                    ) THEN 1 ELSE 0 END
                );

                IF @erisim = 1
                BEGIN
                    -- Eşzamanlı çağrılar için: INSERT ... WHERE NOT EXISTS atomik ve unique index ile uyumlu
                    INSERT INTO DuyuruOkumalari (DuyuruId, KullaniciId, OkunduTarihi)
                    SELECT @duyuruId, @veliId, GETDATE()
                    WHERE NOT EXISTS (
                        SELECT 1 FROM DuyuruOkumalari
                        WHERE DuyuruId = @duyuruId AND KullaniciId = @veliId
                    );
                END

                SELECT @erisim;";

            await using var conn = new SqlConnection(ConnectionString);
            await using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@duyuruId", duyuruId);
            cmd.Parameters.AddWithValue("@veliId", veliId);
            await conn.OpenAsync();
            var sonuc = await cmd.ExecuteScalarAsync();
            return sonuc is int i && i == 1;
        }

        // Veliye görünen tüm okunmamış duyurular için tek seferde okuma kaydı oluşturur.
        public async Task<int> TumunuOkunduIsaretle(int veliId)
        {
            const string query = @"
                INSERT INTO DuyuruOkumalari (DuyuruId, KullaniciId, OkunduTarihi)
                SELECT d.DuyuruId, @veliId, GETDATE()
                FROM Duyurular d
                LEFT JOIN DuyuruOkumalari okuma ON okuma.DuyuruId = d.DuyuruId AND okuma.KullaniciId = @veliId
                WHERE d.IsDeleted = 0
                  AND okuma.DuyuruOkumaId IS NULL
                  AND ( d.Hedef = 2
                     OR (d.Hedef = 1 AND EXISTS (
                            SELECT 1 FROM Ogrenciler o
                            INNER JOIN OgretmenProfiller op ON op.BirimId = o.BirimId
                            WHERE o.VeliId = @veliId
                              AND op.KullaniciId = d.OlusturanKullaniciId
                              AND op.IsDeleted = 0
                              AND o.IsDeleted = 0)));";

            await using var conn = new SqlConnection(ConnectionString);
            await using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@veliId", veliId);
            await conn.OpenAsync();
            return await cmd.ExecuteNonQueryAsync();
        }

        public async Task<int> OkunmamisSayisi(int veliId)
        {
            const string query = @"
                SELECT COUNT(*)
                FROM Duyurular d
                LEFT JOIN DuyuruOkumalari okuma ON okuma.DuyuruId = d.DuyuruId AND okuma.KullaniciId = @veliId
                WHERE d.IsDeleted = 0
                  AND okuma.DuyuruOkumaId IS NULL
                  AND ( d.Hedef = 2
                     OR (d.Hedef = 1 AND EXISTS (
                            SELECT 1 FROM Ogrenciler o
                            INNER JOIN OgretmenProfiller op ON op.BirimId = o.BirimId
                            WHERE o.VeliId = @veliId
                              AND op.KullaniciId = d.OlusturanKullaniciId
                              AND op.IsDeleted = 0
                              AND o.IsDeleted = 0)));";

            await using var conn = new SqlConnection(ConnectionString);
            await using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@veliId", veliId);
            await conn.OpenAsync();
            return (int)(await cmd.ExecuteScalarAsync())!;
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
