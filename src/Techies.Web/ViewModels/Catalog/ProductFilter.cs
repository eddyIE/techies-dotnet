namespace Techies.Web.ViewModels.Catalog;

public sealed record ProductRow(Guid Id, string Name, string CategoryName, decimal Price, int Available, bool Active, string? ThumbnailUrl, string Slug = "");
