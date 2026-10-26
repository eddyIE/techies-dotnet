using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Techies.Web.Domain;
using Techies.Web.Services;

namespace Techies.Web.Infrastructure;

// Two independent cookie schemes: a customer cookie never authorizes /admin and a staff
// cookie is never a shop login, because every policy names exactly one scheme.
public static class AuthSchemes
{
    public const string Staff = "Staff";
    public const string Customer = "Customer";
}

public static class AuthSetup
{
    public static void AddAppAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(AuthSchemes.Customer)
            .AddCookie(AuthSchemes.Staff, o =>
            {
                o.Cookie.Name = ".Techies.Staff";
                o.Cookie.Path = "/admin";
                o.LoginPath = "/admin/auth/login";
                o.LogoutPath = "/admin/auth/logout";
                o.AccessDeniedPath = "/admin/auth/denied";
                o.Events.OnValidatePrincipal = ValidateStaffAsync;
            })
            .AddCookie(AuthSchemes.Customer, o =>
            {
                o.Cookie.Name = ".Techies.Customer";
                o.LoginPath = "/tai-khoan/dang-nhap";
                o.LogoutPath = "/tai-khoan/dang-xuat";
                o.AccessDeniedPath = "/tai-khoan/dang-nhap";
                o.Events.OnValidatePrincipal = ValidateCustomerAsync;
            });

        services.AddHttpContextAccessor();
        services.AddScoped<PermissionService>();
        services.AddScoped<CurrentStaff>();
        services.AddScoped<IAuthorizationHandler, PermissionHandler>();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicies>();
        services.AddAuthorization(options =>
        {
            options.AddPolicy(Policies.AnyStaff, p => p.AddAuthenticationSchemes(AuthSchemes.Staff).RequireAuthenticatedUser());
            options.AddPolicy(Policies.SuperAdmin, p => p
                .AddAuthenticationSchemes(AuthSchemes.Staff).RequireAuthenticatedUser().AddRequirements(new SuperAdminRequirement()));
            options.AddPolicy(Policies.Customer, p => p
                .AddAuthenticationSchemes(AuthSchemes.Customer)
                .RequireAuthenticatedUser());
        });
    }

    public static ClaimsPrincipal StaffPrincipal(Staff staff) => new(new ClaimsIdentity(
    [
        new Claim(ClaimTypes.NameIdentifier, staff.Id.ToString()),
        new Claim(ClaimTypes.Name, staff.FullName),
        new Claim(ClaimTypes.Email, staff.Email),
        new Claim(RoleIdClaim, staff.RoleId.ToString()),
    ], AuthSchemes.Staff));

    public static ClaimsPrincipal CustomerPrincipal(User user) => new(new ClaimsIdentity(
    [
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.FullName),
        new Claim(ClaimTypes.Email, user.Email),
    ], AuthSchemes.Customer));

    public const string RoleIdClaim = "techies:role_id";

    public static Guid UserId(this ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static async Task ValidateStaffAsync(CookieValidatePrincipalContext context)
    {
        var principal = context.Principal!;
        var valid = Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            && int.TryParse(principal.FindFirstValue(RoleIdClaim), out var roleId)
            && await context.HttpContext.RequestServices.GetRequiredService<AccountStatusService>()
                .IsStaffSessionValidAsync(id, roleId);
        if (!valid) await RejectAsync(context);
    }

    private static async Task ValidateCustomerAsync(CookieValidatePrincipalContext context)
    {
        var valid = Guid.TryParse(context.Principal!.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            && await context.HttpContext.RequestServices.GetRequiredService<AccountStatusService>()
                .IsCustomerSessionValidAsync(id);
        if (!valid) await RejectAsync(context);
    }

    private static async Task RejectAsync(CookieValidatePrincipalContext context)
    {
        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(context.Scheme.Name);
    }
}
