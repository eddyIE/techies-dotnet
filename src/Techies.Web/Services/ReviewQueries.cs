using Microsoft.EntityFrameworkCore;
using Techies.Web.Data;
using Techies.Web.Infrastructure;
using Techies.Web.Infrastructure.Grid;
using Techies.Web.ViewModels;
using Techies.Web.ViewModels.Customers;

namespace Techies.Web.Services;

// BRD §6: monitoring only — no edit, delete or reply.
public sealed class ReviewQueries(AppDbContext db)
{
    public static readonly string[] GridSorts = ["date", "rating", "product"];

    public async Task<(List<ReviewRow> Rows, int Total)> GridAsync(GridRequest r, CancellationToken ct = default)
    {
        var query = db.Reviews.AsNoTracking();
        if (r.Text("q") is { } q) query = query.Where(x => x.Comment != null && EF.Functions.ILike(x.Comment, QueryExtensions.Contains(q)));
        if (Guid.TryParse(r.Text("product"), out var productId)) query = query.Where(x => x.ProductId == productId);
        if (r.Int("rating_from") is { } ratingFrom) query = query.Where(x => x.Rating >= ratingFrom);
        if (r.Int("rating_to") is { } ratingTo) query = query.Where(x => x.Rating <= ratingTo);
        if (r.Date("date_from") is { } from) { var start = Format.StartOfLocalDay(from); query = query.Where(x => x.CreatedAt >= start); }
        if (r.Date("date_to") is { } to) { var end = Format.StartOfLocalDay(to.AddDays(1)); query = query.Where(x => x.CreatedAt < end); }

        query = (r.Sort, r.Desc) switch
        {
            ("rating", false) => query.OrderBy(x => x.Rating).ThenByDescending(x => x.CreatedAt),
            ("rating", true) => query.OrderByDescending(x => x.Rating).ThenByDescending(x => x.CreatedAt),
            ("product", false) => query.OrderBy(x => x.Product.Name).ThenByDescending(x => x.CreatedAt),
            ("product", true) => query.OrderByDescending(x => x.Product.Name).ThenByDescending(x => x.CreatedAt),
            (_, false) => query.OrderBy(x => x.CreatedAt),
            _ => query.OrderByDescending(x => x.CreatedAt),
        };
        var total = await query.CountAsync(ct);
        var rows = await query.Skip((r.Page - 1) * r.PageSize).Take(r.PageSize)
            .Select(x => new ReviewRow(x.Id, x.ProductId, x.Product.Name, x.Product.Slug, x.CustomerId, x.Customer.FullName, x.Rating, x.Comment, x.CreatedAt))
            .ToListAsync(ct);
        return (rows, total);
    }

    public Task<List<(Guid Id, string Name)>> ReviewedProductsAsync(CancellationToken ct = default) =>
        db.Products.AsNoTracking().Where(p => db.Reviews.Any(r => r.ProductId == p.Id)).OrderBy(p => p.Name)
            .Select(p => new ValueTuple<Guid, string>(p.Id, p.Name)).ToListAsync(ct);
}
