using Microsoft.EntityFrameworkCore;
using Techies.Web.Data;
using Techies.Web.Domain;
using Techies.Web.Infrastructure;
using Techies.Web.Infrastructure.Grid;
using Techies.Web.ViewModels.Promotions;

namespace Techies.Web.Services;

public sealed record CouponEvaluation(bool Applicable, decimal Discount, string? Reason);

public sealed class CouponService(AppDbContext db, TimeProvider clock)
{
    public const string NotApplicable = "COUPON_NOT_APPLICABLE";

    public static CouponStatus StatusAt(Coupon coupon, DateTimeOffset now) =>
        !coupon.Active ? CouponStatus.Paused
        : coupon.StartsAt > now ? CouponStatus.NotStarted
        : coupon.ExpiresAt <= now ? CouponStatus.Expired
        : CouponStatus.Running;

    // Fixed amount off the subtotal, never more than the subtotal itself.
    public static CouponEvaluation Evaluate(Coupon coupon, decimal subtotal, DateTimeOffset now) =>
        StatusAt(coupon, now) == CouponStatus.Running && subtotal >= coupon.MinSubtotal
            ? new CouponEvaluation(true, Math.Min(coupon.DiscountAmount, subtotal), null)
            : new CouponEvaluation(false, 0, NotApplicable);

    public static readonly string[] GridSorts = ["code", "discount", "min", "starts", "expires", "created"];

    public async Task<(List<Coupon> Rows, int Total)> GridAsync(GridRequest r, CancellationToken ct = default)
    {
        var now = clock.GetUtcNow();
        var query = db.Coupons.AsNoTracking();
        if (r.Text("q") is { } q)
        {
            var pattern = QueryExtensions.Contains(q);
            query = query.Where(c => EF.Functions.ILike(c.Code, pattern) || (c.Description != null && EF.Functions.ILike(c.Description, pattern)));
        }
        // Same precedence as StatusAt: paused, then not started, then expired.
        query = r.Enum<CouponStatus>("status") switch
        {
            CouponStatus.Paused => query.Where(c => !c.Active),
            CouponStatus.NotStarted => query.Where(c => c.Active && c.StartsAt > now),
            CouponStatus.Expired => query.Where(c => c.Active && !(c.StartsAt > now) && c.ExpiresAt <= now),
            CouponStatus.Running => query.Where(c => c.Active && !(c.StartsAt > now) && !(c.ExpiresAt <= now)),
            _ => query,
        };
        if (r.Decimal("discount_from") is { } dFrom) query = query.Where(c => c.DiscountAmount >= dFrom);
        if (r.Decimal("discount_to") is { } dTo) query = query.Where(c => c.DiscountAmount <= dTo);
        if (r.Date("expires_from") is { } eFrom) { var start = Format.StartOfLocalDay(eFrom); query = query.Where(c => c.ExpiresAt >= start); }
        if (r.Date("expires_to") is { } eTo) { var end = Format.StartOfLocalDay(eTo.AddDays(1)); query = query.Where(c => c.ExpiresAt < end); }

        query = (r.Sort, r.Desc) switch
        {
            ("discount", false) => query.OrderBy(c => c.DiscountAmount),
            ("discount", true) => query.OrderByDescending(c => c.DiscountAmount),
            ("min", false) => query.OrderBy(c => c.MinSubtotal),
            ("min", true) => query.OrderByDescending(c => c.MinSubtotal),
            ("starts", false) => query.OrderBy(c => c.StartsAt),
            ("starts", true) => query.OrderByDescending(c => c.StartsAt),
            ("expires", false) => query.OrderBy(c => c.ExpiresAt),
            ("expires", true) => query.OrderByDescending(c => c.ExpiresAt),
            ("created", false) => query.OrderBy(c => c.CreatedAt),
            ("created", true) => query.OrderByDescending(c => c.CreatedAt),
            (_, true) => query.OrderByDescending(c => c.Code),
            _ => query.OrderBy(c => c.Code),
        };
        return (await query.Skip((r.Page - 1) * r.PageSize).Take(r.PageSize).ToListAsync(ct), await query.CountAsync(ct));
    }

    public async Task<Coupon> GetAsync(Guid id, CancellationToken ct = default) =>
        await db.Coupons.FindAsync([id], ct) ?? throw new DomainException("Không tìm thấy mã giảm giá.");

    public async Task<Coupon> CreateAsync(CouponForm form, Guid staffId, CancellationToken ct = default)
    {
        var coupon = new Coupon { CreatedBy = staffId, CreatedAt = clock.GetUtcNow() };
        await ApplyAsync(coupon, form, ct);
        db.Coupons.Add(coupon);
        await db.SaveChangesAsync(ct);
        return coupon;
    }

    public async Task UpdateAsync(Guid id, CouponForm form, CancellationToken ct = default)
    {
        var coupon = await GetAsync(id, ct);
        await ApplyAsync(coupon, form, ct);
        await db.SaveChangesAsync(ct);
    }

    // "Tạm dừng" / "Kích hoạt lại".
    public async Task SetActiveAsync(Guid id, bool active, CancellationToken ct = default)
    {
        var coupon = await GetAsync(id, ct);
        coupon.Active = active;
        await db.SaveChangesAsync(ct);
    }

    private async Task ApplyAsync(Coupon coupon, CouponForm form, CancellationToken ct)
    {
        var code = form.Code.Trim().ToUpperInvariant();
        if (await db.Coupons.AnyAsync(c => c.Code == code && c.Id != coupon.Id, ct))
            throw new DomainException("Mã giảm giá đã tồn tại.");
        if (form.DiscountAmount <= 0) throw new DomainException("Số tiền giảm phải lớn hơn 0.");
        if (form.MinSubtotal < 0) throw new DomainException("Giá trị đơn tối thiểu không được âm.");
        var starts = Format.FromLocalInput(form.StartsAt);
        var expires = Format.FromLocalInput(form.ExpiresAt);
        if (starts is not null && expires is not null && expires <= starts)
            throw new DomainException("Ngày hết hạn phải sau ngày bắt đầu.");

        coupon.Code = code;
        coupon.Description = string.IsNullOrWhiteSpace(form.Description) ? null : form.Description.Trim();
        coupon.DiscountAmount = form.DiscountAmount;
        coupon.MinSubtotal = form.MinSubtotal;
        coupon.StartsAt = starts;
        coupon.ExpiresAt = expires;
    }
}
