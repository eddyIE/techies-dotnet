using System.ComponentModel.DataAnnotations;

namespace Techies.Web.ViewModels.Promotions;

public sealed class CouponForm
{
    [Required(ErrorMessage = "Vui lòng nhập mã.")]
    [RegularExpression("^[A-Za-z0-9]{3,40}$", ErrorMessage = "Mã gồm 3–40 chữ cái hoặc chữ số, không dấu cách.")]
    [Display(Name = "Mã giảm giá")]
    public string Code { get; set; } = "";

    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    [Range(1, 1_000_000_000, ErrorMessage = "Số tiền giảm phải lớn hơn 0.")]
    [Display(Name = "Số tiền giảm (VNĐ)")]
    public decimal DiscountAmount { get; set; }

    [Range(0, 10_000_000_000, ErrorMessage = "Giá trị đơn tối thiểu không được âm.")]
    [Display(Name = "Giá trị đơn tối thiểu (VNĐ)")]
    public decimal MinSubtotal { get; set; }

    // Vietnam local time from <input type="datetime-local">.
    [Display(Name = "Bắt đầu")]
    public DateTime? StartsAt { get; set; }

    [Display(Name = "Hết hạn")]
    public DateTime? ExpiresAt { get; set; }
}
