using System.Security.Claims;

namespace Techies.Web.Infrastructure;

public sealed record AdminMenuItem(string Section, string Title, string Icon, string Controller, string Policy);

public static class AdminMenu
{
    // Grows as each module lands. Order = sidebar order = landing preference after login.
    public static readonly AdminMenuItem[] Items =
    [
        new("Tổng quan", "Bảng điều khiển", "bi-speedometer2", "Dashboard", Policies.SuperAdmin),
        new("Sản phẩm & Kho", "Sản phẩm", "bi-phone", "Products", Policies.Catalog),
        new("Sản phẩm & Kho", "Danh mục", "bi-tags", "Categories", Policies.Catalog),
        new("Sản phẩm & Kho", "Kiểm kho & Nhập hàng", "bi-box-seam", "Inventory", Policies.Catalog),
        new("Bán hàng & Khách hàng", "Đơn hàng", "bi-receipt", "Orders", Policies.Orders),
        new("Bán hàng & Khách hàng", "Khách hàng", "bi-people", "Customers", Policies.Orders),
        new("Bán hàng & Khách hàng", "Đánh giá", "bi-star-half", "Reviews", Policies.Orders),
        new("Bán hàng & Khách hàng", "Quầy nhận quà", "bi-upc-scan", "GiftCounter", Policies.Orders),
        new("Khuyến mãi & Thành viên", "Mã giảm giá", "bi-ticket-perforated", "Coupons", Policies.SuperAdmin),
        new("Khuyến mãi & Thành viên", "Quà tặng đổi điểm", "bi-gift", "Gifts", Policies.SuperAdmin),
        new("Khuyến mãi & Thành viên", "Hạng thành viên", "bi-award", "Tiers", Policies.SuperAdmin),
    ];

    public static IEnumerable<AdminMenuItem> For(ClaimsPrincipal user) => Items.Where(i => user.Allows(i.Policy));
}
