using System.Collections.Concurrent;

namespace OgrenciBilgiSistemi.Shared.Services
{
    /// <summary>
    /// Kaba kuvvet koruması: aynı hesaba (okul + kullanıcı adı) art arda hatalı girişler kilitlenir.
    /// Yalnızca hatalı denemeleri sayar; aynı ağdan (ör. okul Wi-Fi) giriş yapan çok sayıda kullanıcıyı etkilemez.
    /// Bellek içidir (tek süreç); uygulama yeniden başlayınca sıfırlanır. Singleton olarak kayıt edilmelidir.
    /// </summary>
    public sealed class GirisDenemeSayaci
    {
        public const int MaksHataliDeneme = 5;
        public static readonly TimeSpan Pencere = TimeSpan.FromMinutes(15);
        public static readonly TimeSpan KilitSuresi = TimeSpan.FromMinutes(15);
        private const int TemizlikEsigi = 10_000;

        private readonly ConcurrentDictionary<string, Kayit> _kayitlar = new();
        private readonly TimeProvider _saat;

        public GirisDenemeSayaci(TimeProvider saat) => _saat = saat;

        private sealed record Kayit(int Sayi, DateTimeOffset IlkHata, DateTimeOffset? KilitBitis);

        public static string Anahtar(string? okulKodu, string kullaniciAdi)
            => $"{(okulKodu ?? "*").Trim().ToUpperInvariant()}|{kullaniciAdi.Trim().ToLowerInvariant()}";

        /// <summary>Hesap kilitliyse kalan süreyi döndürür.</summary>
        public bool KilitliMi(string anahtar, out TimeSpan kalan)
        {
            kalan = TimeSpan.Zero;
            if (!_kayitlar.TryGetValue(anahtar, out var k) || k.KilitBitis is not { } bitis)
                return false;

            var simdi = _saat.GetUtcNow();
            if (simdi >= bitis)
            {
                _kayitlar.TryRemove(anahtar, out _);
                return false;
            }

            kalan = bitis - simdi;
            return true;
        }

        public void HataliKaydet(string anahtar)
        {
            var simdi = _saat.GetUtcNow();
            _kayitlar.AddOrUpdate(anahtar,
                _ => new Kayit(1, simdi, null),
                (_, k) =>
                {
                    // Pencere dolduysa veya kilit bittiyse sayım baştan başlar
                    if (simdi - k.IlkHata > Pencere || (k.KilitBitis is { } b && simdi >= b))
                        return new Kayit(1, simdi, null);

                    var sayi = k.Sayi + 1;
                    return new Kayit(sayi, k.IlkHata, sayi >= MaksHataliDeneme ? simdi + KilitSuresi : null);
                });

            if (_kayitlar.Count > TemizlikEsigi)
                SuresiDolanlariTemizle(simdi);
        }

        public void Sifirla(string anahtar) => _kayitlar.TryRemove(anahtar, out _);

        private void SuresiDolanlariTemizle(DateTimeOffset simdi)
        {
            foreach (var (anahtar, k) in _kayitlar)
            {
                var bitti = k.KilitBitis is { } b ? simdi >= b : simdi - k.IlkHata > Pencere;
                if (bitti) _kayitlar.TryRemove(anahtar, out _);
            }
        }

        public static string KilitMesaji(TimeSpan kalan)
            => $"Çok fazla hatalı giriş denemesi. {Math.Max(1, (int)Math.Ceiling(kalan.TotalMinutes))} dakika sonra tekrar deneyin.";
    }
}
