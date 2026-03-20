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
        private static int _rol;
        private static string _yetkiToken;
        private static bool _yuklendi;

        /// <summary>
        /// Apple App Store incelemesi için demo modunu belirtir.
        /// Demo modunda API çağrıları yapılmaz, sahte veriler gösterilir.
        /// </summary>
        public static bool DemoModuMu { get; private set; }

        // SecureStorage anahtarları
        private const string AnahtarKullaniciId = "session_user_id";
        private const string AnahtarAdSoyad = "session_full_name";
        private const string AnahtarBirimId = "session_unit_id";
        private const string AnahtarServisId = "session_service_id";
        private const string AnahtarRol = "session_rol";
        private const string AnahtarYetkiToken = "session_auth_token";
        private const string AnahtarGirisZamani = "session_login_time";

        // Oturum zaman aşımı (8 saat)
        private static readonly TimeSpan OturumSuresi = TimeSpan.FromHours(8);

        /// <summary>
        /// Apple App Store incelemesi için demo modunu etkinleştirir.
        /// </summary>
        public static void DemoModuAyarla()
        {
            DemoModuMu = true;
            _kullaniciId = -1;
            _adSoyad = "Demo Kullanıcı";
            _birimId = -1;
            _yetkiToken = "demo-token";
            _yuklendi = true;
        }

        /// <summary>
        /// Giriş başarılı olduğunda tüm oturum bilgilerini SecureStorage'a kaydeder.
        /// </summary>
        public static async Task OturumAyarlaAsync(int kullaniciId, string adSoyad, int? birimId, int rol = 2, int? servisId = null, string yetkiToken = null)
        {
            _kullaniciId = kullaniciId;
            _adSoyad = string.IsNullOrEmpty(adSoyad) ? "Kullanıcı" : adSoyad;
            _birimId = birimId;
            _rol = rol;
            _servisId = servisId;
            _yetkiToken = yetkiToken;
            _yuklendi = true;

            try
            {
                await SecureStorage.Default.SetAsync(AnahtarKullaniciId, kullaniciId.ToString());
                await SecureStorage.Default.SetAsync(AnahtarAdSoyad, _adSoyad);
                await SecureStorage.Default.SetAsync(AnahtarRol, rol.ToString());
                await SecureStorage.Default.SetAsync(AnahtarGirisZamani, DateTime.UtcNow.ToString("O"));

                if (birimId.HasValue)
                    await SecureStorage.Default.SetAsync(AnahtarBirimId, birimId.Value.ToString());

                if (servisId.HasValue)
                    await SecureStorage.Default.SetAsync(AnahtarServisId, servisId.Value.ToString());

                if (!string.IsNullOrEmpty(yetkiToken))
                    await SecureStorage.Default.SetAsync(AnahtarYetkiToken, yetkiToken);
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

                var rolStr = await SecureStorage.Default.GetAsync(AnahtarRol);
                if (!string.IsNullOrEmpty(rolStr) && int.TryParse(rolStr, out var parsedRol))
                    _rol = parsedRol;

                _yetkiToken = await SecureStorage.Default.GetAsync(AnahtarYetkiToken);

                _yuklendi = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SecureStorage YÜKLEME HATASI]: {ex.Message}");
            }
        }

        /// <summary>
        /// Tüm oturum bilgilerini bellekten ve SecureStorage'dan temizler.
        /// </summary>
        public static Task OturumTemizleAsync()
        {
            _kullaniciId = 0;
            _adSoyad = "Kullanıcı";
            _birimId = null;
            _servisId = null;
            _rol = 0;
            _yetkiToken = null;
            _yuklendi = false;
            DemoModuMu = false;

            try
            {
                SecureStorage.Default.Remove(AnahtarKullaniciId);
                SecureStorage.Default.Remove(AnahtarAdSoyad);
                SecureStorage.Default.Remove(AnahtarBirimId);
                SecureStorage.Default.Remove(AnahtarServisId);
                SecureStorage.Default.Remove(AnahtarRol);
                SecureStorage.Default.Remove(AnahtarYetkiToken);
                SecureStorage.Default.Remove(AnahtarGirisZamani);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SecureStorage TEMİZLEME HATASI]: {ex.Message}");
            }

            return Task.CompletedTask;
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

        /// <summary>
        /// Kullanıcı rolü: 1 = Admin, 2 = Ogretmen, 3 = Sofor
        /// </summary>
        public static int Rol
        {
            get => _rol;
            set => _rol = value;
        }

        public static bool SoforMu => _rol == 3;

        public static string YetkiToken
        {
            get => _yetkiToken;
            set => _yetkiToken = value;
        }
    }
}
