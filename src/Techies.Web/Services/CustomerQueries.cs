using Microsoft.EntityFrameworkCore;
using Techies.Web.Data;
using Techies.Web.Domain;
using Techies.Web.Infrastructure;
using Techies.Web.Infrastructure.Grid;
using Techies.Web.ViewModels;
using Techies.Web.ViewModels.Customers;

namespace Techies.Web.Services;

// BRD §6: staff look customers up read-only; the only thing they may change is lock status.
public sealed class CustomerQueries(AppDbContext db, TimeProvider clock)
{
    public static readonly string[] GridSorts = ["name", "email", "balance", "lifetime", "created"];

    public async Task<(List<CustomerRow> Rows, int Total)> GridAsync(GridRequest r, CancellationToken ct = default)
    {
        var query = db.Users.AsNoTracking();
        if (r.Text("q") is { } q)
        {
            var pattern = QueryExtensions.Contains(q);
            query = query.Where(u => EF.Functions.ILike(u.Email, pattern) || EF.Functions.ILike(u.FullName, pattern)
                || EF.Functions.ILike(u.Phone, pattern));
        }
        if (r.Int("tier") is { } tier) query = query.Where(u => u.Loyalty!.Tier == tier);
        if (r.Enum<AccountStatus>("status") is { } status) query = query.Where(u => u.Status == status);

        query = (r.Sort, r.Desc) switch
        {
            ("email", false) => query.OrderBy(u => u.Email),
            ("email", true) => query.OrderByDescending(u => u.Email),
            ("balance", false) => query.OrderBy(u => u.Loyalty!.Balance).ThenBy(u => u.FullName),
            ("balance", true) => query.OrderByDescending(u => u.Loyalty!.Balance).ThenBy(u => u.FullName),
            ("lifetime", false) => query.OrderBy(u => u.Loyalty!.LifetimePoints).ThenBy(u => u.FullName),
            ("lifetime", true) => query.OrderByDescending(u => u.Loyalty!.LifetimePoints).ThenBy(u => u.FullName),
            ("created", false) => query.OrderBy(u => u.CreatedAt),
            ("created", true) => query.OrderByDescending(u => u.CreatedAt),
            (_, true) => query.OrderByDescending(u => u.FullName),
            _ => query.OrderBy(u => u.FullName),
        };
        var total = await query.CountAsync(ct);
        var rows = await query.Skip((r.Page - 1) * r.PageSize).Take(r.PageSize)
            .Select(u => new CustomerRow(u.Id, u.FullName, u.Email, u.Phone, u.Loyalty!.Tier, u.Loyalty.Balance, u.Status))
            .ToListAsync(ct);
        return (rows, total);
    }

    public async Task<CustomerProfile> ProfileAsync(Guid id, CancellationToken ct = default)
    {
        var user = await db.Users.AsNoTracking().Include(u => u.Loyalty).SingleOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new DomainException("Không tìm thấy khách hàng.");
        var loyalty = user.Loyalty ?? new CustomerLoyalty { CustomerId = id };
        var tierName = await db.MembershipTiers.Where(t => t.Tier == loyalty.Tier).Select(t => t.Name).SingleAsync(ct);
        var addresses = await db.Addresses.AsNoTracking().Where(a => a.UserId == id).OrderByDescending(a => a.IsDefault).ToListAsync(ct);
        var points = await db.PointTransactions.AsNoTracking().Where(t => t.CustomerId == id)
            .OrderByDescending(t => t.CreatedAt).Take(50).ToListAsync(ct);
        var orders = await db.Orders.AsNoTracking().Where(o => o.UserId == id).OrderByDescending(o => o.CreatedAt)
            .Select(OrderQueries.ToRow).ToListAsync(ct);
        return new CustomerProfile(user, loyalty, tierName, addresses, points, orders);
    }

    // "Khóa / Mở khóa" for accounts spamming fake orders (BRD §6). Explicit target status so a
    // mass action over mixed rows does what its label says.
    public async Task SetStatusAsync(Guid id, AccountStatus status, CancellationToken ct = default)
    {
        var user = await db.Users.FindAsync([id], ct) ?? throw new DomainException("Không tìm thấy khách hàng.");
        user.Status = status;
        user.UpdatedAt = clock.GetUtcNow();
        await db.SaveChangesAsync(ct);
    }
}
