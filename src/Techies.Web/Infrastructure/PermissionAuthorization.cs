using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Techies.Web.Services;

namespace Techies.Web.Infrastructure;

// [RequirePermission(Permissions.OrdersComplete)] on a controller or action. Several attributes
// combine with AND (e.g. orders.view on the controller plus orders.complete on the action).
public sealed class RequirePermissionAttribute(string permission) : AuthorizeAttribute(PermissionPolicies.Prefix + permission);

public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}

public sealed class SuperAdminRequirement : IAuthorizationRequirement;

public sealed class PermissionHandler(CurrentStaff current) : IAuthorizationHandler
{
    public async Task HandleAsync(AuthorizationHandlerContext context)
    {
        foreach (var requirement in context.PendingRequirements.ToList())
        {
            var granted = requirement switch
            {
                PermissionRequirement p => await current.HasAsync(p.Permission),
                SuperAdminRequirement => (await current.AccessAsync()).IsSuperAdmin,
                _ => (bool?)null,
            };
            if (granted == true) context.Succeed(requirement);
        }
    }
}

// Builds "perm:<code>" policies on demand, all bound to the Staff scheme.
public sealed class PermissionPolicies(IOptions<AuthorizationOptions> options) : DefaultAuthorizationPolicyProvider(options)
{
    public const string Prefix = "perm:";

    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith(Prefix, StringComparison.Ordinal))
            return await base.GetPolicyAsync(policyName);
        return new AuthorizationPolicyBuilder(AuthSchemes.Staff)
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(policyName[Prefix.Length..]))
            .Build();
    }
}
