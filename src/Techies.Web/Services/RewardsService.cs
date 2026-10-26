using Microsoft.EntityFrameworkCore;
using Techies.Web.Data;
using Techies.Web.Domain;

namespace Techies.Web.Services;

public sealed record GiftOffer(Gift Gift, string? BlockedReason);

public sealed record RewardsOverview(CustomerLoyalty Loyalty, string TierName, string? NextTierName, long? PointsToNextTier,
    List<PointTransaction> History, List<GiftOffer> Gifts, List<RedemptionView> Redemptions);

// The customer's side of loyalty (SPEC shop-extras): points page and gift redemption.
public sealed class RewardsService(AppDbContext db, TimeProvider clock)
{
    public async Task<RewardsOverview> OverviewAsync(Guid customerId, CancellationToken ct = default)
    {
        var loyalty = await db.CustomerLoyalty.AsNoTracking().SingleAsync(l => l.CustomerId == customerId, ct);
        var tiers = await db.MembershipTiers.AsNoTracking().OrderBy(t => t.Tier).ToListAsync(ct);
        var next = tiers.FirstOrDefault(t => t.ThresholdPoints > loyalty.LifetimePoints);
        var history = await db.PointTransactions.AsNoTracking().Where(t => t.CustomerId == customerId)
            .OrderByDescending(t => t.CreatedAt).Take(30).ToListAsync(ct);
        var gifts = await db.Gifts.AsNoTracking().Where(g => g.Active).OrderBy(g => g.PointsCost).ToListAsync(ct);
        var redemptions = await db.GiftRedemptions.AsNoTracking().Where(r => r.CustomerId == customerId)
            .OrderByDescending(r => r.RedeemedAt)
            .Select(r => new RedemptionView(r.Id, r.Code, r.Gift.Name, r.Gift.ImageUrl, r.PointsSpent, r.Customer.FullName,
                r.Customer.Phone, r.RedeemedAt, r.Status, r.ClaimedAt, null))
            .ToListAsync(ct);
        return new RewardsOverview(loyalty, tiers.Single(t => t.Tier == loyalty.Tier).Name, next?.Name,
            next is null ? null : next.ThresholdPoints - loyalty.LifetimePoints, history,
            gifts.Select(g => new GiftOffer(g, BlockedReason(g, loyalty))).ToList(), redemptions);
    }

    // Why the customer can't redeem this gift right now, or null if they can.
    public static string? BlockedReason(Gift gift, CustomerLoyalty loyalty) =>
        !gift.Active ? "Quà này không còn mở đổi."
        : gift.StockAvailable <= 0 ? "Quà đã hết."
        : loyalty.Tier < gift.MinTier ? "Cần đạt hạng cao hơn để đổi quà này."
        : loyalty.Balance < gift.PointsCost
            ? $"Bạn không đủ điểm (cần {(gift.PointsCost - loyalty.Balance).ToString("#,0", Infrastructure.Format.Vietnamese)} điểm nữa)."
        : null;

    // One transaction with the customer's loyalty row and the gift row locked, so two requests
    // can't spend the same points or take the same last gift. Lifetime points (and so the tier)
    // are untouched: only the spendable balance goes down.
    public async Task<GiftRedemption> RedeemAsync(Guid customerId, Guid giftId, CancellationToken ct = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var loyalty = await db.CustomerLoyalty.FromSql($"SELECT * FROM customer_loyalty WHERE customer_id = {customerId} FOR UPDATE")
            .SingleOrDefaultAsync(ct) ?? throw new DomainException("Không tìm thấy tài khoản thành viên.");
        await db.Entry(loyalty).ReloadAsync(ct);
        var gift = await db.Gifts.FromSql($"SELECT * FROM gifts WHERE id = {giftId} FOR UPDATE").SingleOrDefaultAsync(ct)
            ?? throw new DomainException("Không tìm thấy quà tặng.");
        await db.Entry(gift).ReloadAsync(ct);
        if (BlockedReason(gift, loyalty) is { } reason) throw new DomainException(reason);

        var now = clock.GetUtcNow();
        loyalty.Balance -= gift.PointsCost;
        loyalty.UpdatedAt = now;
        gift.StockAvailable--;
        gift.UpdatedAt = now;
        var redemption = new GiftRedemption
        {
            Code = await UniqueCodeAsync(ct), GiftId = gift.Id, CustomerId = customerId, PointsSpent = gift.PointsCost,
            Status = GiftRedemptionStatus.Issued, RedeemedAt = now,
        };
        db.GiftRedemptions.Add(redemption);
        db.PointTransactions.Add(new PointTransaction
        {
            CustomerId = customerId, TransactionType = PointTransactionType.Redeem, PointsDelta = -gift.PointsCost,
            GiftRedemptionId = redemption.Id, Note = $"Đổi quà {gift.Name}", CreatedAt = now,
        });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return redemption;
    }

    private async Task<string> UniqueCodeAsync(CancellationToken ct)
    {
        while (true)
        {
            var code = GiftCodes.New(Random.Shared);
            if (!await db.GiftRedemptions.AnyAsync(r => r.Code == code, ct)) return code;
        }
    }
}

public static class GiftCodes
{
    // GIFT-XXXX-XXXX without look-alike characters (no 0/O, 1/I).
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public static string New(Random random)
    {
        string Part() => new(Enumerable.Range(0, 4).Select(_ => Alphabet[random.Next(Alphabet.Length)]).ToArray());
        return $"GIFT-{Part()}-{Part()}";
    }
}
