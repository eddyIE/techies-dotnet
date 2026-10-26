using Techies.Web.Domain;
using Techies.Web.Services;

namespace Techies.Tests.Services;

[Collection(PostgresCollection.Name)]
public class DashboardQueryTests(PostgresFixture fixture)
{
    private static readonly DateTimeOffset Now = new(2026, 10, 26, 7, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Revenue_sums_completed_orders_by_completion_time_and_compares_with_previous_span()
    {
        await using var db = await fixture.CreateContextAsync();
        TestData.Tiers(db);
        var customer = TestData.Customer(db);
        var product = TestData.Product(db, TestData.Category(db), "Loa", price: 1_000_000);
        void Completed(DateTimeOffset at, OrderStatus status = OrderStatus.Completed)
        {
            var order = TestData.Order(db, customer, product, quantity: 1, status: status); // total 1.000.000 (free shipping)
            order.CompletedAt = at;
        }
        Completed(Now.AddDays(-1));
        Completed(Now.AddDays(-2));
        Completed(Now.AddDays(-1), OrderStatus.Cancelled); // not revenue
        Completed(Now.AddDays(-9));                          // previous 7-day span
        Completed(Now.AddDays(-30));                         // outside both
        await db.SaveChangesAsync();

        var revenue = await new DashboardQueries(db).RevenueAsync(RevenuePeriod.Last7Days, Now);

        Assert.Equal(2_000_000, revenue.Total);
        Assert.Equal(1_000_000, revenue.PreviousTotal);
        Assert.Equal(100.0, revenue.ChangePercent);
        Assert.Equal(7, revenue.Values.Count);
        Assert.Equal(2_000_000, revenue.Values.Sum());
    }

    [Fact]
    public async Task Best_sellers_rank_by_quantity_in_completed_orders()
    {
        await using var db = await fixture.CreateContextAsync();
        TestData.Tiers(db);
        var customer = TestData.Customer(db);
        var category = TestData.Category(db);
        var cable = TestData.Product(db, category, "Cáp sạc", price: 100_000, available: 3);
        var phone = TestData.Product(db, category, "Điện thoại", price: 10_000_000, available: 50);
        TestData.Order(db, customer, cable, quantity: 5, status: OrderStatus.Completed).CompletedAt = Now.AddDays(-1);
        TestData.Order(db, customer, phone, quantity: 2, status: OrderStatus.Completed).CompletedAt = Now.AddDays(-1);
        TestData.Order(db, customer, phone, quantity: 9, status: OrderStatus.Confirmed); // not sold yet
        await db.SaveChangesAsync();

        var best = await new DashboardQueries(db).BestSellersAsync(RevenuePeriod.Last7Days, Now);

        Assert.Equal(["Cáp sạc", "Điện thoại"], best.Select(b => b.Name));
        Assert.Equal((5, 500_000m, 3), (best[0].Quantity, best[0].Revenue, best[0].Available));
    }
}
