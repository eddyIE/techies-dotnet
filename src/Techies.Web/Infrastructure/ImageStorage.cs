using Techies.Web.Domain;

namespace Techies.Web.Infrastructure;

// Stores uploaded images under wwwroot/uploads/<yyyy>/ and returns their public URL.
public sealed class ImageStorage(string webRootPath, DateTimeOffset now)
{
    private const long MaxBytes = 2 * 1024 * 1024;
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];

    public async Task<string> SaveAsync(IFormFile file, CancellationToken ct = default)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            throw new DomainException("Chỉ chấp nhận ảnh định dạng jpg, png hoặc webp.");
        if (file.Length == 0 || file.Length > MaxBytes)
            throw new DomainException("Ảnh phải có dung lượng tối đa 2 MB.");

        var relative = $"uploads/{now:yyyy}/{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(webRootPath, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await using var stream = File.Create(fullPath);
        await file.CopyToAsync(stream, ct);
        return "/" + relative;
    }
}
