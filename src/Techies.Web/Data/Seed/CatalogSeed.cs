using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Techies.Web.Domain;

namespace Techies.Web.Data.Seed;

// categories.json / products.json are the final catalogue state of ../techies
// (catalog-service V1–V6 + inventory-service V1–V4 replayed and exported once), minus the
// hidden "Demo" category. Ids are kept so ported reviews can refer to products.
internal static class CatalogSeed
{
    private sealed record CategoryRow(Guid Id, string Name, string Slug, string? ImageUrl, int DisplayOrder);
    private sealed record SpecRow(string Name, string Value);
    private sealed record ProductRow(
        Guid Id, Guid CategoryId, string Name, string Slug, string Description, decimal Price,
        string? ThumbnailUrl, bool Active, List<string> Images, List<SpecRow> Specs, int Available);

    public static async Task SeedAsync(AppDbContext db, DateTimeOffset now, CancellationToken ct)
    {
        if (await db.Categories.AnyAsync(ct)) return;

        foreach (var c in SeedFiles.Read<CategoryRow>("categories.json"))
            db.Categories.Add(new Category { Id = c.Id, Name = c.Name, Slug = c.Slug, ImageUrl = c.ImageUrl, DisplayOrder = c.DisplayOrder });

        // Backdated so seeded orders (up to 15 months old) never predate their products.
        var created = now.AddDays(-500);
        foreach (var p in SeedFiles.Read<ProductRow>("products.json"))
        {
            db.Products.Add(new Product
            {
                Id = p.Id, CategoryId = p.CategoryId, Name = p.Name, Slug = p.Slug, Description = p.Description,
                Price = p.Price, ThumbnailUrl = p.ThumbnailUrl, Active = p.Active, CreatedAt = created,
                Images = p.Images.Select((url, i) => new ProductImage { Url = url, DisplayOrder = i }).ToList(),
                Specs = p.Specs.Select((s, i) => new ProductSpec { SpecName = s.Name, SpecValue = s.Value, DisplayOrder = i }).ToList(),
                Stock = new StockItem { Available = p.Available, UpdatedAt = now },
            });
        }
        await db.SaveChangesAsync(ct);
    }
}

internal static class SeedFiles
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static List<T> Read<T>(string fileName)
    {
        var assembly = typeof(SeedFiles).Assembly;
        using var stream = assembly.GetManifestResourceStream($"Techies.Web.Data.Seed.{fileName}")
            ?? throw new InvalidOperationException($"Seed file {fileName} is not embedded.");
        return JsonSerializer.Deserialize<List<T>>(stream, Options)!;
    }
}
