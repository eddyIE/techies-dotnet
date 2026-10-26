using Microsoft.EntityFrameworkCore;
using Techies.Web.Data;
using Techies.Web.Domain;
using Techies.Web.Infrastructure;
using Techies.Web.Infrastructure.Grid;
using Techies.Web.ViewModels.Loyalty;

namespace Techies.Web.Services;

public sealed record RedemptionView(Guid Id, string Code, string GiftName, string? GiftImageUrl, long PointsSpent,
    string CustomerName, string CustomerPhone, DateTimeOffset RedeemedAt, GiftRedemptionStatus Status,
    DateTimeOffset? ClaimedAt, string? ClaimedByName);

// BRD §5: gift catalogue and counter claims.
public sealed class GiftService(AppDbContext db, TimeProvider clock)
{
    public static readonly string[] GridSorts = ["name", "points", "tier", "stock"];

    public async Task<(List<Gift> Rows, int Total)> GridAsync(GridRequest r, CancellationToken ct = default)
    {
        var query = db.Gifts.AsNoTracking();
        if (r.Text("q") is { } q) query = query.Where(g => EF.Functions.ILike(g.Name, QueryExtensions.Contains(q)));
        if (r.Int("tier") is { } tier) query = query.Where(g => g.MinTier == tier);
        if (r.Decimal("points_from") is { } pFrom) query = query.Where(g => g.PointsCost >= pFrom);
        if (r.Decimal("points_to") is { } pTo) query = query.Where(g => g.PointsCost <= pTo);
        if (r.Int("stock_from") is { } sFrom) query = query.Where(g => g.StockAvailable >= sFrom);
        if (r.Int("stock_to") is { } sTo) query = query.Where(g => g.StockAvailable <= sTo);
        if (r.Bool("active") is { } active) query = query.Where(g => g.Active == active);

        query = (r.Sort, r.Desc) switch
        {
            ("name", false) => query.OrderBy(g => g.Name),
            ("name", true) => query.OrderByDescending(g => g.Name),
            ("tier", false) => query.OrderBy(g => g.MinTier).ThenBy(g => g.PointsCost),
            ("tier", true) => query.OrderByDescending(g => g.MinTier).ThenBy(g => g.PointsCost),
            ("stock", false) => query.OrderBy(g => g.StockAvailable).ThenBy(g => g.Name),
            ("stock", true) => query.OrderByDescending(g => g.StockAvailable).ThenBy(g => g.Name),
            (_, true) => query.OrderByDescending(g => g.PointsCost),
            _ => query.OrderBy(g => g.PointsCost),
        };
        return (await query.Skip((r.Page - 1) * r.PageSize).Take(r.PageSize).ToListAsync(ct), await query.CountAsync(ct));
    }

    // "Mở đổi" / "Tạm khóa" a gift.
    public async Task SetActiveAsync(Guid id, bool active, CancellationToken ct = default)
    {
        var gift = await GetAsync(id, ct);
        gift.Active = active;
        gift.UpdatedAt = clock.GetUtcNow();
        await db.SaveChangesAsync(ct);
    }

    public async Task<Gift> GetAsync(Guid id, CancellationToken ct = default) =>
        await db.Gifts.FindAsync([id], ct) ?? throw new DomainException("Không tìm thấy quà tặng.");

    public async Task<Gift> CreateAsync(GiftForm form, string? imageUrl, CancellationToken ct = default)
    {
        var now = clock.GetUtcNow();
        var gift = new Gift { ImageUrl = imageUrl, CreatedAt = now };
        Apply(gift, form, now);
        db.Gifts.Add(gift);
        await db.SaveChangesAsync(ct);
        return gift;
    }

    public async Task UpdateAsync(Guid id, GiftForm form, string? imageUrl, CancellationToken ct = default)
    {
        var gift = await GetAsync(id, ct);
        if (imageUrl is not null) gift.ImageUrl = imageUrl;
        Apply(gift, form, clock.GetUtcNow());
        await db.SaveChangesAsync(ct);
    }

    // Counter lookup of the code a customer shows on their phone.
    public Task<RedemptionView?> FindRedemptionAsync(string code, CancellationToken ct = default)
    {
        var normalized = code.Trim().ToUpperInvariant();
        return db.GiftRedemptions.AsNoTracking()
            .Where(r => r.Code == normalized)
            .Select(r => new RedemptionView(r.Id, r.Code, r.Gift.Name, r.Gift.ImageUrl, r.PointsSpent, r.Customer.FullName,
                r.Customer.Phone, r.RedeemedAt, r.Status, r.ClaimedAt, r.ClaimedByStaff != null ? r.ClaimedByStaff.FullName : null))
            .SingleOrDefaultAsync(ct);
    }

    // Hand-over at the counter. The conditional update makes a double claim impossible even
    // if two staff press the button at once.
    public async Task ClaimAsync(string code, Guid staffId, CancellationToken ct = default)
    {
        var normalized = code.Trim().ToUpperInvariant();
        var now = clock.GetUtcNow();
        var updated = await db.GiftRedemptions
            .Where(r => r.Code == normalized && r.Status == GiftRedemptionStatus.Issued)
            .ExecuteUpdateAsync(u => u
                .SetProperty(r => r.Status, GiftRedemptionStatus.Claimed)
                .SetProperty(r => r.ClaimedAt, now)
                .SetProperty(r => r.ClaimedBy, staffId), ct);
        if (updated == 1) return;
        throw await db.GiftRedemptions.AnyAsync(r => r.Code == normalized, ct)
            ? new DomainException("Mã này đã được nhận quà trước đó.")
            : new DomainException("Không tìm thấy mã nhận quà.");
    }

    private static void Apply(Gift gift, GiftForm form, DateTimeOffset now)
    {
        if (form.PointsCost <= 0) throw new DomainException("Số điểm cần đổi phải lớn hơn 0.");
        if (form.MinTier is < 0 or > 3) throw new DomainException("Hạng tối thiểu phải từ 0 đến 3.");
        if (form.StockAvailable < 0) throw new DomainException("Số lượng quà không được âm.");
        gift.Name = form.Name.Trim();
        gift.Description = string.IsNullOrWhiteSpace(form.Description) ? null : form.Description.Trim();
        gift.PointsCost = form.PointsCost;
        gift.MinTier = form.MinTier;
        gift.StockAvailable = form.StockAvailable;
        gift.Active = form.Active;
        gift.UpdatedAt = now;
    }
}
