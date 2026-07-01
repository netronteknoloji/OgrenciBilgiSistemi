using Microsoft.Data.SqlClient;
using OgrenciBilgiSistemi.Api.Services.Interfaces;
using OgrenciBilgiSistemi.Shared.Enums;
using OgrenciBilgiSistemi.Shared.Services;

namespace OgrenciBilgiSistemi.Api.Services
{
    public class RandevuService : IRandevuService
    {
        private readonly TenantBaglami _tenantBaglami;
        private string ConnectionString => _tenantBaglami.ConnectionString;

        private static readonly Dictionary<int, string> _durumAdlari = new()
        {
            [0] = "Beklemede",
            [1] = "Onaylandı",
            [2] = "Reddedildi",
            [3] = "İptal Edildi",
            [4] = "Tamamlandı"
        };

        public RandevuService(TenantBaglami tenantBaglami)
        {
            _tenantBaglami = tenantBaglami;
        }

        public async Task<List<RandevuModel>> KullanicininRandevulariniGetir(int kullaniciId, string rol, int sayfaNo = 1, int sayfaBoyutu = 5)
        {
            var liste = new List<RandevuModel>();
            const string query = @"
                SELECT r.RandevuId, r.OgretmenKullaniciId, r.VeliKullaniciId, r.OgrenciId,
                       r.RandevuTarihi, r.SureDakika, r.Durum, r.[Not], r.OgretmenTarafindanOlusturuldu,
                       r.OlusturulmaTarihi,
                       og.KullaniciAdi AS OgretmenAd,
                       v.KullaniciAdi AS VeliAd,
                       o.OgrenciAdSoyad
                FROM Randevular r
                INNER JOIN Kullanicilar      og ON r.OgretmenKullaniciId = og.KullaniciId
                INNER JOIN OgretmenProfiller op ON op.KullaniciId = og.KullaniciId
                INNER JOIN Kullanicilar      v  ON r.VeliKullaniciId = v.KullaniciId
                INNER JOIN VeliProfiller     vp ON vp.KullaniciId = v.KullaniciId
                LEFT  JOIN Ogrenciler        o  ON r.OgrenciId = o.OgrenciId
                WHERE r.IsDeleted = 0
                  AND og.IsDeleted = 0 AND op.IsDeleted = 0
                  AND v.IsDeleted = 0 AND vp.IsDeleted = 0
                  AND ((@rol = 'Ogretmen' AND r.OgretmenKullaniciId = @kullaniciId)
                    OR (@rol = 'Veli'      AND r.VeliKullaniciId = @kullaniciId))
                ORDER BY r.RandevuTarihi DESC
                OFFSET @offset ROWS FETCH NEXT @boyut ROWS ONLY";

            await using var conn = new SqlConnection(ConnectionString);
            await using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@kullaniciId", kullaniciId);
            cmd.Parameters.AddWithValue("@rol", rol);
            cmd.Parameters.AddWithValue("@offset", (sayfaNo - 1) * sayfaBoyutu);
            cmd.Parameters.AddWithValue("@boyut", sayfaBoyutu);
            await conn.OpenAsync();

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var durum = reader.GetInt32(reader.GetOrdinal("Durum"));
                liste.Add(new RandevuModel
                {
                    RandevuId = reader.GetInt32(reader.GetOrdinal("RandevuId")),
                    OgretmenKullaniciId = reader.GetInt32(reader.GetOrdinal("OgretmenKullaniciId")),
                    OgretmenAdSoyad = reader["OgretmenAd"]?.ToString() ?? "",
                    VeliKullaniciId = reader.GetInt32(reader.GetOrdinal("VeliKullaniciId")),
                    VeliAdSoyad = reader["VeliAd"]?.ToString() ?? "",
                    OgrenciId = reader.IsDBNull(reader.GetOrdinal("OgrenciId")) ? null : reader.GetInt32(reader.GetOrdinal("OgrenciId")),
                    OgrenciAdSoyad = reader["OgrenciAdSoyad"]?.ToString(),
                    RandevuTarihi = reader.GetDateTime(reader.GetOrdinal("RandevuTarihi")),
                    SureDakika = reader.GetInt32(reader.GetOrdinal("SureDakika")),
                    Durum = durum,
                    DurumAdi = _durumAdlari.GetValueOrDefault(durum, "Bilinmiyor"),
                    Not = reader["Not"]?.ToString(),
                    OgretmenTarafindanOlusturuldu = reader.GetBoolean(reader.GetOrdinal("OgretmenTarafindanOlusturuldu")),
                    OlusturulmaTarihi = reader.GetDateTime(reader.GetOrdinal("OlusturulmaTarihi"))
                });
            }
            return liste;
        }

        public async Task<RandevuModel?> RandevuGetir(int randevuId)
        {
            const string query = @"
                SELECT r.RandevuId, r.OgretmenKullaniciId, r.VeliKullaniciId, r.OgrenciId,
                       r.RandevuTarihi, r.SureDakika, r.Durum, r.[Not], r.OgretmenTarafindanOlusturuldu,
                       r.OlusturulmaTarihi,
                       og.KullaniciAdi AS OgretmenAd,
                       v.KullaniciAdi AS VeliAd,
                       o.OgrenciAdSoyad
                FROM Randevular r
                INNER JOIN Kullanicilar      og ON r.OgretmenKullaniciId = og.KullaniciId
                INNER JOIN OgretmenProfiller op ON op.KullaniciId = og.KullaniciId
                INNER JOIN Kullanicilar      v  ON r.VeliKullaniciId = v.KullaniciId
                INNER JOIN VeliProfiller     vp ON vp.KullaniciId = v.KullaniciId
                LEFT  JOIN Ogrenciler        o  ON r.OgrenciId = o.OgrenciId
                WHERE r.RandevuId = @id AND r.IsDeleted = 0
                  AND og.IsDeleted = 0 AND op.IsDeleted = 0
                  AND v.IsDeleted = 0 AND vp.IsDeleted = 0";

            await using var conn = new SqlConnection(ConnectionString);
            await using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", randevuId);
            await conn.OpenAsync();

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;

            var durum = reader.GetInt32(reader.GetOrdinal("Durum"));
            return new RandevuModel
            {
                RandevuId = reader.GetInt32(reader.GetOrdinal("RandevuId")),
                OgretmenKullaniciId = reader.GetInt32(reader.GetOrdinal("OgretmenKullaniciId")),
                OgretmenAdSoyad = reader["OgretmenAd"]?.ToString() ?? "",
                VeliKullaniciId = reader.GetInt32(reader.GetOrdinal("VeliKullaniciId")),
                VeliAdSoyad = reader["VeliAd"]?.ToString() ?? "",
                OgrenciId = reader.IsDBNull(reader.GetOrdinal("OgrenciId")) ? null : reader.GetInt32(reader.GetOrdinal("OgrenciId")),
                OgrenciAdSoyad = reader["OgrenciAdSoyad"]?.ToString(),
                RandevuTarihi = reader.GetDateTime(reader.GetOrdinal("RandevuTarihi")),
                SureDakika = reader.GetInt32(reader.GetOrdinal("SureDakika")),
                Durum = durum,
                DurumAdi = _durumAdlari.GetValueOrDefault(durum, "Bilinmiyor"),
                Not = reader["Not"]?.ToString(),
                OgretmenTarafindanOlusturuldu = reader.GetBoolean(reader.GetOrdinal("OgretmenTarafindanOlusturuldu")),
                OlusturulmaTarihi = reader.GetDateTime(reader.GetOrdinal("OlusturulmaTarihi"))
            };
        }

        public async Task<int> OgretmenRandevuOlustur(int ogretmenId, int veliId, int? ogrenciId, DateTime tarih, int sureDakika, string? not)
        {
            var aktifMesaj = await AktiflikKontrolu(ogretmenId, veliId);
            if (aktifMesaj is not null) throw new InvalidOperationException(aktifMesaj);

            var cakisma = await CakismaMesaji(ogretmenId, veliId, tarih, sureDakika);
            if (cakisma is not null) throw new InvalidOperationException(cakisma);

            return await RandevuEkle(ogretmenId, veliId, ogrenciId, tarih, sureDakika, not,
                ogretmenTarafindanOlusturuldu: true, durum: (int)RandevuDurumu.Beklemede);
        }

        public async Task<int> VeliRandevuOlustur(int veliId, int ogretmenId, int? ogrenciId, DateTime tarih, int sureDakika, string? not)
        {
            var aktifMesaj = await AktiflikKontrolu(ogretmenId, veliId);
            if (aktifMesaj is not null) throw new InvalidOperationException(aktifMesaj);

            var cakisma = await CakismaMesaji(ogretmenId, veliId, tarih, sureDakika);
            if (cakisma is not null) throw new InvalidOperationException(cakisma);

            return await RandevuEkle(ogretmenId, veliId, ogrenciId, tarih, sureDakika, not,
                ogretmenTarafindanOlusturuldu: false, durum: (int)RandevuDurumu.Beklemede);
        }

        // Hem öğretmen hem veli (Kullanicilar + rol profili IsDeleted=0) aktif mi?
        // Aktif değilse Türkçe açıklama döner; her ikisi de aktifse null.
        private async Task<string?> AktiflikKontrolu(int ogretmenId, int veliId)
        {
            const string query = @"
                SELECT
                  (SELECT COUNT(*) FROM Kullanicilar k
                    INNER JOIN OgretmenProfiller op ON op.KullaniciId = k.KullaniciId
                    WHERE k.KullaniciId = @ogretmenId
                      AND k.Rol = 2 AND k.IsDeleted = 0 AND op.IsDeleted = 0) AS OgretmenAktif,
                  (SELECT COUNT(*) FROM Kullanicilar k
                    INNER JOIN VeliProfiller vp ON vp.KullaniciId = k.KullaniciId
                    WHERE k.KullaniciId = @veliId
                      AND k.Rol = 4 AND k.IsDeleted = 0 AND vp.IsDeleted = 0)     AS VeliAktif";

            await using var conn = new SqlConnection(ConnectionString);
            await using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@ogretmenId", ogretmenId);
            cmd.Parameters.AddWithValue("@veliId", veliId);
            await conn.OpenAsync();

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return "Kullanıcı durumu doğrulanamadı.";

            var ogretmenAktif = reader.GetInt32(0) > 0;
            var veliAktif = reader.GetInt32(1) > 0;

            if (!ogretmenAktif) return "Seçilen öğretmen şu anda aktif değil.";
            if (!veliAktif)     return "Seçilen veli şu anda aktif değil.";
            return null;
        }

        // Hem öğretmen hem veli takviminde aynı zaman aralığında onaylanmış/bekleyen randevu varsa
        // çakışan tarafın Türkçe mesajını döndürür; çakışma yoksa null döner.
        private async Task<string?> CakismaMesaji(int ogretmenId, int veliId, DateTime tarih, int sureDakika)
        {
            const string query = @"
                SELECT TOP 1
                  CASE WHEN OgretmenKullaniciId = @ogretmenId THEN 'ogretmen'
                       WHEN VeliKullaniciId    = @veliId    THEN 'veli'
                  END AS Taraf
                FROM Randevular
                WHERE IsDeleted = 0 AND Durum IN (0, 1)
                  AND (OgretmenKullaniciId = @ogretmenId OR VeliKullaniciId = @veliId)
                  AND @tarih < DATEADD(MINUTE, SureDakika, RandevuTarihi)
                  AND DATEADD(MINUTE, @sure, @tarih) > RandevuTarihi";

            await using var conn = new SqlConnection(ConnectionString);
            await using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@ogretmenId", ogretmenId);
            cmd.Parameters.AddWithValue("@veliId", veliId);
            cmd.Parameters.AddWithValue("@tarih", tarih);
            cmd.Parameters.AddWithValue("@sure", sureDakika);
            await conn.OpenAsync();
            var taraf = (string?)await cmd.ExecuteScalarAsync();
            return taraf switch
            {
                "ogretmen" => "Öğretmenin bu zaman aralığında zaten bir randevusu bulunmaktadır.",
                "veli"     => "Velinin bu zaman aralığında zaten bir randevusu bulunmaktadır.",
                _ => null
            };
        }

        // Mobil ve diğer servislerin POST öncesi kontrol için kullanabileceği public sarmalayıcı.
        public Task<string?> CakismaMesajiAl(int ogretmenId, int veliId, DateTime tarih, int sureDakika)
            => CakismaMesaji(ogretmenId, veliId, tarih, sureDakika);

        // Karşı tarafın oluşturduğu beklemedeki randevuyu onaylar/reddeder.
        // Öğretmen yalnızca velinin oluşturduğu (OgretmenTarafindanOlusturuldu = 0),
        // veli yalnızca öğretmenin oluşturduğu (OgretmenTarafindanOlusturuldu = 1) randevuya işlem yapabilir.
        public async Task<bool> DurumGuncelle(int randevuId, int kullaniciId, string rol, RandevuDurumu yeniDurum)
        {
            await TaraflarAktifMiKontrolEt(randevuId);

            const string query = @"
                UPDATE Randevular SET Durum = @durum, GuncellenmeTarihi = GETDATE()
                WHERE RandevuId = @id AND IsDeleted = 0
                  AND ((@rol = 'Ogretmen' AND OgretmenKullaniciId = @kullaniciId AND OgretmenTarafindanOlusturuldu = 0)
                    OR (@rol = 'Veli' AND VeliKullaniciId = @kullaniciId AND OgretmenTarafindanOlusturuldu = 1))";

            await using var conn = new SqlConnection(ConnectionString);
            await using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", randevuId);
            cmd.Parameters.AddWithValue("@kullaniciId", kullaniciId);
            cmd.Parameters.AddWithValue("@rol", rol);
            cmd.Parameters.AddWithValue("@durum", (int)yeniDurum);
            await conn.OpenAsync();
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> IptalEt(int randevuId, int kullaniciId)
        {
            await TaraflarAktifMiKontrolEt(randevuId);

            const string query = @"
                UPDATE Randevular SET Durum = @durum, GuncellenmeTarihi = GETDATE()
                WHERE RandevuId = @id AND IsDeleted = 0
                  AND (OgretmenKullaniciId = @kullaniciId OR VeliKullaniciId = @kullaniciId)";

            await using var conn = new SqlConnection(ConnectionString);
            await using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", randevuId);
            cmd.Parameters.AddWithValue("@kullaniciId", kullaniciId);
            cmd.Parameters.AddWithValue("@durum", (int)RandevuDurumu.IptalEdildi);
            await conn.OpenAsync();
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        // Mevcut bir randevudaki öğretmen veya veli pasifleştirilmişse InvalidOperationException fırlatır.
        // Randevu bulunamazsa sessiz geçer (çağıran metot zaten 404 dönecektir).
        private async Task TaraflarAktifMiKontrolEt(int randevuId)
        {
            const string query = @"
                SELECT r.OgretmenKullaniciId, r.VeliKullaniciId
                FROM Randevular r
                WHERE r.RandevuId = @id AND r.IsDeleted = 0";

            int ogretmenId, veliId;
            await using (var conn = new SqlConnection(ConnectionString))
            await using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@id", randevuId);
                await conn.OpenAsync();
                await using var reader = await cmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync()) return;
                ogretmenId = reader.GetInt32(0);
                veliId = reader.GetInt32(1);
            }

            var aktifMesaj = await AktiflikKontrolu(ogretmenId, veliId);
            if (aktifMesaj is not null) throw new InvalidOperationException(aktifMesaj);
        }

        private async Task<int> RandevuEkle(int ogretmenId, int veliId, int? ogrenciId, DateTime tarih, int sureDakika, string? not, bool ogretmenTarafindanOlusturuldu, int durum)
        {
            const string query = @"
                INSERT INTO Randevular
                    (OgretmenKullaniciId, VeliKullaniciId, OgrenciId, RandevuTarihi, SureDakika,
                     Durum, [Not], OgretmenTarafindanOlusturuldu, OlusturulmaTarihi, IsDeleted)
                OUTPUT INSERTED.RandevuId
                VALUES
                    (@ogretmenId, @veliId, @ogrenciId, @tarih, @sure,
                     @durum, @not, @olusturan, GETDATE(), 0)";

            await using var conn = new SqlConnection(ConnectionString);
            await using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@ogretmenId", ogretmenId);
            cmd.Parameters.AddWithValue("@veliId", veliId);
            cmd.Parameters.AddWithValue("@ogrenciId", (object?)ogrenciId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@tarih", tarih);
            cmd.Parameters.AddWithValue("@sure", sureDakika);
            cmd.Parameters.AddWithValue("@durum", durum);
            cmd.Parameters.AddWithValue("@not", (object?)not ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@olusturan", ogretmenTarafindanOlusturuldu);
            await conn.OpenAsync();
            return (int)(await cmd.ExecuteScalarAsync())!;
        }
    }
}
