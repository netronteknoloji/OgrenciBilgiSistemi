using OgrenciBilgiSistemi.Abstractions;

namespace OgrenciBilgiSistemi.Infrastructure.FileStorage
{

public class LocalFileStorage : IFileStorage
{
    private readonly long _maxBytes = 2 * 1024 * 1024;
    private static readonly string[] _allowed = [".jpg", ".jpeg", ".png"];

    public async Task<string?> SaveImageAsync(IFormFile file, string? existingPath = null, CancellationToken ct = default)
    {
        if (file == null || file.Length == 0) return existingPath;

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_allowed.Contains(ext)) throw new InvalidOperationException("Yalnızca JPG ve PNG dosyalarına izin verilir.");
        if (file.Length > _maxBytes) throw new InvalidOperationException("Dosya boyutu en fazla 2MB olabilir.");

        // Dosyanın gerçek formatını magic byte'larla doğrula (uzantı aldatmacasına karşı)
        var header = new byte[4];
        await using (var peek = file.OpenReadStream())
        {
            _ = await peek.ReadAsync(header, 0, 4, ct);
        }

        bool jpegMi = header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
        bool pngMi  = header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47;

        if (!jpegMi && !pngMi)
            throw new InvalidOperationException("Dosya içeriği geçerli bir JPEG veya PNG değil.");

        // eski dosyayı sil
        if (!string.IsNullOrEmpty(existingPath))
        {
            var old = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", existingPath.TrimStart('/'));
            if (File.Exists(old)) File.Delete(old);
        }

        var dir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        var name = Guid.NewGuid() + ext;
        var path = Path.Combine(dir, name);

        using var stream = new FileStream(path, FileMode.Create);
        await file.CopyToAsync(stream, ct);

        return "/uploads/" + name;
    }
}

}