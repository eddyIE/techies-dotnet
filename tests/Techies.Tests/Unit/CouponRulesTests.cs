using Techies.Web.Domain;
using Techies.Web.Services;

namespace Techies.Tests.Unit;

public class CouponRulesTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 26, 12, 0, 0, TimeSpan.Zero);

    private static Coupon Coupon(bool active = true, DateTimeOffset? starts = null, DateTimeOffset? expires = null,
        decimal discount = 50_000, decimal min = 500_000) =>
        new() { Code = "TECHIES50K", DiscountAmount = discount, MinSubtotal = min, Active = active, StartsAt = starts, ExpiresAt = expires };

    [Fact]
    public void Status_is_running_inside_its_window() =>
        Assert.Equal(CouponStatus.Running, CouponService.StatusAt(Coupon(starts: Now.AddDays(-1), expires: Now.AddDays(1)), Now));

    [Fact]
    public void Status_is_paused_when_inactive_even_inside_window() =>
        Assert.Equal(CouponStatus.Paused, CouponService.StatusAt(Coupon(active: false, expires: Now.AddDays(1)), Now));

    [Fact]
    public void Status_is_not_started_before_start() =>
        Assert.Equal(CouponStatus.NotStarted, CouponService.StatusAt(Coupon(starts: Now.AddMinutes(1)), Now));

    [Fact]
    public void Status_is_expired_at_expiry_instant() =>
        Assert.Equal(CouponStatus.Expired, CouponService.StatusAt(Coupon(expires: Now), Now));

    [Fact]
    public void Evaluate_gives_fixed_discount_when_minimum_met()
    {
        var result = CouponService.Evaluate(Coupon(), subtotal: 500_000, Now);

        Assert.True(result.Applicable);
        Assert.Equal(50_000, result.Discount);
    }

    [Fact]
    public void Evaluate_rejects_subtotal_below_minimum()
    {
        var result = CouponService.Evaluate(Coupon(), subtotal: 499_999, Now);

        Assert.False(result.Applicable);
        Assert.Equal(0, result.Discount);
        Assert.Equal("COUPON_NOT_APPLICABLE", result.Reason);
    }

    [Fact]
    public void Evaluate_rejects_paused_coupon() =>
        Assert.Equal("COUPON_NOT_APPLICABLE", CouponService.Evaluate(Coupon(active: false), 1_000_000, Now).Reason);

    [Fact]
    public void Evaluate_caps_discount_at_subtotal()
    {
        var result = CouponService.Evaluate(Coupon(discount: 100_000, min: 0), subtotal: 60_000, Now);

        Assert.Equal(60_000, result.Discount);
    }
}
