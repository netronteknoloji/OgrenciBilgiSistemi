using System.Security.Cryptography;
using System.Text;

namespace OgrenciBilgiSistemi.Shared.Helpers
{
    /// <summary>
    /// Refresh token biçimi: "{okulKodu}.{32 bayt rastgele, base64url}".
    /// Refresh isteği okul kodu taşımadığı için token'ın hangi okul DB'sinde saklandığı token'dan çözülür.
    /// DB'de token değil yalnızca <see cref="Hash"/> saklanır.
    /// </summary>
    public static class RefreshTokenBicimi
    {
        private const char Ayirici = '.';

        public static string Olustur(string okulKodu)
        {
            if (string.IsNullOrWhiteSpace(okulKodu) || okulKodu.Contains(Ayirici))
                throw new ArgumentException("Geçersiz okul kodu.", nameof(okulKodu));

            var rastgele = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .TrimEnd('=').Replace('+', '-').Replace('/', '_');
            return $"{okulKodu}{Ayirici}{rastgele}";
        }

        /// <summary>Token'daki okul kodu; biçim geçersizse null.</summary>
        public static string? OkulKoduCoz(string? token)
        {
            if (string.IsNullOrWhiteSpace(token)) return null;
            var i = token.IndexOf(Ayirici);
            return i > 0 && i < token.Length - 1 ? token[..i] : null;
        }

        /// <summary>SHA-256, küçük harf hex (64 karakter).</summary>
        public static string Hash(string token)
            => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
    }
}
