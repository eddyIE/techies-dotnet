using Techies.Web.Domain;

namespace Techies.Web.Infrastructure;

// Vietnamese display names for stored enum values.
public static class Labels
{
    public static string For(AdjustmentType type) => type switch
    {
        AdjustmentType.Import => "Nhập hàng",
        AdjustmentType.Count => "Kiểm kê",
        AdjustmentType.Order => "Xuất theo đơn",
        AdjustmentType.Cancel => "Hoàn kho (hủy đơn)",
        _ => type.ToString(),
    };

    public static string For(StaffRole role) => role switch
    {
        StaffRole.SuperAdmin => "Quản trị viên cao cấp",
        StaffRole.CatalogStaff => "Nhân viên Kho & Sản phẩm",
        StaffRole.OrderStaff => "Nhân viên Điều phối & Quầy",
        _ => role.ToString(),
    };

    public static (string Text, string Badge) For(CouponStatus status) => status switch
    {
        CouponStatus.Running => ("Đang chạy", "text-bg-success"),
        CouponStatus.Paused => ("Tạm dừng", "text-bg-secondary"),
        CouponStatus.NotStarted => ("Chưa bắt đầu", "text-bg-info"),
        CouponStatus.Expired => ("Hết hạn", "text-bg-dark"),
        _ => (status.ToString(), "text-bg-light"),
    };
}
