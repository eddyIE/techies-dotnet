using Techies.Web.Domain;
using Techies.Web.Services;
using Techies.Web.ViewModels.Orders;

namespace Techies.Tests.Services;

[Collection(PostgresCollection.Name)]
public class OrderQueryTests(PostgresFixture fixture)
{
    [Fact]
    public async Task List_filters_by_status_and_counts_every_status()
    {
        await using var db = await fixture.CreateContextAsync();
        TestData.Tiers(db);
        var customer = TestData.Customer(db);
        var product = TestData.Product(db, TestData.Category(db), "Loa");
        TestData.Order(db, customer, product, status: OrderStatus.Confirmed);
        TestData.Order(db, customer, product, status: OrderStatus.Confirmed);
        TestData.Order(db, customer, product, status: OrderStatus.Failed);
        await db.SaveChangesAsync();
        var queries = new OrderQueries(db);

        var page = await queries.ListAsync(new OrderFilter { Status = OrderStatus.Confirmed });
        var counts = await queries.CountsByStatusAsync();

        Assert.Equal(2, page.Total);
        Assert.Equal(2, counts[OrderStatus.Confirmed]);
        Assert.Equal(1, counts[OrderStatus.Failed]);
        Assert.Equal(0, counts[OrderStatus.Completed]);
    }

    [Fact]
    public async Task Search_matches_order_ref_or_recipient_phone()
    {
        await using var db = await fixture.CreateContextAsync();
        TestData.Tiers(db);
        var lan = TestData.Customer(db, "lan@example.com");
        var minh = TestData.Customer(db, "minh@example.com");
        minh.Phone = "0987654321";
        var product = TestData.Product(db, TestData.Category(db), "Loa");
        var lanOrder = TestData.Order(db, lan, product);
        var minhOrder = TestData.Order(db, minh, product);
        await db.SaveChangesAsync();
        var queries = new OrderQueries(db);

        var byRef = await queries.ListAsync(new OrderFilter { Q = lanOrder.OrderRef.ToLowerInvariant() });
        var byPhone = await queries.ListAsync(new OrderFilter { Q = "0987654321" });

        Assert.Equal([lanOrder.Id], byRef.Items.Select(o => o.Id));
        Assert.Equal([minhOrder.Id], byPhone.Items.Select(o => o.Id));
    }
}
