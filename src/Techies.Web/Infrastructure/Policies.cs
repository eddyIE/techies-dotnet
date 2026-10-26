namespace Techies.Web.Infrastructure;

public static class Policies
{
    public const string AnyStaff = nameof(AnyStaff);
    // Staff and role management: the system Super Admin role only (BRD §7).
    public const string SuperAdmin = nameof(SuperAdmin);
    public const string Customer = nameof(Customer);
}
