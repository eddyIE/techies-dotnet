using Microsoft.EntityFrameworkCore;
using Techies.Web.Data;
using Techies.Web.Domain;
using Techies.Web.Services;

namespace Techies.Tests.Services;

[Collection(PostgresCollection.Name)]
public class MyOrdersTests(PostgresFixture fixture)
{
    private static OrderService Orders(AppDbContext db) =>
        new(db, new LoyaltyService(db, TimeProvider.System), new StockLedger(db, TimeProvider.System), TimeProvider.System);

    [Fact]
    public async Task Customer_sees_only_their_own_orders()
    {
        await using var db = await fixture.CreateContextAsync();
        TestData.Tiers(db);
        var lan = TestData.Customer(db, "lan@example.com");
        var minh = TestData.Customer(db, "minh@example.com");
        var product = TestData.Product(db, TestData.Category(db), "Loa");
        var lanOrder = TestData.Order(db, lan, product);
        var minhOrder = TestData.Order(db, minh, product);
        await db.SaveChangesAsync();
        var queries = new OrderQueries(db);

        var lanList = await queries.ForCustomerAsync(lan.Id, page: 1);

        Assert.Equal([lanOrder.Id], lanList.Items.Select(o => o.Id));
        Assert.NotNull(await queries.ForCustomerDetailAsync(lan.Id, lanOrder.Id));
        Assert.Null(await queries.ForCustomerDetailAsync(lan.Id, minhOrder.Id));
    }

    [Fact]
    public async Task Customer_cancelling_own_confirmed_order_restores_stock()
    {
        await using var db = await fixture.CreateContextAsync();
        TestData.Tiers(db);
        var lan = TestData.Customer(db);
        var product = TestData.Product(db, TestData.Category(db), "Loa", available: 4);
        var order = TestData.Order(db, lan, product, quantity: 1);
        await db.SaveChangesAsync();

        await Orders(db).CancelByCustomerAsync(order.Id, lan.Id);

        db.ChangeTracker.Clear();
        Assert.Equal(OrderStatus.Cancelled, (await db.Orders.SingleAsync()).Status);
        Assert.Equal(5, (await db.StockItems.SingleAsync()).Available);
    }

    [Fact]
    public async Task Customer_cannot_cancel_someone_elses_order()
    {
        await using var db = await fixture.CreateContextAsync();
        TestData.Tiers(db);
        var lan = TestData.Customer(db, "lan@example.com");
        var minh = TestData.Customer(db, "minh@example.com");
        var product = TestData.Product(db, TestData.Category(db), "Loa", available: 4);
        var minhOrder = TestData.Order(db, minh, product);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<DomainException>(() => Orders(db).CancelByCustomerAsync(minhOrder.Id, lan.Id));
        db.ChangeTracker.Clear();
        Assert.Equal(OrderStatus.Confirmed, (await db.Orders.SingleAsync()).Status);
    }
}
