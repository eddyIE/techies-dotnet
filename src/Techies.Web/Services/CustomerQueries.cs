using Microsoft.EntityFrameworkCore;
using Techies.Web.Data;
using Techies.Web.Domain;
using Techies.Web.Infrastructure;
using Techies.Web.ViewModels;
using Techies.Web.ViewModels.Customers;

namespace Techies.Web.Services;

// BRD §6: staff look customers up read-only; the only thing they may change is lock status.
public sealed class CustomerQueries(AppDbContext db)
{
    public Task<Paged<CustomerRow>> SearchAsync(string? q, int page, CancellationToken ct = default)
    {
        var query = db.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var pattern = QueryExtensions.Contains(q);
            query = query.Where(u => EF.Functions.ILike(u.Email, pattern) || EF.Functions.ILike(u.FullName, pattern)
                || EF.Functions.ILike(u.Phone, pattern));
        }
        return query.OrderBy(u => u.FullName)
            .Select(u => new CustomerRow(u.Id, u.FullName, u.Email, u.Phone, u.Loyalty!.Tier, u.Loyalty.Balance, u.Status))
            .ToPagedAsync(page, ct: ct);
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

    // "Khóa / Mở khóa" for accounts spamming fake orders. Returns the new status.
    public async Task<AccountStatus> ToggleLockAsync(Guid id, CancellationToken ct = default)
    {
        var user = await db.Users.FindAsync([id], ct) ?? throw new DomainException("Không tìm thấy khách hàng.");
        user.Status = user.Status == AccountStatus.Active ? AccountStatus.Locked : AccountStatus.Active;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return user.Status;
    }
}
