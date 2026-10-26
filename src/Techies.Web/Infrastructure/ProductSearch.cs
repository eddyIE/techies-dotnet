using Microsoft.EntityFrameworkCore;
using Techies.Web.Data;
using Techies.Web.Domain;

namespace Techies.Web.Infrastructure;

public static class ProductSearch
{
    // Every word must appear in the product name or its category name, ignoring case and
    // Vietnamese accents: "dien thoai samsung" finds Samsung phones in "Điện thoại".
    public static IQueryable<Product> MatchKeyword(this IQueryable<Product> query, string? keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword)) return query;
        foreach (var word in keyword.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Take(8))
        {
            var pattern = QueryExtensions.Contains(word);
            query = query.Where(p =>
                EF.Functions.ILike(AppDbContext.Unaccent(p.Name), AppDbContext.Unaccent(pattern)) ||
                EF.Functions.ILike(AppDbContext.Unaccent(p.Category.Name), AppDbContext.Unaccent(pattern)));
        }
        return query;
    }
}
