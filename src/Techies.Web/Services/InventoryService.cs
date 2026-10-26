using Microsoft.EntityFrameworkCore;
using Techies.Web.Data;
using Techies.Web.Domain;

namespace Techies.Web.Services;

public sealed record AdjustmentRow(DateTimeOffset CreatedAt, AdjustmentType Type, int Delta, int Before, int After,
    string? Reason, string? StaffName, Guid? OrderId);

// BRD §2 "Kiểm kho & Nhập hàng".
public sealed class InventoryService(AppDbContext db, TimeProvider clock)
{
    public async Task ImportAsync(Guid productId, int quantity, string? reason, Guid staffId, CancellationToken ct = default)
    {
        if (quantity <= 0) throw new DomainException("Số lượng nhập phải lớn hơn 0.");
        await InTransactionAsync(ledger => ledger.ChangeAsync(productId, quantity, AdjustmentType.Import, Clean(reason), staffId: staffId, ct: ct), ct);
    }

    // Stock count: staff enter what is physically on the shelf; the difference is recorded.
    public async Task CountAsync(Guid productId, int countedQuantity, string? reason, Guid staffId, CancellationToken ct = default)
    {
        if (countedQuantity < 0) throw new DomainException("Số lượng kiểm kê không được âm.");
        await InTransactionAsync(ledger => ledger.SetAsync(productId, countedQuantity, AdjustmentType.Count, Clean(reason), staffId, ct), ct);
    }

    public Task<List<AdjustmentRow>> HistoryAsync(Guid productId, CancellationToken ct = default) =>
        db.InventoryAdjustments.AsNoTracking()
            .Where(a => a.ProductId == productId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new AdjustmentRow(a.CreatedAt, a.AdjustmentType, a.QuantityDelta, a.QuantityBefore,
                a.QuantityAfter, a.Reason, a.Staff != null ? a.Staff.FullName : null, a.OrderId))
            .Take(100)
            .ToListAsync(ct);

    private async Task InTransactionAsync(Func<StockLedger, Task<InventoryAdjustment>> change, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await change(new StockLedger(db, clock));
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    private static string? Clean(string? reason) => string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
}
