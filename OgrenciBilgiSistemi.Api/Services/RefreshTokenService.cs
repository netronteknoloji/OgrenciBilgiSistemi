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

        // Refresh token geçerlilik süresi (8 saat)
        private static readonly TimeSpan TokenSuresi = TimeSpan.FromHours(8);

        /// <summary>
        /// Kullanıcı için yeni bir refresh token üretir ve depoya kaydeder.
        /// Aynı kullanıcının önceki refresh token'ı varsa geçersiz kılınır.
        /// </summary>
        public string TokenOlustur(int kullaniciId)
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
                SonKullanim = DateTime.UtcNow.Add(TokenSuresi)
            };

            return token;
        }

        /// <summary>
        /// Refresh token'ı doğrular. Geçerliyse kullanıcı ID döner, değilse null.
        /// Doğrulanan token tüketilir (tek kullanımlık).
        /// </summary>
        public int? TokenDogrula(string refreshToken)
        {
            if (!_tokenlar.TryRemove(refreshToken, out var bilgi))
                return null;

            if (DateTime.UtcNow > bilgi.SonKullanim)
                return null;

            return bilgi.KullaniciId;
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
            public DateTime SonKullanim { get; set; }
        }
    }
}
