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

            const string query = @"
                SELECT
                    (SELECT COUNT(*) FROM Ogrenciler
                        WHERE OgrenciDurum = 1) AS ToplamOgrenci,

                    (SELECT COUNT(*) FROM Kullanicilar
                        WHERE Rol = @ogretmenRol AND KullaniciDurum = 1) AS ToplamOgretmen,

                    (SELECT COUNT(*) FROM Birimler
                        WHERE BirimSinifMi = 1 AND BirimDurum = 1) AS ToplamSinif,

                    (SELECT COUNT(*) FROM Kullanicilar
                        WHERE Rol = @veliRol AND KullaniciDurum = 1) AS ToplamVeli,

                    (SELECT COUNT(*) FROM OgrenciDetaylar
                        WHERE IstasyonTipi = @yemekhaneTipi
                          AND OgrenciGecisTipi = N'GİRİŞ'
                          AND OgrenciGTarih IS NOT NULL
                          AND OgrenciGTarih >= @bugun
                          AND OgrenciGTarih < @yarin) AS BugunYemekhaneGiris,

                    (SELECT COUNT(*) FROM OgrenciDetaylar
                        WHERE IstasyonTipi = @anaKapiTipi
                          AND OgrenciGecisTipi = N'ÇIKIŞ'
                          AND OgrenciCTarih IS NOT NULL
                          AND OgrenciCTarih >= @bugun
                          AND OgrenciCTarih < @yarin) AS BugunAnakapiCikis;";

            var ozet = new OkulOzetModel();

            try
            {
                await using var conn = new SqlConnection(ConnectionString);
                await using var cmd = new SqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@ogretmenRol", (int)KullaniciRolu.Ogretmen);
                cmd.Parameters.AddWithValue("@veliRol", (int)KullaniciRolu.Veli);
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
    }
}
