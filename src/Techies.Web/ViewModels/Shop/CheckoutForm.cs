using System.ComponentModel.DataAnnotations;

namespace Techies.Web.ViewModels.Shop;

// Either pick a saved address (AddressId) or fill in a new one.
public sealed class CheckoutForm : IValidatableObject
{
    public Guid? AddressId { get; set; }

    [Display(Name = "Người nhận")] public string? RecipientName { get; set; }
    [Display(Name = "Số điện thoại")] public string? Phone { get; set; }
    [Display(Name = "Số nhà, đường")] public string? Line1 { get; set; }
    [Display(Name = "Phường/Xã")] public string? Ward { get; set; }
    [Display(Name = "Quận/Huyện")] public string? District { get; set; }
    [Display(Name = "Tỉnh/Thành phố")] public string? Province { get; set; }
    [Display(Name = "Đặt làm địa chỉ mặc định")] public bool MakeDefault { get; set; }
    [Display(Name = "Mã giảm giá")] public string? CouponCode { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (AddressId is not null) yield break;
        foreach (var (value, name, label) in new[]
        {
            (RecipientName, nameof(RecipientName), "người nhận"), (Line1, nameof(Line1), "số nhà, đường"),
            (Ward, nameof(Ward), "phường/xã"), (District, nameof(District), "quận/huyện"), (Province, nameof(Province), "tỉnh/thành phố"),
        })
        {
            if (string.IsNullOrWhiteSpace(value)) yield return new ValidationResult($"Vui lòng nhập {label}.", [name]);
        }
        if (Phone is null || !System.Text.RegularExpressions.Regex.IsMatch(Phone.Trim(), @"^0\d{9}$"))
            yield return new ValidationResult("Số điện thoại gồm 10 chữ số, bắt đầu bằng 0.", [nameof(Phone)]);
    }
}
