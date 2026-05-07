namespace OgrenciBilgiSistemi.Mobil.Services
{
    /// <summary>
    /// GirisView'dan OkulSecimView'a GenelAdmin şifresini geçici taşımak için tek seferlik
    /// memory-only kanal. SecureStorage'a yazılmaz; uygulama kapanırsa kaybolur.
    /// </summary>
    public sealed class GenelAdminGirisGecisService
    {
        private string? _sifre;

        public void Ayarla(string sifre) => _sifre = sifre;

        /// <summary>Şifreyi okur ve hafızadan siler.</summary>
        public string? TukutVeAl()
        {
            var s = _sifre;
            _sifre = null;
            return s;
        }

        public void Temizle() => _sifre = null;

        public bool HazirMi => !string.IsNullOrEmpty(_sifre);
    }
}
