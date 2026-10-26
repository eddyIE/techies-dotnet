using Microsoft.EntityFrameworkCore;
using Techies.Web.Data;
using Techies.Web.Domain;
using Techies.Web.Services;

namespace Techies.Tests.Services;

[Collection(PostgresCollection.Name)]
public class CheckoutServiceTests(PostgresFixture fixture)
{
    private static readonly ShippingAddress Address = new("Nguyễn Văn A", "0912345678", "12 Lê Lợi", "Bến Nghé", "Quận 1", "TP. Hồ Chí Minh");

    private static CheckoutService Service(AppDbContext db) => new(db, new StockLedger(db, TimeProvider.System), TimeProvider.System);

    [Fact]
    public async Task Places_confirmed_cod_order_with_snapshots_and_reserves_stock()
    {
        await using var db = await fixture.CreateContextAsync();
        TestData.Tiers(db);
        var customer = TestData.Customer(db);
        var category = TestData.Category(db);
        var phone = TestData.Product(db, category, "Điện thoại", price: 400_000, available: 5);
        await db.SaveChangesAsync();

        var order = await Service(db).PlaceOrderAsync(customer.Id, new Dictionary<Guid, int> { [phone.Id] = 2 }, Address);

        db.ChangeTracker.Clear();
        var saved = await db.Orders.Include(o => o.Items).SingleAsync();
        Assert.Equal(order.Id, saved.Id);
        Assert.Equal((OrderStatus.Confirmed, PaymentMethod.Cod, PaymentStatus.Pending), (saved.Status, saved.PaymentMethod, saved.PaymentStatus));
        Assert.Equal((800_000m, 0m, 800_000m), (saved.Subtotal, saved.ShippingFee, saved.Total)); // free shipping from 500k
        Assert.Equal(("Điện thoại", 400_000m, 2), (saved.Items[0].ProductName, saved.Items[0].UnitPrice, saved.Items[0].Quantity));
        Assert.Equal("12 Lê Lợi", saved.ShipLine1);
        Assert.Equal(3, (await db.StockItems.SingleAsync()).Available);
        var adj = await db.InventoryAdjustments.SingleAsync();
        Assert.Equal((AdjustmentType.Order, -2, (Guid?)order.Id), (adj.AdjustmentType, adj.QuantityDelta, adj.OrderId));
    }

    [Fact]
    public async Task Insufficient_stock_creates_nothing_and_names_the_product()
    {
        await using var db = await fixture.CreateContextAsync();
        TestData.Tiers(db);
        var customer = TestData.Customer(db);
        var category = TestData.Category(db);
        var plenty = TestData.Product(db, category, "Ốp lưng", available: 10);
        var scarce = TestData.Product(db, category, "Tai nghe", available: 1);
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<DomainException>(() => Service(db).PlaceOrderAsync(customer.Id,
            new Dictionary<Guid, int> { [plenty.Id] = 3, [scarce.Id] = 2 }, Address));

        Assert.Contains("Tai nghe", ex.Message);
        db.ChangeTracker.Clear();
        Assert.Equal(0, await db.Orders.CountAsync());
        Assert.Equal(0, await db.InventoryAdjustments.CountAsync());
        Assert.Equal(10, (await db.StockItems.SingleAsync(s => s.ProductId == plenty.Id)).Available);
    }

    [Fact]
    public async Task Inactive_product_cannot_be_checked_out()
    {
        await using var db = await fixture.CreateContextAsync();
        TestData.Tiers(db);
        var customer = TestData.Customer(db);
        var hidden = TestData.Product(db, TestData.Category(db), "Ngừng bán", active: false);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<DomainException>(() =>
            Service(db).PlaceOrderAsync(customer.Id, new Dictionary<Guid, int> { [hidden.Id] = 1 }, Address));
    }

    [Fact]
    public async Task Concurrent_checkouts_for_the_last_unit_sell_it_once()
    {
        await using var db = await fixture.CreateContextAsync();
        TestData.Tiers(db);
        var lan = TestData.Customer(db, "lan@example.com");
        var minh = TestData.Customer(db, "minh@example.com");
        var last = TestData.Product(db, TestData.Category(db), "Hàng cuối", available: 1);
        await db.SaveChangesAsync();
        await using var db1 = PostgresFixture.CreateSibling(db);
        await using var db2 = PostgresFixture.CreateSibling(db);

        var results = await Task.WhenAll(
            Attempt(Service(db1).PlaceOrderAsync(lan.Id, new Dictionary<Guid, int> { [last.Id] = 1 }, Address)),
            Attempt(Service(db2).PlaceOrderAsync(minh.Id, new Dictionary<Guid, int> { [last.Id] = 1 }, Address)));

        Assert.Equal(1, results.Count(ok => ok));
        Assert.Equal(0, (await db.StockItems.AsNoTracking().SingleAsync()).Available);
        Assert.Equal(1, await db.Orders.CountAsync());
    }

    private static async Task<bool> Attempt(Task task)
    {
        try { await task; return true; }
        catch (DomainException) { return false; }
    }
}
