using Microsoft.Extensions.Options;
using System.Net;
using System.Text;
using System.Text.Json;
using ZKTecoWindowsService.Models.Options;
using System.Linq;

namespace ZKTecoWindowsService.Services
{
    public sealed class OgrenciSmsService : IOgrenciSmsService
    {
        private readonly HttpClient _http;
        private readonly SmsSettings _opt;

        public OgrenciSmsService(HttpClient http, IOptions<SmsSettings> opt)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _opt = opt?.Value ?? throw new ArgumentNullException(nameof(opt));
            if (string.IsNullOrWhiteSpace(_opt.ApiUrl)) throw new InvalidOperationException("SmsSettings.ApiUrl boş olamaz.");
            if (string.IsNullOrWhiteSpace(_opt.Username)) throw new InvalidOperationException("SmsSettings.Username boş olamaz.");
            if (string.IsNullOrWhiteSpace(_opt.Password)) throw new InvalidOperationException("SmsSettings.Password boş olamaz.");
            if (string.IsNullOrWhiteSpace(_opt.Sender)) throw new InvalidOperationException("SmsSettings.Sender boş olamaz.");

            _http.Timeout = TimeSpan.FromSeconds(_opt.TimeoutSeconds > 0 ? _opt.TimeoutSeconds : 30);
        }

        public async Task<SmsSendResult> SendSmsAsync(string telefon, string mesaj, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(telefon)) return Fail("Telefon boş.");
            if (string.IsNullOrWhiteSpace(mesaj)) return Fail("Mesaj boş.");

            var gsm05 = NormalizeTo05(telefon);
            if (gsm05 is null) return Fail("Geçersiz telefon formatı.");

            var text = _opt.UseTurkish ? mesaj : RemoveTr(mesaj);

            // iLKSMS ntonsms beklenen form: telMesajlar = [{ "mesaj":"...", "telefon":"05xxxxxxxxx" }]
            var telMesajlarJson = JsonSerializer.Serialize(new[] { new { mesaj = text, telefon = gsm05 } },
                new JsonSerializerOptions { PropertyNamingPolicy = null });

            var form = new[]
            {
                new KeyValuePair<string,string>("apiUsername", _opt.Username),
                new KeyValuePair<string,string>("apiPassword", _opt.Password),
                new KeyValuePair<string,string>("baslik",     _opt.Sender),
                new KeyValuePair<string,string>("tur",        _opt.UseTurkish ? "turkce" : "normal"),
                new KeyValuePair<string,string>("telMesajlar", telMesajlarJson)
                // Gerekirse ileri tarih/saat:
                // new("tarih","30/01/2019"), new("saat","17:05")
            };

            using var content = new FormUrlEncodedContent(form);
            content.Headers.ContentType!.CharSet = "utf-8";

            HttpResponseMessage resp;
            try
            {
                resp = await _http.PostAsync(_opt.ApiUrl, content, ct);
            }
            catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
            {
                return Fail("İstek zaman aşımı.", (int)HttpStatusCode.RequestTimeout, ex.Message);
            }
            catch (Exception ex)
            {
                return Fail(ex.Message, (int)HttpStatusCode.InternalServerError, ex.ToString());
            }

            var body = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode)
            {
                // eski: var allowed = resp.Headers.Allow is null ? "" : string.Join(",", resp.Headers.Allow);

                var allowed = resp.Headers.TryGetValues("Allow", out var allowValues)
                    ? string.Join(",", allowValues)
                    : (resp.Content?.Headers?.TryGetValues("Allow", out var allowValues2) == true
                        ? string.Join(",", allowValues2)
                        : "");

                return Fail($"HTTP {(int)resp.StatusCode} {resp.ReasonPhrase} (Allow:{allowed})",
                            (int)resp.StatusCode, body);
            }

            // Bazı entegrasyonlarda gövde "OK" veya detay JSON olabilir; ham cevabı aynen döndürüyoruz.
            return new SmsSendResult
            {
                Success = true,
                StatusCode = (int)resp.StatusCode,
                RawResponse = body
            };
        }

        // ---- helpers ----
        private static SmsSendResult Fail(string err, int status = 400, string raw = "")
            => new() { Success = false, Error = err, StatusCode = status, RawResponse = raw };

        // 05XXXXXXXXX'a normalize eder. Kabul edilen girdiler:
        // 05XXXXXXXXX, 5XXXXXXXXX, +90 5XXXXXXXXX, 90 5XXXXXXXXX, 0 5XXXXXXXXXX
        private static string? NormalizeTo05(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;

            var d = new string(input.Where(char.IsDigit).ToArray());
            // 5XXXXXXXXX -> 05XXXXXXXXX
            if (d.Length == 10 && d.StartsWith("5")) return "0" + d;
            // 05XXXXXXXXX (11 hane, doğru)
            if (d.Length == 11 && d.StartsWith("05")) return d;
            // 905XXXXXXXXX -> 05XXXXXXXXX
            if (d.Length == 12 && d.StartsWith("90") && d[2] == '5') return "0" + d[2..];
            // 0905XXXXXXXXX -> 05XXXXXXXXX
            if (d.Length == 13 && d.StartsWith("090") && d[3] == '5') return "0" + d[3..];
            // başka tüm durumlar: geçersiz
            return null;
        }

        private static string RemoveTr(string s) =>
            s.Replace('ç', 'c').Replace('ğ', 'g').Replace('ı', 'i').Replace('ö', 'o').Replace('ş', 's').Replace('ü', 'u')
             .Replace('Ç', 'C').Replace('Ğ', 'G').Replace('İ', 'I').Replace('Ö', 'O').Replace('Ş', 'S').Replace('Ü', 'U');
    }
}