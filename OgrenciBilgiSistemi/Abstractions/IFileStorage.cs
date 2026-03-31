namespace OgrenciBilgiSistemi.Abstractions
{
    public interface IFileStorage
    {
        Task<string?> SaveImageAsync(IFormFile file, string? existingPath = null, CancellationToken ct = default);
    }
}
