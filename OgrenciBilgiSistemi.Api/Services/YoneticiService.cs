using Microsoft.Data.SqlClient;
using OgrenciBilgiSistemi.Api.Models;
using OgrenciBilgiSistemi.Shared.Enums;
using OgrenciBilgiSistemi.Shared.Services;

namespace OgrenciBilgiSistemi.Api.Services
{
    public class YoneticiService
    {
        private readonly TenantBaglami _tenantBaglami;

        public YoneticiService(TenantBaglami tenantBaglami)
        {
            _tenantBaglami = tenantBaglami;
        }

        private string ConnectionString => _tenantBaglami.ConnectionString;

        /// <summary>
        /// Okulun toplam sayım ve bugünkü geçiş özetini tek bağlantıda hesaplar.
        /// HomeController.DashboardStats (MVC) ile aynı mantığı ADO.NET ile uygular.
        /// </summary>
        public async Task<OkulOzetModel> OkulOzetGetirAsync()
        {
            var bugun = DateTime.Today;
            var yarin = bugun.AddDays(1);

            // Sayım sorguları MVC tarafıyla uyumlu kriterler kullanır:
            // - Öğrenci: OgrenciDurum=1 (HomeController.DashboardStats ve OgrenciService liste filtresi)
            // - Öğretmen: OgretmenProfiller.OgretmenDurum=1 (OgretmenProfilService default Aktif filtresi)
            // - Veli: VeliProfiller.VeliDurum=1 (her iki taraf aktif velileri sayar)
            const string query = @"
                SELECT
                    (SELECT COUNT(*) FROM Ogrenciler
                        WHERE OgrenciDurum = 1) AS ToplamOgrenci,

                    (SELECT COUNT(*) FROM OgretmenProfiller
                        WHERE OgretmenDurum = 1) AS ToplamOgretmen,

                    (SELECT COUNT(*) FROM Birimler
                        WHERE BirimSinifMi = 1 AND BirimDurum = 1) AS ToplamSinif,

                    (SELECT COUNT(*) FROM VeliProfiller
                        WHERE VeliDurum = 1) AS ToplamVeli,

                    (SELECT COUNT(*) FROM ServisProfiller
                        WHERE ServisDurum = 1) AS ToplamServis,

                    (SELECT COUNT(*) FROM OgrenciDetaylar d
                        INNER JOIN Ogrenciler o ON o.OgrenciId = d.OgrenciId
                        WHERE d.IstasyonTipi = @yemekhaneTipi
                          AND d.OgrenciGecisTipi = N'GİRİŞ'
                          AND d.OgrenciGTarih >= @bugun
                          AND d.OgrenciGTarih < @yarin
                          AND o.OgrenciDurum = 1) AS BugunYemekhaneGiris,

                    (SELECT COUNT(*) FROM OgrenciDetaylar d
                        INNER JOIN Ogrenciler o ON o.OgrenciId = d.OgrenciId
                        WHERE d.IstasyonTipi = @anaKapiTipi
                          AND d.OgrenciGecisTipi = N'ÇIKIŞ'
                          AND d.OgrenciCTarih >= @bugun
                          AND d.OgrenciCTarih < @yarin
                          AND o.OgrenciDurum = 1) AS BugunAnakapiCikis;";

            var ozet = new OkulOzetModel();

            try
            {
                await using var conn = new SqlConnection(ConnectionString);
                await using var cmd = new SqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@yemekhaneTipi", (short)IstasyonTipi.Yemekhane);
                cmd.Parameters.AddWithValue("@anaKapiTipi", (short)IstasyonTipi.AnaKapi);
                cmd.Parameters.AddWithValue("@bugun", bugun);
                cmd.Parameters.AddWithValue("@yarin", yarin);

                await conn.OpenAsync();
                await using var reader = await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    ozet.ToplamOgrenci = Convert.ToInt32(reader["ToplamOgrenci"]);
                    ozet.ToplamOgretmen = Convert.ToInt32(reader["ToplamOgretmen"]);
                    ozet.ToplamSinif = Convert.ToInt32(reader["ToplamSinif"]);
                    ozet.ToplamVeli = Convert.ToInt32(reader["ToplamVeli"]);
                    ozet.ToplamServis = Convert.ToInt32(reader["ToplamServis"]);
                    ozet.BugunYemekhaneGiris = Convert.ToInt32(reader["BugunYemekhaneGiris"]);
                    ozet.BugunAnakapiCikis = Convert.ToInt32(reader["BugunAnakapiCikis"]);
                }
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException("Okul özeti alınamadı.", ex);
            }

            return ozet;
        }

        /// <summary>
        /// Tüm servis kullanıcılarını ve her birinin aktif öğrenci sayısını döner.
        /// Admin ana ekranındaki servis listesi için kullanılır.
        /// </summary>
        public async Task<List<ServisListeOgesiModel>> TumServisleriGetirAsync()
        {
            const string query = @"
                SELECT k.KullaniciId,
                       k.KullaniciAdi,
                       sp.Plaka,
                       sp.ServisTelefon,
                       sp.ServisDurum,
                       (SELECT COUNT(*) FROM Ogrenciler o
                          WHERE o.ServisId = k.KullaniciId
                            AND o.OgrenciDurum = 1) AS OgrenciSayisi
                FROM Kullanicilar k
                INNER JOIN ServisProfiller sp ON sp.KullaniciId = k.KullaniciId
                WHERE sp.ServisDurum = 1
                ORDER BY k.KullaniciAdi;";

            var liste = new List<ServisListeOgesiModel>();

            try
            {
                await using var conn = new SqlConnection(ConnectionString);
                await using var cmd = new SqlCommand(query, conn);

                await conn.OpenAsync();
                await using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    liste.Add(new ServisListeOgesiModel
                    {
                        KullaniciId = Convert.ToInt32(reader["KullaniciId"]),
                        KullaniciAdi = reader["KullaniciAdi"]?.ToString() ?? "",
                        Plaka = reader["Plaka"] is DBNull ? null : reader["Plaka"]?.ToString(),
                        ServisTelefon = reader["ServisTelefon"] is DBNull ? null : reader["ServisTelefon"]?.ToString(),
                        ServisDurum = reader["ServisDurum"] is not DBNull && Convert.ToBoolean(reader["ServisDurum"]),
                        OgrenciSayisi = Convert.ToInt32(reader["OgrenciSayisi"])
                    });
                }
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException("Servis listesi alınamadı.", ex);
            }

            return liste;
        }

        /// <summary>
        /// Belirtilen servise atanmış aktif öğrencileri döner.
        /// Admin ana ekranı → servis detayı için kullanılır.
        /// </summary>
        public async Task<List<OgrenciModel>> ServisOgrencileriGetirAsync(int servisKullaniciId)
        {
            const string query = @"
                SELECT O.OgrenciId, O.OgrenciAdSoyad, O.OgrenciNo, O.OgrenciGorsel, O.BirimId,
                       B.BirimAd AS SinifAdi
                FROM Ogrenciler O
                LEFT JOIN Birimler B ON O.BirimId = B.BirimId
                WHERE O.ServisId = @servisId AND O.OgrenciDurum = 1
                ORDER BY O.OgrenciAdSoyad";

            var ogrenciler = new List<OgrenciModel>();

            try
            {
                await using var conn = new SqlConnection(ConnectionString);
                await using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@servisId", servisKullaniciId);

                await conn.OpenAsync();
                await using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var rawFileName = reader["OgrenciGorsel"]?.ToString() ?? string.Empty;
                    ogrenciler.Add(new OgrenciModel
                    {
                        OgrenciId      = (int)reader["OgrenciId"],
                        OgrenciAdSoyad = reader["OgrenciAdSoyad"]?.ToString() ?? string.Empty,
                        OgrenciNo      = reader["OgrenciNo"] is DBNull ? 0 : Convert.ToInt32(reader["OgrenciNo"]),
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
        /// Bugün yemekhaneye giriş yapan aktif öğrencileri (en yenisi en üstte) döner.
        /// Saat alanı 'HH:mm' formatlanmış string olarak gelir.
        /// </summary>
        public async Task<List<YemekhaneBugunOgesiModel>> BugunYemekhaneGirislerinAsync()
        {
            var bugun = DateTime.Today;
            var yarin = bugun.AddDays(1);

            const string query = @"
                SELECT o.OgrenciId,
                       o.OgrenciAdSoyad,
                       o.OgrenciNo,
                       b.BirimAd AS SinifAdi,
                       d.OgrenciGTarih
                FROM OgrenciDetaylar d
                INNER JOIN Ogrenciler o ON o.OgrenciId = d.OgrenciId
                LEFT  JOIN Birimler  b ON b.BirimId   = o.BirimId
                WHERE d.IstasyonTipi      = @yemekhaneTipi
                  AND d.OgrenciGecisTipi  = N'GİRİŞ'
                  AND d.OgrenciGTarih    >= @bugun
                  AND d.OgrenciGTarih    <  @yarin
                  AND o.OgrenciDurum      = 1
                ORDER BY d.OgrenciGTarih DESC;";

            var liste = new List<YemekhaneBugunOgesiModel>();

            try
            {
                await using var conn = new SqlConnection(ConnectionString);
                await using var cmd = new SqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@yemekhaneTipi", (short)IstasyonTipi.Yemekhane);
                cmd.Parameters.AddWithValue("@bugun", bugun);
                cmd.Parameters.AddWithValue("@yarin", yarin);

                await conn.OpenAsync();
                await using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var girisTarihi = reader["OgrenciGTarih"] is DateTime dt ? dt : DateTime.MinValue;

                    liste.Add(new YemekhaneBugunOgesiModel
                    {
                        OgrenciId = Convert.ToInt32(reader["OgrenciId"]),
                        OgrenciAdSoyad = reader["OgrenciAdSoyad"]?.ToString() ?? "",
                        OgrenciNo = Convert.ToInt32(reader["OgrenciNo"]),
                        SinifAdi = reader["SinifAdi"] is DBNull ? null : reader["SinifAdi"]?.ToString(),
                        GirisSaati = girisTarihi == DateTime.MinValue ? "" : girisTarihi.ToString("HH:mm")
                    });
                }
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException("Bugünkü yemekhane girişleri alınamadı.", ex);
            }

            return liste;
        }

        /// <summary>
        /// Bugün ana kapıdan çıkış yapan aktif öğrencileri (en yenisi en üstte) döner.
        /// Saat alanı 'HH:mm' formatlanmış string olarak gelir.
        /// </summary>
        public async Task<List<AnakapiCikisBugunOgesiModel>> BugunAnakapiCikislariAsync()
        {
            var bugun = DateTime.Today;
            var yarin = bugun.AddDays(1);

            const string query = @"
                SELECT o.OgrenciId,
                       o.OgrenciAdSoyad,
                       o.OgrenciNo,
                       b.BirimAd AS SinifAdi,
                       d.OgrenciCTarih
                FROM OgrenciDetaylar d
                INNER JOIN Ogrenciler o ON o.OgrenciId = d.OgrenciId
                LEFT  JOIN Birimler  b ON b.BirimId   = o.BirimId
                WHERE d.IstasyonTipi      = @anaKapiTipi
                  AND d.OgrenciGecisTipi  = N'ÇIKIŞ'
                  AND d.OgrenciCTarih    >= @bugun
                  AND d.OgrenciCTarih    <  @yarin
                  AND o.OgrenciDurum      = 1
                ORDER BY d.OgrenciCTarih DESC;";

            var liste = new List<AnakapiCikisBugunOgesiModel>();

            try
            {
                await using var conn = new SqlConnection(ConnectionString);
                await using var cmd = new SqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@anaKapiTipi", (short)IstasyonTipi.AnaKapi);
                cmd.Parameters.AddWithValue("@bugun", bugun);
                cmd.Parameters.AddWithValue("@yarin", yarin);

                await conn.OpenAsync();
                await using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var cikisTarihi = reader["OgrenciCTarih"] is DateTime dt ? dt : DateTime.MinValue;

                    liste.Add(new AnakapiCikisBugunOgesiModel
                    {
                        OgrenciId = Convert.ToInt32(reader["OgrenciId"]),
                        OgrenciAdSoyad = reader["OgrenciAdSoyad"]?.ToString() ?? "",
                        OgrenciNo = Convert.ToInt32(reader["OgrenciNo"]),
                        SinifAdi = reader["SinifAdi"] is DBNull ? null : reader["SinifAdi"]?.ToString(),
                        CikisSaati = cikisTarihi == DateTime.MinValue ? "" : cikisTarihi.ToString("HH:mm")
                    });
                }
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException("Bugünkü ana kapı çıkışları alınamadı.", ex);
            }

            return liste;
        }
    }
}
