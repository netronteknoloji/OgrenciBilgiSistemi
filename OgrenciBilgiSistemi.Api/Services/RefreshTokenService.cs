using System.Collections.Concurrent;

namespace OgrenciBilgiSistemi.Api.Services
{
    /// <summary>
    /// Bellek içi refresh token deposu.
    /// Sunucu yeniden başlatıldığında tokenlar silinir — kullanıcı tekrar giriş yapar.
    /// </summary>
    public class RefreshTokenService
    {
        private readonly ConcurrentDictionary<string, RefreshTokenBilgi> _tokenlar = new();
        private readonly TimeProvider _timeProvider;

        // Refresh token geçerlilik süresi (8 saat)
        private static readonly TimeSpan TokenSuresi = TimeSpan.FromHours(8);

        public RefreshTokenService(TimeProvider timeProvider)
        {
            _timeProvider = timeProvider;
        }

        /// <summary>
        /// Kullanıcı için yeni bir refresh token üretir ve depoya kaydeder.
        /// Aynı kullanıcının önceki refresh token'ı varsa geçersiz kılınır.
        /// </summary>
        public string TokenOlustur(int kullaniciId, string okulKodu)
        {
            // Eski tokenları temizle
            var eskiTokenlar = _tokenlar
                .Where(kvp => kvp.Value.KullaniciId == kullaniciId)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var eski in eskiTokenlar)
                _tokenlar.TryRemove(eski, out _);

            var token = Guid.NewGuid().ToString("N");
            _tokenlar[token] = new RefreshTokenBilgi
            {
                KullaniciId = kullaniciId,
                OkulKodu = okulKodu,
                SonKullanim = _timeProvider.GetUtcNow().UtcDateTime.Add(TokenSuresi)
            };

            return token;
        }

        /// <summary>
        /// Refresh token'ı doğrular. Geçerliyse (kullanıcıId, okulKodu) döner, değilse null.
        /// Doğrulanan token tüketilir (tek kullanımlık).
        /// </summary>
        public (int KullaniciId, string OkulKodu)? TokenDogrula(string refreshToken)
        {
            if (!_tokenlar.TryRemove(refreshToken, out var bilgi))
                return null;

            if (_timeProvider.GetUtcNow().UtcDateTime > bilgi.SonKullanim)
                return null;

            return (bilgi.KullaniciId, bilgi.OkulKodu);
        }

        /// <summary>
        /// Kullanıcının tüm refresh tokenlarını geçersiz kılar (çıkış yapma vb.).
        /// </summary>
        public void KullaniciTokenlariniSil(int kullaniciId)
        {
            var tokenlar = _tokenlar
                .Where(kvp => kvp.Value.KullaniciId == kullaniciId)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var token in tokenlar)
                _tokenlar.TryRemove(token, out _);
        }

        private class RefreshTokenBilgi
        {
            public int KullaniciId { get; set; }
            public string OkulKodu { get; set; } = string.Empty;
            public DateTime SonKullanim { get; set; }
        }
    }
}
