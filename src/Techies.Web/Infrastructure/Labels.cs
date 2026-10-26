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

    public static (string Text, string Badge) For(OrderStatus status) => status switch
    {
        OrderStatus.AwaitingPayment => ("Chờ thanh toán", "text-bg-warning"),
        OrderStatus.Confirmed => ("Đã xác nhận / Chờ giao", "text-bg-primary"),
        OrderStatus.Completed => ("Đã hoàn tất", "text-bg-success"),
        OrderStatus.Cancelled => ("Đã hủy", "text-bg-secondary"),
        OrderStatus.Failed => ("Giao dịch lỗi", "text-bg-danger"),
        _ => (status.ToString(), "text-bg-light"),
    };

    public static string For(PaymentMethod method) => method switch
    {
        PaymentMethod.Cod => "Thanh toán khi nhận hàng (COD)",
        PaymentMethod.MockCard => "Thẻ (MOCK_CARD)",
        _ => method.ToString(),
    };

    public static string Short(PaymentMethod method) => method == PaymentMethod.Cod ? "COD" : "Thẻ";

    public static (string Text, string Badge) For(PaymentStatus status) => status switch
    {
        PaymentStatus.Pending => ("Chưa thanh toán", "text-bg-warning"),
        PaymentStatus.Paid => ("Đã thanh toán", "text-bg-success"),
        PaymentStatus.Declined => ("Bị từ chối", "text-bg-danger"),
        PaymentStatus.Refunded => ("Đã hoàn tiền", "text-bg-info"),
        _ => (status.ToString(), "text-bg-light"),
    };

    public static string FailureCode(string? code) => code switch
    {
        "PAYMENT_FAILED" => "Thanh toán thất bại",
        "OUT_OF_STOCK" => "Hết hàng",
        "SERVICE_UNAVAILABLE" => "Dịch vụ không khả dụng",
        null => "",
        _ => code,
    };
}
