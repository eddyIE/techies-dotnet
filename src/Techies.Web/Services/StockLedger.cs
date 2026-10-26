using Microsoft.EntityFrameworkCore;
using Techies.Web.Data;
using Techies.Web.Domain;

namespace Techies.Web.Services;

// The only code that changes stock_items.available. Locks the row (SELECT ... FOR UPDATE) so
// concurrent changes queue up, refuses to go below zero, and always writes the audit row.
// Callers must run inside a transaction and call SaveChanges.
public sealed class StockLedger(AppDbContext db, TimeProvider clock)
{
    public async Task<InventoryAdjustment> ChangeAsync(Guid productId, int delta, AdjustmentType type,
        string? reason = null, Guid? orderId = null, Guid? staffId = null, CancellationToken ct = default)
    {
        if (db.Database.CurrentTransaction is null)
            throw new InvalidOperationException("Stock changes must run inside a transaction.");

        var stock = await db.StockItems
            .FromSql($"SELECT * FROM stock_items WHERE product_id = {productId} FOR UPDATE")
            .SingleOrDefaultAsync(ct)
            ?? throw new DomainException("Không tìm thấy tồn kho của sản phẩm.");

        var before = stock.Available;
        if (before + delta < 0)
            throw new DomainException("Số lượng tồn kho không đủ.");

        var now = clock.GetUtcNow();
        stock.Available = before + delta;
        stock.UpdatedAt = now;
        var adjustment = new InventoryAdjustment
        {
            ProductId = productId, AdjustmentType = type, QuantityDelta = delta, QuantityBefore = before,
            QuantityAfter = stock.Available, Reason = reason, OrderId = orderId, StaffId = staffId, CreatedAt = now,
        };
        db.InventoryAdjustments.Add(adjustment);
        return adjustment;
    }

    public async Task<int> CurrentAsync(Guid productId, CancellationToken ct = default) =>
        await db.StockItems.Where(s => s.ProductId == productId).Select(s => (int?)s.Available).SingleOrDefaultAsync(ct)
        ?? throw new DomainException("Không tìm thấy tồn kho của sản phẩm.");
}
