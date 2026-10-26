using Microsoft.EntityFrameworkCore;
using Techies.Web.ViewModels;

namespace Techies.Web.Infrastructure;

public static class QueryExtensions
{
    public static async Task<Paged<T>> ToPagedAsync<T>(this IQueryable<T> query, int page, int pageSize = 20,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new Paged<T>(items, page, pageSize, total);
    }

    // ILIKE pattern for "contains", with the user's own % and _ treated literally.
    public static string Contains(string text) =>
        "%" + text.Trim().Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_") + "%";
}
