using Techies.Web.Services;

namespace Techies.Web.Infrastructure;

// Permission null = system Super Admin only (staff and role management).
public sealed record AdminMenuItem(string Section, string Title, string Icon, string Controller, string? Permission);

public static class AdminMenu
{
    // Order = sidebar order = landing preference after login.
    public static readonly AdminMenuItem[] Items =
    [
        new("Tổng quan", "Bảng điều khiển", "bi-speedometer2", "Dashboard", Permissions.DashboardView),
        new("Sản phẩm & Kho", "Sản phẩm", "bi-phone", "Products", Permissions.ProductsView),
        new("Sản phẩm & Kho", "Danh mục", "bi-tags", "Categories", Permissions.CategoriesManage),
        new("Sản phẩm & Kho", "Kiểm kho & Nhập hàng", "bi-box-seam", "Inventory", Permissions.InventoryView),
        new("Bán hàng & Khách hàng", "Đơn hàng", "bi-receipt", "Orders", Permissions.OrdersView),
        new("Bán hàng & Khách hàng", "Khách hàng", "bi-people", "Customers", Permissions.CustomersView),
        new("Bán hàng & Khách hàng", "Đánh giá", "bi-star-half", "Reviews", Permissions.ReviewsView),
        new("Bán hàng & Khách hàng", "Quầy nhận quà", "bi-upc-scan", "GiftCounter", Permissions.GiftCounterLookup),
        new("Khuyến mãi & Thành viên", "Mã giảm giá", "bi-ticket-perforated", "Coupons", Permissions.CouponsView),
        new("Khuyến mãi & Thành viên", "Quà tặng đổi điểm", "bi-gift", "Gifts", Permissions.GiftsView),
        new("Khuyến mãi & Thành viên", "Hạng thành viên", "bi-award", "Tiers", Permissions.TiersManage),
        new("Hệ thống", "Nhân sự", "bi-person-badge", "Staff", null),
        new("Hệ thống", "Vai trò & Phân quyền", "bi-shield-lock", "Roles", null),
    ];

    public static IEnumerable<AdminMenuItem> For(StaffAccess access) =>
        Items.Where(i => i.Permission is null ? access.IsSuperAdmin : access.Has(i.Permission));
}
