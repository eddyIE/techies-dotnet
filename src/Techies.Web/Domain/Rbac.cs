namespace Techies.Web.Domain;

// A staff role (BRD §7). The system role is the built-in Super Admin: it implicitly has every
// permission, manages staff and roles, and cannot be edited or deleted.
public sealed class Role
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public List<RolePermission> Permissions { get; set; } = [];
}

// Mirrors one entry of Infrastructure.Permissions (the catalogue in code).
public sealed class Permission
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Module { get; set; } = "";
}

public sealed class RolePermission
{
    public int RoleId { get; set; }
    public int PermissionId { get; set; }
}

// Roles created by the RBAC migration; ids are fixed so seeds and tests can refer to them.
public static class SeededRoles
{
    public const int SuperAdmin = 1;
    public const int CatalogStaff = 2;
    public const int OrderStaff = 3;
}
