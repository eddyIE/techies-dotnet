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
