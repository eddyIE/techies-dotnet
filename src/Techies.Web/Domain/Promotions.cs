namespace Techies.Web.Domain;

// Fixed-amount coupon (BRD §4). Percentage discounts belong to tier vouchers, not coupons.
public sealed class Coupon
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Code { get; set; } = "";
    public string? Description { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal MinSubtotal { get; set; }
    public bool Active { get; set; } = true;
    public DateTimeOffset? StartsAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public enum CouponStatus { Running, Paused, NotStarted, Expired }
