namespace Techies.Web.Infrastructure;

public sealed record PermissionDefinition(int Id, string Code, string Name, string Module);

// The permission catalogue (SPEC Module: rbac). Ids are stable: they are seeded into the
// permissions table by migration, so never renumber — append new entries with new ids.
// Staff and role management are deliberately absent: they belong to the system role only.
public static class Permissions
{
    public const string DashboardView = "dashboard.view";
    public const string ProductsView = "products.view";
    public const string ProductsEdit = "products.edit";
    public const string ProductsToggle = "products.toggle";
    public const string CategoriesManage = "categories.manage";
    public const string InventoryView = "inventory.view";
    public const string InventoryImport = "inventory.import";
    public const string InventoryCount = "inventory.count";
    public const string OrdersView = "orders.view";
    public const string OrdersComplete = "orders.complete";
    public const string OrdersCancel = "orders.cancel";
    public const string OrdersPrint = "orders.print";
    public const string CustomersView = "customers.view";
    public const string CustomersLock = "customers.lock";
    public const string ReviewsView = "reviews.view";
    public const string GiftCounterLookup = "giftcounter.lookup";
    public const string GiftCounterClaim = "giftcounter.claim";
    public const string CouponsView = "coupons.view";
    public const string CouponsManage = "coupons.manage";
    public const string GiftsView = "gifts.view";
    public const string GiftsManage = "gifts.manage";
    public const string TiersManage = "tiers.manage";

    public static readonly IReadOnlyList<PermissionDefinition> All =
    [
        new(1, DashboardView, "Xem bảng điều khiển & doanh thu", "Tổng quan"),
        new(2, ProductsView, "Xem sản phẩm", "Sản phẩm"),
        new(3, ProductsEdit, "Thêm / sửa sản phẩm", "Sản phẩm"),
        new(4, ProductsToggle, "Bật / tắt kinh doanh", "Sản phẩm"),
        new(5, CategoriesManage, "Quản lý danh mục", "Danh mục"),
        new(6, InventoryView, "Xem tồn kho", "Kho"),
        new(7, InventoryImport, "Nhập hàng", "Kho"),
        new(8, InventoryCount, "Kiểm kê", "Kho"),
        new(9, OrdersView, "Xem đơn hàng", "Đơn hàng"),
        new(10, OrdersComplete, "Hoàn tất đơn hàng", "Đơn hàng"),
        new(11, OrdersCancel, "Hủy đơn hàng", "Đơn hàng"),
        new(12, OrdersPrint, "In phiếu giao hàng", "Đơn hàng"),
        new(13, CustomersView, "Xem khách hàng", "Khách hàng"),
        new(14, CustomersLock, "Khóa / mở khóa khách hàng", "Khách hàng"),
        new(15, ReviewsView, "Xem đánh giá", "Đánh giá"),
        new(16, GiftCounterLookup, "Tra cứu mã nhận quà", "Quầy nhận quà"),
        new(17, GiftCounterClaim, "Xác nhận giao quà", "Quầy nhận quà"),
        new(18, CouponsView, "Xem mã giảm giá", "Mã giảm giá"),
        new(19, CouponsManage, "Tạo / sửa / tạm dừng mã", "Mã giảm giá"),
        new(20, GiftsView, "Xem quà tặng", "Quà tặng"),
        new(21, GiftsManage, "Thêm / sửa quà tặng", "Quà tặng"),
        new(22, TiersManage, "Cấu hình hạng thành viên", "Hạng thành viên"),
    ];

    private static readonly Dictionary<string, PermissionDefinition> ByCode = All.ToDictionary(p => p.Code);

    public static PermissionDefinition Find(string code) => ByCode[code];

    // The access the old fixed roles had (SPEC identity table), given to the seeded roles.
    public static readonly string[] CatalogStaffDefaults =
        [ProductsView, ProductsEdit, ProductsToggle, CategoriesManage, InventoryView, InventoryImport, InventoryCount];

    public static readonly string[] OrderStaffDefaults =
        [OrdersView, OrdersComplete, OrdersCancel, OrdersPrint, CustomersView, CustomersLock, ReviewsView, GiftCounterLookup, GiftCounterClaim];
}
