using System.Security.Claims;

namespace Techies.Web.Infrastructure;

public sealed record AdminMenuItem(string Section, string Title, string Icon, string Controller, string Policy);

public static class AdminMenu
{
    // Grows as each module lands. Order = sidebar order = landing preference after login.
    public static readonly AdminMenuItem[] Items =
    [
        new("Tổng quan", "Bảng điều khiển", "bi-speedometer2", "Dashboard", Policies.SuperAdmin),
        new("Sản phẩm & Kho", "Danh mục", "bi-tags", "Categories", Policies.Catalog),
    ];

    public static IEnumerable<AdminMenuItem> For(ClaimsPrincipal user) => Items.Where(i => user.Allows(i.Policy));
}
