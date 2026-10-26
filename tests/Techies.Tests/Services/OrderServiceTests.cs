using Microsoft.EntityFrameworkCore;
using Techies.Web.Data;
using Techies.Web.Domain;
using Techies.Web.Services;

namespace Techies.Tests.Services;

[Collection(PostgresCollection.Name)]
public class OrderServiceTests(PostgresFixture fixture)
{
    private static OrderService Service(AppDbContext db) =>
        new(db, new LoyaltyService(db, TimeProvider.System), new StockLedger(db, TimeProvider.System), TimeProvider.System);

    private static async Task<(User Customer, Product Product)> ArrangeAsync(AppDbContext db, decimal price = 1_250_000, int stock = 8)
    {
        TestData.Tiers(db);
        var customer = TestData.Customer(db);
        var product = TestData.Product(db, TestData.Category(db), "Tai nghe", price: price, available: stock);
        await db.SaveChangesAsync();
        return (customer, product);
    }

    // ---- Complete ----

    [Fact]
    public async Task Complete_marks_order_completed_and_cod_paid()
    {
        await using var db = await fixture.CreateContextAsync();
        var (customer, product) = await ArrangeAsync(db);
        var order = TestData.Order(db, customer, product);
        await db.SaveChangesAsync();

        await Service(db).CompleteAsync(order.Id);

        db.ChangeTracker.Clear();
        var saved = await db.Orders.SingleAsync();
        Assert.Equal(OrderStatus.Completed, saved.Status);
        Assert.Equal(PaymentStatus.Paid, saved.PaymentStatus);
        Assert.NotNull(saved.CompletedAt);
    }

    [Fact]
    public async Task Complete_awards_one_point_per_1000_of_subtotal_minus_discount()
    {
        await using var db = await fixture.CreateContextAsync();
        var (customer, product) = await ArrangeAsync(db);
        var order = TestData.Order(db, customer, product, quantity: 2, discount: 50_000, couponCode: "TECHIES50K"); // 2.500.000 - 50.000
        await db.SaveChangesAsync();

        await Service(db).CompleteAsync(order.Id);

        db.ChangeTracker.Clear();
        var loyalty = await db.CustomerLoyalty.SingleAsync();
        Assert.Equal(2_450, loyalty.LifetimePoints);
        Assert.Equal(2_450, loyalty.Balance);
        var earn = await db.PointTransactions.SingleAsync();
        Assert.Equal((PointTransactionType.Earn, 2_450L, (Guid?)order.Id), (earn.TransactionType, earn.PointsDelta, earn.OrderId));
    }

    [Fact]
    public async Task Complete_upgrades_tier_when_threshold_crossed()
    {
        await using var db = await fixture.CreateContextAsync();
        var (customer, product) = await ArrangeAsync(db, price: 10_000_000);
        var order = TestData.Order(db, customer, product, quantity: 1);
        await db.SaveChangesAsync();

        await Service(db).CompleteAsync(order.Id);

        db.ChangeTracker.Clear();
        Assert.Equal(1, (await db.CustomerLoyalty.SingleAsync()).Tier);
    }

    [Fact]
    public async Task Awarding_the_same_order_twice_adds_points_once()
    {
        await using var db = await fixture.CreateContextAsync();
        var (customer, product) = await ArrangeAsync(db);
        var order = TestData.Order(db, customer, product, status: OrderStatus.Completed);
        await db.SaveChangesAsync();
        var loyalty = new LoyaltyService(db, TimeProvider.System);

        await using (var tx = await db.Database.BeginTransactionAsync())
        {
            await loyalty.AwardForOrderAsync(order);
            await db.SaveChangesAsync();
            await loyalty.AwardForOrderAsync(order);
            await db.SaveChangesAsync();
            await tx.CommitAsync();
        }

        db.ChangeTracker.Clear();
        Assert.Equal(1, await db.PointTransactions.CountAsync());
        Assert.Equal(2_500, (await db.CustomerLoyalty.SingleAsync()).LifetimePoints);
    }

    [Theory]
    [InlineData(OrderStatus.AwaitingPayment)]
    [InlineData(OrderStatus.Completed)]
    [InlineData(OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Failed)]
    public async Task Complete_rejects_orders_not_confirmed(OrderStatus status)
    {
        await using var db = await fixture.CreateContextAsync();
        var (customer, product) = await ArrangeAsync(db);
        var order = TestData.Order(db, customer, product, status: status);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<DomainException>(() => Service(db).CompleteAsync(order.Id));
    }

    // ---- Cancel ----

    [Fact]
    public async Task Cancel_returns_items_to_stock_with_cancel_adjustment()
    {
        await using var db = await fixture.CreateContextAsync();
        var (customer, product) = await ArrangeAsync(db, stock: 8);
        var order = TestData.Order(db, customer, product, quantity: 2);
        await db.SaveChangesAsync();

        await Service(db).CancelByStaffAsync(order.Id, staffId: null);

        db.ChangeTracker.Clear();
        Assert.Equal(OrderStatus.Cancelled, (await db.Orders.SingleAsync()).Status);
        Assert.Equal(10, (await db.StockItems.SingleAsync()).Available);
        var adj = await db.InventoryAdjustments.SingleAsync();
        Assert.Equal((AdjustmentType.Cancel, 2, (Guid?)order.Id), (adj.AdjustmentType, adj.QuantityDelta, adj.OrderId));
    }

    [Fact]
    public async Task Cancel_restores_applied_coupon_and_refunds_paid_order()
    {
        await using var db = await fixture.CreateContextAsync();
        var (customer, product) = await ArrangeAsync(db);
        var coupon = new Coupon { Code = "TECHIES50K", DiscountAmount = 50_000, MinSubtotal = 0 };
        db.Coupons.Add(coupon);
        var order = TestData.Order(db, customer, product, method: PaymentMethod.MockCard, payment: PaymentStatus.Paid,
            discount: 50_000, couponCode: coupon.Code);
        db.CouponRedemptions.Add(new CouponRedemption { CouponId = coupon.Id, OrderId = order.Id, CustomerId = customer.Id, DiscountAmount = 50_000 });
        await db.SaveChangesAsync();

        await Service(db).CancelByStaffAsync(order.Id, staffId: null);

        db.ChangeTracker.Clear();
        Assert.Equal(PaymentStatus.Refunded, (await db.Orders.SingleAsync()).PaymentStatus);
        var redemption = await db.CouponRedemptions.SingleAsync();
        Assert.Equal(CouponRedemptionStatus.Restored, redemption.Status);
        Assert.NotNull(redemption.RestoredAt);
    }

    [Fact]
    public async Task Cancel_keeps_unpaid_cod_payment_pending()
    {
        await using var db = await fixture.CreateContextAsync();
        var (customer, product) = await ArrangeAsync(db);
        var order = TestData.Order(db, customer, product, method: PaymentMethod.Cod, payment: PaymentStatus.Pending);
        await db.SaveChangesAsync();

        await Service(db).CancelByStaffAsync(order.Id, staffId: null);

        db.ChangeTracker.Clear();
        Assert.Equal(PaymentStatus.Pending, (await db.Orders.SingleAsync()).PaymentStatus);
    }

    [Theory]
    [InlineData(OrderStatus.AwaitingPayment)]
    [InlineData(OrderStatus.Completed)]
    [InlineData(OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Failed)]
    public async Task Cancel_rejects_orders_not_confirmed(OrderStatus status)
    {
        await using var db = await fixture.CreateContextAsync();
        var (customer, product) = await ArrangeAsync(db, stock: 8);
        var order = TestData.Order(db, customer, product, status: status);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<DomainException>(() => Service(db).CancelByStaffAsync(order.Id, staffId: null));
        db.ChangeTracker.Clear();
        Assert.Equal(8, (await db.StockItems.SingleAsync()).Available);
    }
}
