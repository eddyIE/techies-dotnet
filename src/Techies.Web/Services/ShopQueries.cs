using Microsoft.EntityFrameworkCore;
using Techies.Web.Data;
using Techies.Web.Infrastructure;
using Techies.Web.ViewModels;
using Techies.Web.ViewModels.Shop;

namespace Techies.Web.Services;

// Read side of the customer shop. Inactive products never leave this class (BRD §2 "Bật/Tắt kinh doanh").
public sealed class ShopQueries(AppDbContext db)
{
    public const int PageSize = 12;

    public async Task<HomeModel> HomeAsync(CancellationToken ct = default) => new(
        await db.Categories.AsNoTracking().OrderBy(c => c.DisplayOrder).ToListAsync(ct),
        await db.Products.AsNoTracking().Where(p => p.Active).OrderByDescending(p => p.CreatedAt).ThenBy(p => p.Name)
            .Take(8).Select(ToCard).ToListAsync(ct));

    public Task<Paged<ProductCard>> ListAsync(string? categorySlug, string? q, int page, CancellationToken ct = default)
    {
        var query = db.Products.AsNoTracking().Where(p => p.Active);
        if (!string.IsNullOrWhiteSpace(categorySlug))
            query = query.Where(p => p.Category.Slug == categorySlug);
        query = query.MatchKeyword(q);
        return query.OrderBy(p => p.Name).Select(ToCard).ToPagedAsync(page, PageSize, ct);
    }

    public Task<Domain.Category?> CategoryAsync(string slug, CancellationToken ct = default) =>
        db.Categories.AsNoTracking().SingleOrDefaultAsync(c => c.Slug == slug, ct);

    public async Task<ProductDetail?> DetailAsync(string slug, CancellationToken ct = default)
    {
        var product = await db.Products.AsNoTracking()
            .Include(p => p.Category).Include(p => p.Images).Include(p => p.Specs).Include(p => p.Stock)
            .SingleOrDefaultAsync(p => p.Slug == slug && p.Active, ct);
        if (product is null) return null;

        var reviews = await db.Reviews.AsNoTracking().Where(r => r.ProductId == product.Id)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReviewItem(r.Customer.FullName, r.Rating, r.Comment, r.CreatedAt))
            .ToListAsync(ct);
        double? average = reviews.Count == 0 ? null : Math.Round(reviews.Average(r => r.Rating), 1);
        return new ProductDetail(product, product.Category.Name, product.Category.Slug, product.Stock?.Available ?? 0, reviews, average);
    }

    private static readonly System.Linq.Expressions.Expression<Func<Domain.Product, ProductCard>> ToCard = p =>
        new ProductCard(p.Id, p.Name, p.Slug, p.Price, p.ThumbnailUrl, p.Stock!.Available);
}
