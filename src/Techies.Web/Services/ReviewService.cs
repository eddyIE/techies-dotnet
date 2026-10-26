using Microsoft.EntityFrameworkCore;
using Techies.Web.Data;
using Techies.Web.Domain;

namespace Techies.Web.Services;

// Customers review what they bought (SPEC shop-extras): one review per line of a COMPLETED
// order, never edited or deleted afterwards.
public sealed class ReviewService(AppDbContext db, TimeProvider clock)
{
    public const int MaxCommentLength = 1000;

    public async Task AddAsync(Guid customerId, Guid orderId, Guid productId, short rating, string? comment, CancellationToken ct = default)
    {
        if (rating is < 1 or > 5) throw new DomainException("Vui lòng chọn từ 1 đến 5 sao.");
        comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        if (comment?.Length > MaxCommentLength) throw new DomainException($"Nhận xét tối đa {MaxCommentLength} ký tự.");

        var order = await db.Orders.AsNoTracking().Include(o => o.Items)
            .SingleOrDefaultAsync(o => o.Id == orderId && o.UserId == customerId, ct)
            ?? throw new DomainException("Không tìm thấy đơn hàng.");
        if (order.Status != OrderStatus.Completed) throw new DomainException("Chỉ đánh giá được đơn hàng đã hoàn tất.");
        if (order.Items.All(i => i.ProductId != productId)) throw new DomainException("Sản phẩm này không có trong đơn hàng.");
        if (await db.Reviews.AnyAsync(r => r.OrderId == orderId && r.ProductId == productId, ct))
            throw new DomainException("Bạn đã đánh giá sản phẩm này cho đơn hàng này rồi.");

        db.Reviews.Add(new Review
        {
            CustomerId = customerId, OrderId = orderId, ProductId = productId, Rating = rating, Comment = comment,
            CreatedAt = clock.GetUtcNow(),
        });
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException { SqlState: "23505" })
        {
            throw new DomainException("Bạn đã đánh giá sản phẩm này cho đơn hàng này rồi."); // double submit
        }
    }

    public Task<Dictionary<Guid, Review>> ForOrderAsync(Guid orderId, CancellationToken ct = default) =>
        db.Reviews.AsNoTracking().Where(r => r.OrderId == orderId).ToDictionaryAsync(r => r.ProductId, ct);
}
