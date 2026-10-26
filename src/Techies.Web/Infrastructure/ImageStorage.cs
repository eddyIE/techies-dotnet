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
        if (!await LooksLikeImageAsync(file, ct))
            throw new DomainException("Tệp tải lên không phải là ảnh hợp lệ.");

        var relative = $"uploads/{now:yyyy}/{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(webRootPath, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await using var stream = File.Create(fullPath);
        await file.CopyToAsync(stream, ct);
        return "/" + relative;
    }

    // The extension is chosen by the uploader, so also check the file's signature bytes.
    private static async Task<bool> LooksLikeImageAsync(IFormFile file, CancellationToken ct)
    {
        var header = new byte[12];
        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, ct);
        var h = header.AsSpan(0, read);
        return h.StartsWith((ReadOnlySpan<byte>)[0xFF, 0xD8, 0xFF])                                   // JPEG
            || h.StartsWith((ReadOnlySpan<byte>)[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A])     // PNG
            || (read >= 12 && h[..4].SequenceEqual("RIFF"u8) && h[8..12].SequenceEqual("WEBP"u8));    // WebP
    }
}
