using Microsoft.EntityFrameworkCore;
using Techies.Web.Data;
using Techies.Web.Domain;
using Techies.Web.Services;

namespace Techies.Tests.Services;

[Collection(PostgresCollection.Name)]
public class ReviewServiceTests(PostgresFixture fixture)
{
    private static async Task<(User Customer, Product Product, Order Order)> ArrangeAsync(AppDbContext db, OrderStatus status = OrderStatus.Completed)
    {
        TestData.Tiers(db);
        var customer = TestData.Customer(db);
        var product = TestData.Product(db, TestData.Category(db), "Loa");
        var order = TestData.Order(db, customer, product, status: status);
        await db.SaveChangesAsync();
        return (customer, product, order);
    }

    [Fact]
    public async Task Customer_reviews_a_line_of_their_completed_order()
    {
        await using var db = await fixture.CreateContextAsync();
        var (customer, product, order) = await ArrangeAsync(db);

        await new ReviewService(db, TimeProvider.System).AddAsync(customer.Id, order.Id, product.Id, 4, "  Âm thanh tốt  ");

        var review = await db.Reviews.AsNoTracking().SingleAsync();
        Assert.Equal((customer.Id, product.Id, order.Id, (short)4, "Âm thanh tốt"), (review.CustomerId, review.ProductId, review.OrderId, review.Rating, review.Comment));
    }

    [Fact]
    public async Task A_line_can_only_be_reviewed_once()
    {
        await using var db = await fixture.CreateContextAsync();
        var (customer, product, order) = await ArrangeAsync(db);
        var service = new ReviewService(db, TimeProvider.System);
        await service.AddAsync(customer.Id, order.Id, product.Id, 5, null);

        var ex = await Assert.ThrowsAsync<DomainException>(() => service.AddAsync(customer.Id, order.Id, product.Id, 1, "đổi ý"));
        Assert.Contains("đã đánh giá", ex.Message);
    }

    [Fact]
    public async Task Only_completed_orders_can_be_reviewed()
    {
        await using var db = await fixture.CreateContextAsync();
        var (customer, product, order) = await ArrangeAsync(db, OrderStatus.Confirmed);

        await Assert.ThrowsAsync<DomainException>(() => new ReviewService(db, TimeProvider.System).AddAsync(customer.Id, order.Id, product.Id, 5, null));
    }

    [Fact]
    public async Task Another_customers_order_or_a_product_not_in_it_is_rejected()
    {
        await using var db = await fixture.CreateContextAsync();
        var (_, product, order) = await ArrangeAsync(db);
        var stranger = TestData.Customer(db, "stranger@example.com");
        var other = TestData.Product(db, TestData.Category(db, "Khác"), "Cáp");
        await db.SaveChangesAsync();
        var service = new ReviewService(db, TimeProvider.System);

        await Assert.ThrowsAsync<DomainException>(() => service.AddAsync(stranger.Id, order.Id, product.Id, 5, null));
        await Assert.ThrowsAsync<DomainException>(() => service.AddAsync(order.UserId, order.Id, other.Id, 5, null));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public async Task Rating_must_be_1_to_5(short rating)
    {
        await using var db = await fixture.CreateContextAsync();
        var (customer, product, order) = await ArrangeAsync(db);

        await Assert.ThrowsAsync<DomainException>(() => new ReviewService(db, TimeProvider.System).AddAsync(customer.Id, order.Id, product.Id, rating, null));
    }

    [Fact]
    public async Task Comment_is_limited_to_1000_characters()
    {
        await using var db = await fixture.CreateContextAsync();
        var (customer, product, order) = await ArrangeAsync(db);

        await Assert.ThrowsAsync<DomainException>(() =>
            new ReviewService(db, TimeProvider.System).AddAsync(customer.Id, order.Id, product.Id, 5, new string('a', 1001)));
    }
}
