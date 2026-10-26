namespace Techies.Web.Domain;

public sealed class MembershipTier
{
    public short Tier { get; set; }
    public string Name { get; set; } = "";
    public long ThresholdPoints { get; set; }
    public decimal DiscountPercent { get; set; }
}

public sealed class CustomerLoyalty
{
    public Guid CustomerId { get; set; }
    public long LifetimePoints { get; set; }
    public long Balance { get; set; }
    public short Tier { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

// Accessory customers exchange points for and collect at the counter.
public sealed class Gift
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public long PointsCost { get; set; }
    public short MinTier { get; set; }
    public int StockAvailable { get; set; }
    public bool Active { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public enum GiftRedemptionStatus { Issued, Claimed }

// One exchange of points for a gift. The customer shows Code (GIFT-XXXX-XXXX) at the counter.
public sealed class GiftRedemption
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Code { get; set; } = "";
    public Guid GiftId { get; set; }
    public Gift Gift { get; set; } = null!;
    public Guid CustomerId { get; set; }
    public User Customer { get; set; } = null!;
    public long PointsSpent { get; set; }
    public GiftRedemptionStatus Status { get; set; } = GiftRedemptionStatus.Issued;
    public DateTimeOffset RedeemedAt { get; set; }
    public DateTimeOffset? ClaimedAt { get; set; }
    public Guid? ClaimedBy { get; set; }
    public Staff? ClaimedByStaff { get; set; }
}
