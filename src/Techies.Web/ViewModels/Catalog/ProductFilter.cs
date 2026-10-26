namespace Techies.Web.ViewModels.Catalog;

public sealed class ProductFilter
{
    public string? Q { get; set; }
    public Guid? CategoryId { get; set; }
    public bool OutOfStock { get; set; }
    public bool? Active { get; set; }
    public int Page { get; set; } = 1;
}

public sealed record ProductRow(Guid Id, string Name, string CategoryName, decimal Price, int Available, bool Active, string? ThumbnailUrl, string Slug = "");
