using Microsoft.EntityFrameworkCore;
using Techies.Web.Data;
using Techies.Web.Infrastructure;
using Techies.Web.ViewModels;
using Techies.Web.ViewModels.Customers;

namespace Techies.Web.Services;

// BRD §6: monitoring only — no edit, delete or reply.
public sealed class ReviewQueries(AppDbContext db)
{
    public Task<Paged<ReviewRow>> ListAsync(ReviewFilter filter, CancellationToken ct = default)
    {
        var query = db.Reviews.AsNoTracking();
        if (filter.LowOnly) query = query.Where(r => r.Rating <= 2);
        if (filter.Rating is { } rating) query = query.Where(r => r.Rating == rating);
        if (filter.ProductId is { } productId) query = query.Where(r => r.ProductId == productId);
        return query.OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReviewRow(r.Id, r.ProductId, r.Product.Name, r.CustomerId, r.Customer.FullName, r.Rating, r.Comment, r.CreatedAt))
            .ToPagedAsync(filter.Page, ct: ct);
    }

    public Task<List<(Guid Id, string Name)>> ReviewedProductsAsync(CancellationToken ct = default) =>
        db.Products.AsNoTracking().Where(p => db.Reviews.Any(r => r.ProductId == p.Id)).OrderBy(p => p.Name)
            .Select(p => new ValueTuple<Guid, string>(p.Id, p.Name)).ToListAsync(ct);
}
