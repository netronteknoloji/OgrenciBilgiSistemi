namespace StudentTrackingSystem.Services
{
    /// <summary>
    /// Kullanıcı oturum bilgilerini SecureStorage ile güvenli şekilde yöneten sınıf.
    /// Hassas veriler (token, kullanıcı bilgileri) şifreli olarak saklanır.
    /// </summary>
    public static class UserSession
    {
        // Bellek içi cache - SecureStorage'a her erişimde I/O yapılmasını önler
        private static int _userId;
        private static string _fullName = "Kullanıcı";
        private static int? _unitId;
        private static int? _serviceId;
        private static string _authToken;
        private static bool _isLoaded;

        // SecureStorage anahtarları
        private const string KeyUserId = "session_user_id";
        private const string KeyFullName = "session_full_name";
        private const string KeyUnitId = "session_unit_id";
        private const string KeyServiceId = "session_service_id";
        private const string KeyAuthToken = "session_auth_token";
        private const string KeyLoginTime = "session_login_time";

        // Oturum zaman aşımı (8 saat)
        private static readonly TimeSpan SessionTimeout = TimeSpan.FromHours(8);

        /// <summary>
        /// Giriş başarılı olduğunda tüm oturum bilgilerini SecureStorage'a kaydeder.
        /// </summary>
        public static async Task SetSessionAsync(int userId, string fullName, int? unitId, string authToken = null)
        {
            _userId = userId;
            _fullName = string.IsNullOrEmpty(fullName) ? "Kullanıcı" : fullName;
            _unitId = unitId;
            _authToken = authToken;
            _isLoaded = true;

            try
            {
                await SecureStorage.Default.SetAsync(KeyUserId, userId.ToString());
                await SecureStorage.Default.SetAsync(KeyFullName, _fullName);
                await SecureStorage.Default.SetAsync(KeyLoginTime, DateTime.UtcNow.ToString("O"));

                if (unitId.HasValue)
                    await SecureStorage.Default.SetAsync(KeyUnitId, unitId.Value.ToString());

                if (!string.IsNullOrEmpty(authToken))
                    await SecureStorage.Default.SetAsync(KeyAuthToken, authToken);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SecureStorage HATASI]: {ex.Message}");
            }
        }

        /// <summary>
        /// Uygulama yeniden başlatıldığında oturum bilgilerini SecureStorage'dan yükler.
        /// Oturum süresi dolmuşsa otomatik olarak temizler.
        /// </summary>
        public static async Task LoadSessionAsync()
        {
            if (_isLoaded) return;

            try
            {
                // Oturum süresini kontrol et
                var loginTimeStr = await SecureStorage.Default.GetAsync(KeyLoginTime);
                if (!string.IsNullOrEmpty(loginTimeStr) && DateTime.TryParse(loginTimeStr, out var loginTime))
                {
                    if (DateTime.UtcNow - loginTime > SessionTimeout)
                    {
                        await ClearSessionAsync();
                        return;
                    }
                }

                var userIdStr = await SecureStorage.Default.GetAsync(KeyUserId);
                if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out var uid))
                    _userId = uid;

                _fullName = await SecureStorage.Default.GetAsync(KeyFullName) ?? "Kullanıcı";

                var unitIdStr = await SecureStorage.Default.GetAsync(KeyUnitId);
                if (!string.IsNullOrEmpty(unitIdStr) && int.TryParse(unitIdStr, out var parsedUnitId))
                    _unitId = parsedUnitId;

                var serviceIdStr = await SecureStorage.Default.GetAsync(KeyServiceId);
                if (!string.IsNullOrEmpty(serviceIdStr) && int.TryParse(serviceIdStr, out var sid))
                    _serviceId = sid;

                _authToken = await SecureStorage.Default.GetAsync(KeyAuthToken);

                _isLoaded = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SecureStorage YÜKLEME HATASI]: {ex.Message}");
            }
        }

        /// <summary>
        /// Tüm oturum bilgilerini bellekten ve SecureStorage'dan temizler.
        /// </summary>
        public static async Task ClearSessionAsync()
        {
            _userId = 0;
            _fullName = "Kullanıcı";
            _unitId = null;
            _serviceId = null;
            _authToken = null;
            _isLoaded = true;

            try
            {
                SecureStorage.Default.Remove(KeyUserId);
                SecureStorage.Default.Remove(KeyFullName);
                SecureStorage.Default.Remove(KeyUnitId);
                SecureStorage.Default.Remove(KeyServiceId);
                SecureStorage.Default.Remove(KeyAuthToken);
                SecureStorage.Default.Remove(KeyLoginTime);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SecureStorage TEMİZLEME HATASI]: {ex.Message}");
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Kullanıcının aktif bir oturumu olup olmadığını kontrol eder.
        /// </summary>
        public static bool IsLoggedIn => _userId > 0;

        public static int UserId
        {
            get => _userId;
            set => _userId = value;
        }

        public static string FullName
        {
            get => _fullName;
            set => _fullName = string.IsNullOrEmpty(value) ? "Kullanıcı" : value;
        }

        public static int? UnitId
        {
            get => _unitId;
            set => _unitId = value;
        }

        public static int? ServiceId
        {
            get => _serviceId;
            set => _serviceId = value;
        }

        public static string AuthToken
        {
            get => _authToken;
            set => _authToken = value;
        }
    }
}
