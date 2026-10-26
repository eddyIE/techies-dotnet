using Techies.Web.Domain;

namespace Techies.Web.ViewModels.Shop;

public sealed record ProductCard(Guid Id, string Name, string Slug, decimal Price, string? ThumbnailUrl, int Available);

public sealed record ReviewItem(string CustomerName, short Rating, string? Comment, DateTimeOffset CreatedAt);

public sealed record ProductDetail(Product Product, string CategoryName, string CategorySlug, int Available,
    List<ReviewItem> Reviews, double? AverageRating);

public sealed record HomeModel(List<Category> Categories, List<ProductCard> Latest);
