using Microsoft.EntityFrameworkCore;
using Techies.Web.Data;
using Techies.Web.Domain;
using Techies.Web.Services;

namespace Techies.Tests.Services;

[Collection(PostgresCollection.Name)]
public class CheckoutCouponTests(PostgresFixture fixture)
{
    private static readonly ShippingAddress Address = new("Nguyễn Văn A", "0912345678", "12 Lê Lợi", "Bến Nghé", "Quận 1", "TP. Hồ Chí Minh");

    private static CheckoutService Service(AppDbContext db) =>
        new(db, new StockLedger(db, TimeProvider.System), new CustomerAccountService(db, TimeProvider.System), TimeProvider.System);

    private static async Task<(User Customer, Product Product)> ArrangeAsync(AppDbContext db, decimal price = 300_000)
    {
        TestData.Tiers(db);
        var customer = TestData.Customer(db);
        var product = TestData.Product(db, TestData.Category(db), "Tai nghe", price: price, available: 10);
        db.Coupons.AddRange(
            new Coupon { Code = "TECHIES50K", DiscountAmount = 50_000, MinSubtotal = 500_000 },
            new Coupon { Code = "BIG100K", DiscountAmount = 100_000, MinSubtotal = 0 },
            new Coupon { Code = "PAUSED200K", DiscountAmount = 200_000, MinSubtotal = 0, Active = false });
        await db.SaveChangesAsync();
        return (customer, product);
    }

    [Fact]
    public async Task Valid_coupon_discounts_the_order_and_records_the_redemption()
    {
        await using var db = await fixture.CreateContextAsync();
        var (customer, product) = await ArrangeAsync(db);

        var order = await Service(db).PlaceOrderAsync(customer.Id, new Dictionary<Guid, int> { [product.Id] = 2 }, Address, " techies50k ");

        db.ChangeTracker.Clear();
        var saved = await db.Orders.SingleAsync();
        Assert.Equal(("TECHIES50K", 600_000m, 0m, 50_000m, 550_000m), (saved.CouponCode, saved.Subtotal, saved.ShippingFee, saved.Discount, saved.Total));
        var redemption = await db.CouponRedemptions.SingleAsync();
        Assert.Equal((order.Id, customer.Id, 50_000m, CouponRedemptionStatus.Applied),
            (redemption.OrderId, redemption.CustomerId, redemption.DiscountAmount, redemption.Status));
    }

    [Theory]
    [InlineData("TECHIES50K", "tối thiểu")] // 300.000 < 500.000
    [InlineData("PAUSED200K", "tạm dừng")]
    [InlineData("NOPE", "không tồn tại")]
    public async Task Unusable_coupon_creates_nothing_and_says_why(string code, string reason)
    {
        await using var db = await fixture.CreateContextAsync();
        var (customer, product) = await ArrangeAsync(db);

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            Service(db).PlaceOrderAsync(customer.Id, new Dictionary<Guid, int> { [product.Id] = 1 }, Address, code));

        Assert.Contains(reason, ex.Message);
        db.ChangeTracker.Clear();
        Assert.Equal(0, await db.Orders.CountAsync());
        Assert.Equal(10, (await db.StockItems.SingleAsync()).Available);
    }

    [Fact]
    public async Task Discount_never_exceeds_the_subtotal()
    {
        await using var db = await fixture.CreateContextAsync();
        var (customer, product) = await ArrangeAsync(db, price: 60_000);

        await Service(db).PlaceOrderAsync(customer.Id, new Dictionary<Guid, int> { [product.Id] = 1 }, Address, "BIG100K");

        var saved = await db.Orders.AsNoTracking().SingleAsync();
        Assert.Equal((60_000m, 30_000m), (saved.Discount, saved.Total)); // pays shipping only
    }

    [Fact]
    public async Task Customer_cancel_restores_the_coupon_redemption()
    {
        await using var db = await fixture.CreateContextAsync();
        var (customer, product) = await ArrangeAsync(db);
        var order = await Service(db).PlaceOrderAsync(customer.Id, new Dictionary<Guid, int> { [product.Id] = 2 }, Address, "TECHIES50K");
        var orders = new OrderService(db, new LoyaltyService(db, TimeProvider.System), new StockLedger(db, TimeProvider.System), TimeProvider.System);

        await orders.CancelByCustomerAsync(order.Id, customer.Id);

        db.ChangeTracker.Clear();
        Assert.Equal(CouponRedemptionStatus.Restored, (await db.CouponRedemptions.SingleAsync()).Status);
    }

    [Fact]
    public async Task Preview_reports_discount_or_reason_for_the_cart_subtotal()
    {
        await using var db = await fixture.CreateContextAsync();
        await ArrangeAsync(db);
        var coupons = new CouponService(db, TimeProvider.System);

        var ok = await coupons.CheckAsync("techies50k", subtotal: 800_000);
        var low = await coupons.CheckAsync("TECHIES50K", subtotal: 100_000);

        Assert.Equal((true, 50_000m, "TECHIES50K"), (ok.Applicable, ok.Discount, ok.Code));
        Assert.False(low.Applicable);
        Assert.Contains("500.000đ", low.Message);
    }
}
