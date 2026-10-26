using Microsoft.EntityFrameworkCore;
using Techies.Web.Data;
using Techies.Web.Domain;
using Techies.Web.ViewModels.Loyalty;

namespace Techies.Web.Services;

public sealed record RedemptionView(Guid Id, string Code, string GiftName, string? GiftImageUrl, long PointsSpent,
    string CustomerName, string CustomerPhone, DateTimeOffset RedeemedAt, GiftRedemptionStatus Status,
    DateTimeOffset? ClaimedAt, string? ClaimedByName);

// BRD §5: gift catalogue and counter claims.
public sealed class GiftService(AppDbContext db, TimeProvider clock)
{
    public Task<List<Gift>> ListAsync(CancellationToken ct = default) =>
        db.Gifts.AsNoTracking().OrderBy(g => g.MinTier).ThenBy(g => g.PointsCost).ToListAsync(ct);

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
