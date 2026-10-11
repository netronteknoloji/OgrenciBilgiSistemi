using Microsoft.Data.SqlClient;
using OgrenciBilgiSistemi.Shared.Helpers;
using OgrenciBilgiSistemi.Shared.Models;
using OgrenciBilgiSistemi.Shared.Services;

namespace OgrenciBilgiSistemi.Api.Services
{
    public sealed record RefreshTokenSonucu(int KullaniciId, Guid OturumId, OkulBilgiAyari Okul);

    /// <summary>
    /// Refresh token deposu: okulun DB'sindeki RefreshTokenlar tablosu (sunucu yeniden başlasa da oturumlar sürer).
    /// Token biçimi "{okulKodu}.{rastgele}" (bkz. RefreshTokenBicimi); DB'de yalnızca hash'i saklanır.
    /// Her giriş yeni bir oturum (OturumId) açar, kullanıcı birden çok cihazda oturum tutabilir.
    /// Token tek kullanımlıktır; yenilemede aynı oturumda yenisi üretilir. Zamanlar UTC.
    /// </summary>
    public class RefreshTokenService
    {
        // Refresh token geçerlilik süresi (8 saat, kayan değil: her yenilemede yeniden 8 saat)
        public static readonly TimeSpan TokenSuresi = TimeSpan.FromHours(8);

        private readonly OkulYapilandirmaServisi _okulServisi;
        private readonly TimeProvider _timeProvider;

        public RefreshTokenService(OkulYapilandirmaServisi okulServisi, TimeProvider timeProvider)
        {
            _okulServisi = okulServisi;
            _timeProvider = timeProvider;
        }

        /// <summary>Yeni refresh token üretip kaydeder. <paramref name="oturumId"/> verilmezse yeni oturum açılır.</summary>
        public async Task<(string Token, Guid OturumId)> OlusturAsync(
            OkulBilgiAyari okul, int kullaniciId, Guid? oturumId = null, CancellationToken ct = default)
        {
            var token = RefreshTokenBicimi.Olustur(okul.OkulKodu);
            var oturum = oturumId ?? Guid.NewGuid();
            var simdi = _timeProvider.GetUtcNow().UtcDateTime;

            const string sql = @"
                INSERT INTO RefreshTokenlar (KullaniciId, OturumId, TokenHash, OlusturmaZamani, SonKullanim)
                VALUES (@kullaniciId, @oturumId, @hash, @simdi, @sonKullanim)";

            await using var conn = new SqlConnection(okul.ConnectionString);
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@kullaniciId", kullaniciId);
            cmd.Parameters.AddWithValue("@oturumId", oturum);
            cmd.Parameters.AddWithValue("@hash", RefreshTokenBicimi.Hash(token));
            cmd.Parameters.AddWithValue("@simdi", simdi);
            cmd.Parameters.AddWithValue("@sonKullanim", simdi + TokenSuresi);
            await conn.OpenAsync(ct);
            await cmd.ExecuteNonQueryAsync(ct);

            return (token, oturum);
        }

        /// <summary>
        /// Token'ı doğrular ve tüketir (atomik: eşzamanlı iki yenilemeden yalnızca biri kazanır).
        /// Geçersiz, süresi dolmuş, kullanılmış veya iptal edilmişse null.
        /// </summary>
        public async Task<RefreshTokenSonucu?> TuketAsync(string refreshToken, CancellationToken ct = default)
        {
            var okulKodu = RefreshTokenBicimi.OkulKoduCoz(refreshToken);
            var okul = okulKodu is null ? null : _okulServisi.OkulGetir(okulKodu);
            if (okul is null) return null;

            const string sql = @"
                UPDATE RefreshTokenlar SET IptalZamani = @simdi
                OUTPUT inserted.KullaniciId, inserted.OturumId
                WHERE TokenHash = @hash AND IptalZamani IS NULL AND SonKullanim > @simdi";

            await using var conn = new SqlConnection(okul.ConnectionString);
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@hash", RefreshTokenBicimi.Hash(refreshToken));
            cmd.Parameters.AddWithValue("@simdi", _timeProvider.GetUtcNow().UtcDateTime);
            await conn.OpenAsync(ct);

            await using var r = await cmd.ExecuteReaderAsync(ct);
            return await r.ReadAsync(ct)
                ? new RefreshTokenSonucu(r.GetInt32(0), r.GetGuid(1), okul)
                : null;
        }

        /// <summary>Tek cihazın oturumunu iptal eder (çıkış); kullanıcının diğer cihazları etkilenmez.</summary>
        public Task OturumuIptalEtAsync(OkulBilgiAyari okul, Guid oturumId, CancellationToken ct = default)
            => IptalEtAsync(okul, "OturumId = @deger", oturumId, ct);

        /// <summary>Kullanıcının tüm cihazlardaki oturumlarını iptal eder (şifre değişikliği vb.).</summary>
        public Task KullanicininTumunuIptalEtAsync(OkulBilgiAyari okul, int kullaniciId, CancellationToken ct = default)
            => IptalEtAsync(okul, "KullaniciId = @deger", kullaniciId, ct);

        private async Task IptalEtAsync(OkulBilgiAyari okul, string kosul, object deger, CancellationToken ct)
        {
            var sql = $"UPDATE RefreshTokenlar SET IptalZamani = @simdi WHERE {kosul} AND IptalZamani IS NULL";

            await using var conn = new SqlConnection(okul.ConnectionString);
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@deger", deger);
            cmd.Parameters.AddWithValue("@simdi", _timeProvider.GetUtcNow().UtcDateTime);
            await conn.OpenAsync(ct);
            await cmd.ExecuteNonQueryAsync(ct);
        }
    }
}
