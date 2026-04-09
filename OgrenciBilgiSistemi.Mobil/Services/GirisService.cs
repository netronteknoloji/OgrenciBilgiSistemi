using System.Text;
using System.Text.Json;
using OgrenciBilgiSistemi.Mobil.Models;
using OgrenciBilgiSistemi.Shared.Enums;

namespace OgrenciBilgiSistemi.Mobil.Services
{
    public class GirisService : TemelApiService
    {
        public async Task<bool> KullaniciGirisYapAsync(string kullaniciAdi, string sifre, string okulKodu)
        {
            try
            {
                var loginData = new { KullaniciAdi = kullaniciAdi, Sifre = sifre, OkulKodu = okulKodu };
                var json = JsonSerializer.Serialize(loginData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{BaseUrl}kimlik-dogrulama/login", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();

                    using var doc = JsonDocument.Parse(responseString);
                    var root = doc.RootElement;

                    string token = null;
                    Kullanici kullanici = null;
                    string refreshToken = null;

                    if (root.TryGetProperty("token", out var tokenElement))
                        token = tokenElement.GetString();

                    if (root.TryGetProperty("refreshToken", out var refreshTokenElement))
                        refreshToken = refreshTokenElement.GetString();

                    if (root.TryGetProperty("kullanici", out var userElement))
                        kullanici = JsonSerializer.Deserialize<Kullanici>(userElement.GetRawText(), _jsonOptions);

                    if (kullanici != null)
                    {
                        string gorunenAd = !string.IsNullOrWhiteSpace(kullanici.AdSoyad)
                            ? kullanici.AdSoyad
                            : kullanici.KullaniciAdi;

                        int? veliId = kullanici.Rol == KullaniciRolu.Veli ? kullanici.KullaniciId : null;
                        int? servisId = kullanici.Rol == KullaniciRolu.Servis ? kullanici.KullaniciId : null;

                        await KullaniciOturum.OturumAyarlaAsync(
                            kullaniciId: kullanici.KullaniciId,
                            adSoyad: gorunenAd,
                            birimId: null,
                            rol: kullanici.Rol,
                            servisId: servisId,
                            veliId: veliId,
                            yetkiToken: token,
                            refreshToken: refreshToken,
                            okulKodu: okulKodu,
                            okulApiUrl: Preferences.Default.Get("AktifOkulApiUrl", Constants.VarsayilanApiUrl)
                        );

                        YetkiBasliginiYenile();

                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[API HATASI]: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// Giriş yapmış kullanıcının şifresini değiştirir.
        /// </summary>
        public async Task<bool> SifreDegistirAsync(string yeniSifre)
        {
            try
            {
                var model = new { YeniSifre = yeniSifre };
                var response = await PostAsJsonAsync($"{BaseUrl}kimlik-dogrulama/sifre-degistir", model);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ŞİFRE DEĞİŞTİRME HATASI]: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Kullanıcı adının ilk harflerine göre eşleşen kullanıcı adlarını getirir.
        /// Okul kodu ile birlikte aranır.
        /// </summary>
        public async Task<List<string>> KullaniciAdiAraAsync(string aranan, string okulKodu)
        {
            try
            {
                var response = await _httpClient.GetAsync(
                    $"{BaseUrl}kimlik-dogrulama/kullanici-ara?q={Uri.EscapeDataString(aranan)}&okulKodu={Uri.EscapeDataString(okulKodu)}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<string>>(json, _jsonOptions) ?? new List<string>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[KullaniciAdiAra HATASI]: {ex.Message}");
            }

            return new List<string>();
        }
    }
}
