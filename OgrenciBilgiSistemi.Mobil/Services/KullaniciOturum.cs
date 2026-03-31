using OgrenciBilgiSistemi.Shared.Enums;

namespace OgrenciBilgiSistemi.Mobil.Services
{
    /// <summary>
    /// Kullanıcı oturum bilgilerini SecureStorage ile güvenli şekilde yöneten sınıf.
    /// Hassas veriler (token, kullanıcı bilgileri) şifreli olarak saklanır.
    /// </summary>
    public static class KullaniciOturum
    {
        // Bellek içi cache - SecureStorage'a her erişimde I/O yapılmasını önler
        private static int _kullaniciId;
        private static string _adSoyad = "Kullanıcı";
        private static int? _birimId;
        private static int? _servisId;
        private static int? _veliId;
        private static KullaniciRolu _rol;
        private static string _yetkiToken;
        private static bool _yuklendi;

        // SecureStorage anahtarları
        private const string AnahtarKullaniciId = "session_user_id";
        private const string AnahtarAdSoyad = "session_full_name";
        private const string AnahtarBirimId = "session_unit_id";
        private const string AnahtarServisId = "session_service_id";
        private const string AnahtarVeliId = "session_veli_id";
        private const string AnahtarRol = "session_rol";
        private const string AnahtarYetkiToken = "session_auth_token";
        private const string AnahtarGirisZamani = "session_login_time";
        private const string AnahtarRefreshToken = "session_refresh_token";

        // Oturum zaman aşımı (8 saat)
        private static readonly TimeSpan OturumSuresi = TimeSpan.FromHours(8);

        private static string _refreshToken;

        /// <summary>
        /// Giriş başarılı olduğunda tüm oturum bilgilerini SecureStorage'a kaydeder.
        /// </summary>
        public static async Task OturumAyarlaAsync(int kullaniciId, string adSoyad, int? birimId, KullaniciRolu rol = KullaniciRolu.Ogretmen, int? servisId = null, int? veliId = null, string yetkiToken = null, string refreshToken = null)
        {
            _kullaniciId = kullaniciId;
            _adSoyad = string.IsNullOrEmpty(adSoyad) ? "Kullanıcı" : adSoyad;
            _birimId = birimId;
            _rol = rol;
            _servisId = servisId;
            _veliId = veliId;
            _yetkiToken = yetkiToken;
            _refreshToken = refreshToken;
            _yuklendi = true;

            try
            {
                await SecureStorage.Default.SetAsync(AnahtarKullaniciId, kullaniciId.ToString());
                await SecureStorage.Default.SetAsync(AnahtarAdSoyad, _adSoyad);
                await SecureStorage.Default.SetAsync(AnahtarRol, ((int)rol).ToString());
                await SecureStorage.Default.SetAsync(AnahtarGirisZamani, DateTime.UtcNow.ToString("O"));

                if (birimId.HasValue)
                    await SecureStorage.Default.SetAsync(AnahtarBirimId, birimId.Value.ToString());

                if (servisId.HasValue)
                    await SecureStorage.Default.SetAsync(AnahtarServisId, servisId.Value.ToString());

                if (veliId.HasValue)
                    await SecureStorage.Default.SetAsync(AnahtarVeliId, veliId.Value.ToString());

                if (!string.IsNullOrEmpty(yetkiToken))
                    await SecureStorage.Default.SetAsync(AnahtarYetkiToken, yetkiToken);

                if (!string.IsNullOrEmpty(refreshToken))
                    await SecureStorage.Default.SetAsync(AnahtarRefreshToken, refreshToken);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SecureStorage HATASI]: {ex.Message}");
            }
        }

        /// <summary>
        /// Uygulama yeniden başlatıldığında oturum bilgilerini SecureStorage'dan yükler.
        /// </summary>
        public static async Task OturumYukleAsync()
        {
            if (_yuklendi) return;

            try
            {
                var girisZamaniStr = await SecureStorage.Default.GetAsync(AnahtarGirisZamani);
                if (!string.IsNullOrEmpty(girisZamaniStr) && DateTime.TryParse(girisZamaniStr, out var girisZamani))
                {
                    if (DateTime.UtcNow - girisZamani > OturumSuresi)
                    {
                        await OturumTemizleAsync();
                        return;
                    }
                }

                var kullaniciIdStr = await SecureStorage.Default.GetAsync(AnahtarKullaniciId);
                if (!string.IsNullOrEmpty(kullaniciIdStr) && int.TryParse(kullaniciIdStr, out var uid))
                    _kullaniciId = uid;

                _adSoyad = await SecureStorage.Default.GetAsync(AnahtarAdSoyad) ?? "Kullanıcı";

                var birimIdStr = await SecureStorage.Default.GetAsync(AnahtarBirimId);
                if (!string.IsNullOrEmpty(birimIdStr) && int.TryParse(birimIdStr, out var parsedBirimId))
                    _birimId = parsedBirimId;

                var servisIdStr = await SecureStorage.Default.GetAsync(AnahtarServisId);
                if (!string.IsNullOrEmpty(servisIdStr) && int.TryParse(servisIdStr, out var sid))
                    _servisId = sid;

                var veliIdStr = await SecureStorage.Default.GetAsync(AnahtarVeliId);
                if (!string.IsNullOrEmpty(veliIdStr) && int.TryParse(veliIdStr, out var vid))
                    _veliId = vid;

                var rolStr = await SecureStorage.Default.GetAsync(AnahtarRol);
                if (!string.IsNullOrEmpty(rolStr) && int.TryParse(rolStr, out var parsedRol))
                    _rol = (KullaniciRolu)parsedRol;

                _yetkiToken = await SecureStorage.Default.GetAsync(AnahtarYetkiToken);
                _refreshToken = await SecureStorage.Default.GetAsync(AnahtarRefreshToken);

                _yuklendi = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SecureStorage YÜKLEME HATASI]: {ex.Message}");
            }
        }

        /// <summary>
        /// Tüm oturum bilgilerini bellekten ve SecureStorage'dan temizler.
        /// Sunucu tarafında refresh token'ı geçersiz kılmak için logout API'sini çağırır.
        /// </summary>
        public static async Task OturumTemizleAsync()
        {
            // Sunucudaki refresh token'ı geçersiz kıl
            await LogoutApiCagirAsync();

            _kullaniciId = 0;
            _adSoyad = "Kullanıcı";
            _birimId = null;
            _servisId = null;
            _veliId = null;
            _rol = 0;
            _yetkiToken = null;
            _refreshToken = null;
            _yuklendi = false;

            try
            {
                SecureStorage.Default.Remove(AnahtarKullaniciId);
                SecureStorage.Default.Remove(AnahtarAdSoyad);
                SecureStorage.Default.Remove(AnahtarBirimId);
                SecureStorage.Default.Remove(AnahtarServisId);
                SecureStorage.Default.Remove(AnahtarVeliId);
                SecureStorage.Default.Remove(AnahtarRol);
                SecureStorage.Default.Remove(AnahtarYetkiToken);
                SecureStorage.Default.Remove(AnahtarRefreshToken);
                SecureStorage.Default.Remove(AnahtarGirisZamani);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SecureStorage TEMİZLEME HATASI]: {ex.Message}");
            }
        }

        /// <summary>
        /// Sunucudaki refresh token'ı geçersiz kılmak için logout endpoint'ini çağırır.
        /// Başarısız olursa sessizce devam eder (lokal temizlik yine yapılır).
        /// </summary>
        private static async Task LogoutApiCagirAsync()
        {
            try
            {
                var token = _yetkiToken;
                if (string.IsNullOrEmpty(token))
                    return;

                var baseUrl = Preferences.Default.Get("ApiBaseUrl", Constants.ApiBaseUrl);
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                await client.PostAsync($"{baseUrl}kimlik-dogrulama/logout", null);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LOGOUT API HATASI]: {ex.Message}");
            }
        }

        public static bool GirisYapildiMi => _kullaniciId > 0;

        public static int KullaniciId
        {
            get => _kullaniciId;
            set => _kullaniciId = value;
        }

        public static string AdSoyad
        {
            get => _adSoyad;
            set => _adSoyad = string.IsNullOrEmpty(value) ? "Kullanıcı" : value;
        }

        public static int? BirimId
        {
            get => _birimId;
            set => _birimId = value;
        }

        public static int? ServisId
        {
            get => _servisId;
            set => _servisId = value;
        }

        public static int? VeliId
        {
            get => _veliId;
            set => _veliId = value;
        }

        public static KullaniciRolu Rol
        {
            get => _rol;
            set => _rol = value;
        }

        public static bool ServisMi => _rol == KullaniciRolu.Servis;
        public static bool VeliMi => _rol == KullaniciRolu.Veli;
        public static bool OgretmenMi => _rol == KullaniciRolu.Ogretmen;

        public static string YetkiToken
        {
            get => _yetkiToken;
            set => _yetkiToken = value;
        }

        public static string RefreshToken
        {
            get => _refreshToken;
            set => _refreshToken = value;
        }

        /// <summary>
        /// Token yenilemesi sonrası access ve refresh token'ları günceller.
        /// </summary>
        public static async Task TokenlariGuncelleAsync(string yetkiToken, string refreshToken)
        {
            _yetkiToken = yetkiToken;
            _refreshToken = refreshToken;

            try
            {
                if (!string.IsNullOrEmpty(yetkiToken))
                    await SecureStorage.Default.SetAsync(AnahtarYetkiToken, yetkiToken);
                if (!string.IsNullOrEmpty(refreshToken))
                    await SecureStorage.Default.SetAsync(AnahtarRefreshToken, refreshToken);
                await SecureStorage.Default.SetAsync(AnahtarGirisZamani, DateTime.UtcNow.ToString("O"));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SecureStorage TOKEN GÜNCELLEME HATASI]: {ex.Message}");
            }
        }
    }
}
