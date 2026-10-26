using Microsoft.EntityFrameworkCore;
using Techies.Web.Data;
using Techies.Web.Infrastructure;
using Techies.Web.ViewModels;
using Techies.Web.ViewModels.Catalog;

namespace Techies.Web.Services;

public sealed class ProductService(AppDbContext db)
{
    public Task<Paged<ProductRow>> SearchAsync(ProductFilter filter, CancellationToken ct = default)
    {
        var query = db.Products.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(filter.Q))
            query = query.Where(p => EF.Functions.ILike(p.Name, QueryExtensions.Contains(filter.Q)));
        if (filter.CategoryId is { } categoryId)
            query = query.Where(p => p.CategoryId == categoryId);
        if (filter.OutOfStock)
            query = query.Where(p => p.Stock!.Available == 0);
        if (filter.Active is { } active)
            query = query.Where(p => p.Active == active);

        return query
            .OrderBy(p => p.Name)
            .Select(p => new ProductRow(p.Id, p.Name, p.Category.Name, p.Price, p.Stock!.Available, p.Active, p.ThumbnailUrl))
            .ToPagedAsync(filter.Page, ct: ct);
    }
}
