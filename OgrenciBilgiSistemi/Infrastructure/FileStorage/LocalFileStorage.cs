using System.Text.RegularExpressions;
using OgrenciBilgiSistemi.Abstractions;

namespace OgrenciBilgiSistemi.Infrastructure.FileStorage
{
    public class LocalFileStorage : IFileStorage
    {
        private const long MaxBytes = 2 * 1024 * 1024;
        private static readonly string[] _izinliUzantilar = [".jpg", ".jpeg", ".png"];
        private static readonly Regex _altKlasorDeseni = new("^[a-z]{1,20}$", RegexOptions.Compiled);

        private readonly string _webRoot;
        private readonly string _uploadsKok;
        private readonly ILogger<LocalFileStorage> _logger;

        public LocalFileStorage(IWebHostEnvironment env, ILogger<LocalFileStorage> logger)
        {
            _webRoot = Path.GetFullPath(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"));
            _uploadsKok = Path.Combine(_webRoot, "uploads") + Path.DirectorySeparatorChar;
            _logger = logger;
        }

        public async Task<string?> SaveImageAsync(IFormFile file, string? existingPath = null, CancellationToken ct = default, string? altKlasor = null)
        {
            if (file == null || file.Length == 0) return existingPath;

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_izinliUzantilar.Contains(ext)) throw new InvalidOperationException("Yalnızca JPG ve PNG dosyalarına izin verilir.");
            if (file.Length > MaxBytes) throw new InvalidOperationException("Dosya boyutu en fazla 2MB olabilir.");
            if (altKlasor is not null && !_altKlasorDeseni.IsMatch(altKlasor))
                throw new ArgumentException("Geçersiz klasör adı.", nameof(altKlasor));

            // Dosyanın gerçek formatını içerik imzasıyla doğrula (uzantı aldatmacasına karşı)
            var header = new byte[4];
            await using (var peek = file.OpenReadStream())
            {
                _ = await peek.ReadAsync(header.AsMemory(0, 4), ct);
            }

            bool jpegMi = header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
            bool pngMi = header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47;
            if (!jpegMi && !pngMi)
                throw new InvalidOperationException("Dosya içeriği geçerli bir JPEG veya PNG değil.");

            var dir = altKlasor is null ? _uploadsKok : Path.Combine(_uploadsKok, altKlasor);
            Directory.CreateDirectory(dir);

            var name = Guid.NewGuid().ToString("N") + (pngMi ? ".png" : ".jpg");
            await using (var stream = new FileStream(Path.Combine(dir, name), FileMode.CreateNew))
            {
                await file.CopyToAsync(stream, ct);
            }

            // Eski görsel, yenisi başarıyla yazıldıktan sonra silinir
            Sil(existingPath);

            return altKlasor is null ? $"/uploads/{name}" : $"/uploads/{altKlasor}/{name}";
        }

        public void Sil(string? goreliYol)
        {
            var tamYol = UploadsIcindekiYol(goreliYol);
            if (tamYol is null || !File.Exists(tamYol)) return;

            try { File.Delete(tamYol); }
            catch (Exception ex) { _logger.LogWarning(ex, "Eski görsel silinemedi."); }
        }

        // Göreli yol wwwroot/uploads altına çözülüyorsa tam yolu; değilse (../, mutlak yol vb.) null
        private string? UploadsIcindekiYol(string? goreliYol)
        {
            if (string.IsNullOrWhiteSpace(goreliYol)) return null;
            try
            {
                var tam = Path.GetFullPath(Path.Combine(_webRoot, goreliYol.TrimStart('/', '\\')));
                return tam.StartsWith(_uploadsKok, StringComparison.OrdinalIgnoreCase) ? tam : null;
            }
            catch (Exception) // geçersiz karakterli yol
            {
                return null;
            }
        }
    }
}
