using Microsoft.EntityFrameworkCore;
using Techies.Web.Data;
using Techies.Web.Domain;
using Techies.Web.Infrastructure;
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

    public Task<List<Coupon>> ListAsync(CancellationToken ct = default) =>
        db.Coupons.AsNoTracking().OrderByDescending(c => c.CreatedAt).ToListAsync(ct);

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
    public async Task<bool> ToggleActiveAsync(Guid id, CancellationToken ct = default)
    {
        var coupon = await GetAsync(id, ct);
        coupon.Active = !coupon.Active;
        await db.SaveChangesAsync(ct);
        return coupon.Active;
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
