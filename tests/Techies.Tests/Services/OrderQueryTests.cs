using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Techies.Web.Data;
using Techies.Web.Domain;
using Techies.Web.Infrastructure.Grid;
using Techies.Web.Services;

namespace Techies.Tests.Services;

[Collection(PostgresCollection.Name)]
public class OrderQueryTests(PostgresFixture fixture)
{
    private static GridRequest Request(string query) =>
        GridRequest.From("/admin/orders", new QueryString(query), OrderQueries.GridSorts, "created", true);

    [Fact]
    public async Task Grid_filters_by_status_and_counts_every_status()
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

        var (_, total) = await queries.GridAsync(Request("?status=Confirmed"));
        var counts = await queries.CountsByStatusAsync();

        Assert.Equal(2, total);
        Assert.Equal((2, 1, 0), (counts[OrderStatus.Confirmed], counts[OrderStatus.Failed], counts[OrderStatus.Completed]));
    }

    [Fact]
    public async Task Keyword_matches_order_ref_or_recipient_phone()
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

        var (byRef, _) = await queries.GridAsync(Request("?q=" + lanOrder.OrderRef.ToLowerInvariant()));
        var (byPhone, _) = await queries.GridAsync(Request("?q=0987654321"));

        Assert.Equal([lanOrder.Id], byRef.Select(o => o.Id));
        Assert.Equal([minhOrder.Id], byPhone.Select(o => o.Id));
    }

    [Fact]
    public async Task Total_range_payment_filters_and_date_range_in_vietnam_time()
    {
        await using var db = await fixture.CreateContextAsync();
        TestData.Tiers(db);
        var customer = TestData.Customer(db);
        var category = TestData.Category(db);
        var cheap = TestData.Product(db, category, "Cáp", price: 100_000);
        var dear = TestData.Product(db, category, "Máy", price: 9_000_000);
        var a = TestData.Order(db, customer, dear, quantity: 1, method: PaymentMethod.MockCard, payment: PaymentStatus.Paid);
        var b = TestData.Order(db, customer, cheap, quantity: 1);
        var c = TestData.Order(db, customer, dear, quantity: 1);
        // 25/10 23:30 in Vietnam is still 25/10 16:30 UTC; 26/10 00:30 Vietnam is 25/10 17:30 UTC.
        a.CreatedAt = new DateTimeOffset(2026, 10, 25, 16, 30, 0, TimeSpan.Zero);
        b.CreatedAt = new DateTimeOffset(2026, 10, 25, 16, 30, 0, TimeSpan.Zero);
        c.CreatedAt = new DateTimeOffset(2026, 10, 25, 17, 30, 0, TimeSpan.Zero);
        await db.SaveChangesAsync();
        var queries = new OrderQueries(db);

        var (onThe25th, _) = await queries.GridAsync(Request("?created_from=2026-10-25&created_to=2026-10-25"));
        var (bigCard, _) = await queries.GridAsync(Request("?total_from=1000000&method=MockCard&payment=Paid"));

        Assert.Equal(new[] { a.Id, b.Id }.Order(), onThe25th.Select(o => o.Id).Order());
        Assert.Equal([a.Id], bigCard.Select(o => o.Id));
    }

    [Fact]
    public async Task Mass_complete_reports_orders_that_were_not_confirmed()
    {
        await using var db = await fixture.CreateContextAsync();
        TestData.Tiers(db);
        var customer = TestData.Customer(db);
        var product = TestData.Product(db, TestData.Category(db), "Loa");
        var ok = TestData.Order(db, customer, product, status: OrderStatus.Confirmed);
        var done = TestData.Order(db, customer, product, status: OrderStatus.Completed);
        await db.SaveChangesAsync();
        var orders = new OrderService(db, new LoyaltyService(db, TimeProvider.System), new StockLedger(db, TimeProvider.System), TimeProvider.System);

        var result = await Bulk.RunAsync([ok.Id, done.Id], id => orders.CompleteAsync(id));

        Assert.Equal("Đã hoàn tất 1 / 2. Bỏ qua: Chỉ hoàn tất được đơn đang ở trạng thái Đã xác nhận.", result.Summary("hoàn tất"));
        db.ChangeTracker.Clear();
        Assert.Equal(OrderStatus.Completed, (await db.Orders.SingleAsync(o => o.Id == ok.Id)).Status);
    }
}
