using Microsoft.AspNetCore.Http;
using Techies.Web.Domain;
using Techies.Web.Infrastructure;

namespace Techies.Tests.Unit;

public sealed class ImageStorageTests : IDisposable
{
    private readonly string _webRoot = Directory.CreateTempSubdirectory("techies-upload-").FullName;
    private readonly DateTimeOffset _now = new(2026, 10, 26, 0, 0, 0, TimeSpan.Zero);

    public void Dispose() => Directory.Delete(_webRoot, recursive: true);

    private static readonly byte[] PngHeader = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    // A file of `bytes` length that starts with `header` (a real PNG signature by default).
    private static IFormFile File(string name, int bytes, byte[]? header = null)
    {
        var content = new byte[bytes];
        (header ?? PngHeader).CopyTo(content, 0);
        return new FormFile(new MemoryStream(content), 0, bytes, "file", name);
    }

    [Fact]
    public async Task Saves_allowed_image_under_uploads_year_folder()
    {
        var storage = new ImageStorage(_webRoot, _now);

        var url = await storage.SaveAsync(File("photo.PNG", 1024));

        Assert.Matches(@"^/uploads/2026/[0-9a-f]{32}\.png$", url);
        Assert.True(System.IO.File.Exists(Path.Combine(_webRoot, url.TrimStart('/'))));
    }

    [Fact]
    public async Task Rejects_disallowed_extension()
    {
        var storage = new ImageStorage(_webRoot, _now);

        var ex = await Assert.ThrowsAsync<DomainException>(() => storage.SaveAsync(File("script.svg", 10)));
        Assert.Contains("jpg", ex.Message);
    }

    [Fact]
    public async Task Rejects_non_image_content_with_an_image_extension()
    {
        var storage = new ImageStorage(_webRoot, _now);

        await Assert.ThrowsAsync<DomainException>(() => storage.SaveAsync(File("evil.png", 64, "<html><script>"u8.ToArray())));
    }

    [Fact]
    public async Task Accepts_jpeg_and_webp_signatures()
    {
        var storage = new ImageStorage(_webRoot, _now);

        await storage.SaveAsync(File("a.jpg", 64, [0xFF, 0xD8, 0xFF, 0xE0]));
        await storage.SaveAsync(File("b.webp", 64, "RIFF\0\0\0\0WEBP"u8.ToArray()));
    }

    [Fact]
    public async Task Rejects_file_larger_than_2_mb()
    {
        var storage = new ImageStorage(_webRoot, _now);

        await Assert.ThrowsAsync<DomainException>(() => storage.SaveAsync(File("big.jpg", 2 * 1024 * 1024 + 1)));
    }
}
