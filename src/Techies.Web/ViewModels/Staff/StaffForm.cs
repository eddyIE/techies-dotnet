using System.ComponentModel.DataAnnotations;
using Techies.Web.Domain;

namespace Techies.Web.ViewModels.Staff;

public sealed class StaffForm
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
    [StringLength(120)]
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập email công việc.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [Display(Name = "Email công việc")]
    public string Email { get; set; } = "";

    [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại gồm 10 chữ số, bắt đầu bằng 0.")]
    [Display(Name = "Số điện thoại")]
    public string? Phone { get; set; }

    // Only used when creating; editing never touches the password (see reset password).
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu khởi tạo")]
    public string? Password { get; set; }

    [Display(Name = "Vai trò")]
    public StaffRole Role { get; set; } = StaffRole.OrderStaff;
}
