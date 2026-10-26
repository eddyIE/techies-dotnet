using Microsoft.EntityFrameworkCore;
using Techies.Web.Data;
using Techies.Web.Domain;
using Techies.Web.Services;

namespace Techies.Tests.Services;

[Collection(PostgresCollection.Name)]
public class InventoryServiceTests(PostgresFixture fixture)
{
    private static async Task<(Product Product, Staff Staff)> ArrangeAsync(AppDbContext db, int available)
    {
        var product = TestData.Product(db, TestData.Category(db), "Pin dự phòng", available: available);
        var staff = new Staff { FullName = "Kho", Email = "kho@x.vn", PasswordHash = "x", RoleId = SeededRoles.CatalogStaff };
        db.Staff.Add(staff);
        await db.SaveChangesAsync();
        return (product, staff);
    }

    [Fact]
    public async Task Import_adds_quantity_and_records_adjustment()
    {
        await using var db = await fixture.CreateContextAsync();
        var (product, staff) = await ArrangeAsync(db, available: 7);

        await new InventoryService(db, TimeProvider.System).ImportAsync(product.Id, 5, "Nhập lô tháng 10", staff.Id);

        db.ChangeTracker.Clear();
        Assert.Equal(12, (await db.StockItems.SingleAsync(s => s.ProductId == product.Id)).Available);
        var adj = await db.InventoryAdjustments.SingleAsync();
        Assert.Equal((AdjustmentType.Import, 5, 7, 12, staff.Id, "Nhập lô tháng 10"),
            (adj.AdjustmentType, adj.QuantityDelta, adj.QuantityBefore, adj.QuantityAfter, adj.StaffId, adj.Reason));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public async Task Import_rejects_non_positive_quantity(int quantity)
    {
        await using var db = await fixture.CreateContextAsync();
        var (product, staff) = await ArrangeAsync(db, available: 7);

        await Assert.ThrowsAsync<DomainException>(
            () => new InventoryService(db, TimeProvider.System).ImportAsync(product.Id, quantity, null, staff.Id));
    }

    [Fact]
    public async Task Count_sets_actual_quantity_and_records_computed_delta()
    {
        await using var db = await fixture.CreateContextAsync();
        var (product, staff) = await ArrangeAsync(db, available: 10);

        await new InventoryService(db, TimeProvider.System).CountAsync(product.Id, 8, "Kiểm kê cuối tháng", staff.Id);

        db.ChangeTracker.Clear();
        Assert.Equal(8, (await db.StockItems.SingleAsync(s => s.ProductId == product.Id)).Available);
        var adj = await db.InventoryAdjustments.SingleAsync();
        Assert.Equal((AdjustmentType.Count, -2, 10, 8), (adj.AdjustmentType, adj.QuantityDelta, adj.QuantityBefore, adj.QuantityAfter));
    }

    [Fact]
    public async Task Count_rejects_negative_quantity()
    {
        await using var db = await fixture.CreateContextAsync();
        var (product, staff) = await ArrangeAsync(db, available: 10);

        await Assert.ThrowsAsync<DomainException>(
            () => new InventoryService(db, TimeProvider.System).CountAsync(product.Id, -1, null, staff.Id));
    }

    [Fact]
    public async Task History_lists_newest_first()
    {
        await using var db = await fixture.CreateContextAsync();
        var (product, staff) = await ArrangeAsync(db, available: 0);
        var service = new InventoryService(db, TimeProvider.System);
        await service.ImportAsync(product.Id, 10, "lần 1", staff.Id);
        await service.CountAsync(product.Id, 9, "lần 2", staff.Id);

        var history = await service.HistoryAsync(product.Id);

        Assert.Equal(["lần 2", "lần 1"], history.Select(h => h.Reason));
    }

    [Fact]
    public async Task Count_applies_the_counted_quantity_even_if_stock_changed_while_waiting_for_the_lock()
    {
        await using var db = await fixture.CreateContextAsync();
        var (product, staff) = await ArrangeAsync(db, available: 10);
        await using var other = PostgresFixture.CreateSibling(db);

        // Another request holds the stock row and takes 2 units, committing a moment later.
        await using var tx = await other.Database.BeginTransactionAsync();
        await other.Database.ExecuteSqlAsync($"UPDATE stock_items SET available = 8 WHERE product_id = {product.Id}");
        var count = new InventoryService(db, TimeProvider.System).CountAsync(product.Id, 10, "Kiểm kê", staff.Id);
        await Task.Delay(300);
        await tx.CommitAsync();
        await count;

        db.ChangeTracker.Clear();
        Assert.Equal(10, (await db.StockItems.SingleAsync()).Available);
        var adj = await db.InventoryAdjustments.SingleAsync();
        Assert.Equal((8, 10, 2), (adj.QuantityBefore, adj.QuantityAfter, adj.QuantityDelta));
    }
}
