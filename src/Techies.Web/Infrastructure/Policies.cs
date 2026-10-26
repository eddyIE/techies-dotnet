using System.Security.Claims;
using Techies.Web.Domain;

namespace Techies.Web.Infrastructure;

public static class Policies
{
    public const string AnyStaff = nameof(AnyStaff);
    public const string SuperAdmin = nameof(SuperAdmin);
    public const string Catalog = nameof(Catalog);
    public const string Orders = nameof(Orders);
    public const string Customer = nameof(Customer);

    // Role access from SPEC identity module (BRD §7).
    public static readonly IReadOnlyDictionary<string, StaffRole[]> StaffRoles = new Dictionary<string, StaffRole[]>
    {
        [AnyStaff] = Enum.GetValues<StaffRole>(),
        [SuperAdmin] = [StaffRole.SuperAdmin],
        [Catalog] = [StaffRole.SuperAdmin, StaffRole.CatalogStaff],
        [Orders] = [StaffRole.SuperAdmin, StaffRole.OrderStaff],
    };

    public static bool Allows(this ClaimsPrincipal user, string policy) =>
        StaffRoles[policy].Any(role => user.IsInRole(role.ToString()));
}
