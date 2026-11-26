using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Shop.Data.Application.Admin.Contracts;

namespace Shop.Admin.Security;

public sealed class AdminAuthentication(IAdminIdentityData data)
{
    public async Task<ClaimsPrincipal?> SignInAsync(string userName, string password,
        CancellationToken cancellationToken)
    {
        string normalizedName = userName.Trim().ToUpperInvariant();
        var identity = await data.AuthenticateAsync(normalizedName, password, cancellationToken);
        return identity is null ? null : CreatePrincipal(identity);
    }

    internal static ClaimsPrincipal CreatePrincipal(AdminIdentityState user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName),
            new("display_name", user.DisplayName),
            new("admin_security_stamp", user.SecurityStamp.ToString("D"))
        };
        foreach (long warehouseId in user.WarehouseIds)
            claims.Add(new Claim("admin_warehouse", warehouseId.ToString()));

        var children = user.AllPermissions.GroupBy(x => x.ParentPermissionKey ?? "")
            .ToDictionary(x => x.Key, x => x.ToArray(), StringComparer.OrdinalIgnoreCase);
        var granted = new Dictionary<string, AdminPermissionData>(StringComparer.OrdinalIgnoreCase);

        void Grant(AdminPermissionData permission)
        {
            if (!granted.TryAdd(permission.PermissionKey, permission)) return;

            if (children.TryGetValue(permission.PermissionKey, out AdminPermissionData[]? descendants))
                foreach (AdminPermissionData descendant in descendants)
                    Grant(descendant);
        }

        foreach (AdminRoleData role in user.Roles.Where(x => x.IsActive))
        {
            claims.Add(new Claim(ClaimTypes.Role, role.RoleKey));
            if (role.RoleKey == "system.full-access")
            {
                foreach (AdminPermissionData permission in user.AllPermissions)
                    Grant(permission);
                continue;
            }

            foreach (AdminRolePermissionData permission in role.Permissions.Where(x => x.Permission.IsActive))
                Grant(permission.Permission);
        }

        foreach (AdminPermissionData permission in granted.Values.Where(x => !string.IsNullOrWhiteSpace(x.Area)
                                                                         && !string.IsNullOrWhiteSpace(x.Controller) &&
                                                                         !string.IsNullOrWhiteSpace(x.Action)))
            claims.Add(new Claim("admin_permission",
                RoutePermission(permission.Area ?? "", permission.Controller ?? "", permission.Action ?? "",
                    permission.FeatureKey)));

        var identity = new ClaimsIdentity(claims, "AdminCookie");

        return new ClaimsPrincipal(identity);
    }

    public static string RoutePermission(string area, string controller, string action, string? featureKey = null) =>
        string.IsNullOrWhiteSpace(featureKey)
            ? $"{area}:{controller}:{action}"
            : $"{area}:{controller}:{action}:{featureKey}";
}

public sealed class AdminBootstrapper(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<AdminBootstrapper> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        string? userName = configuration["AdminBootstrap:UserName"];
        string? password = configuration["AdminBootstrap:Password"];
        string? displayName = configuration["AdminBootstrap:DisplayName"];
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
            return;

        if (password.Length < 12 || password.Length > 200)
        {
            logger.LogError(
                "AdminBootstrap password must be between 12 and 200 characters; no initial system user was created.");

            return;
        }

        using IServiceScope scope = scopeFactory.CreateScope();
        var data = scope.ServiceProvider.GetRequiredService<IAdminIdentityData>();
        var result = await data.BootstrapAsync(userName, password, displayName, cancellationToken);
        if (result == AdminBootstrapResult.SchemaMissing)
        {
            logger.LogError("Admin schema is not installed; the configured initial system user was not created.");
        }
        else if (result == AdminBootstrapResult.Created)
            logger.LogInformation("Created the initial system user from AdminBootstrap configuration.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

public sealed class AdminCookieEvents(IAdminIdentityData data) : CookieAuthenticationEvents
{
    public override async Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> context)
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;

            return;
        }

        await base.RedirectToLogin(context);
    }

    public override async Task RedirectToAccessDenied(RedirectContext<CookieAuthenticationOptions> context)
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;

            return;
        }

        await base.RedirectToAccessDenied(context);
    }

    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        string? idText = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        string? stampText = context.Principal?.FindFirstValue("admin_security_stamp");
        if (!long.TryParse(idText, out long userId) || !Guid.TryParse(stampText, out Guid stamp))
        {
            await RejectAsync(context);

            return;
        }

        var identity = await data.ReadActiveAsync(userId, context.HttpContext.RequestAborted);
        if (identity is null || identity.SecurityStamp != stamp)
        {
            await RejectAsync(context);

            return;
        }

        ClaimsPrincipal current = context.Principal!;
        ClaimsPrincipal refreshed = AdminAuthentication.CreatePrincipal(identity);
        if (!ClaimsMatch(current, refreshed))
        {
            context.ReplacePrincipal(refreshed);
            context.ShouldRenew = true;
        }
    }

    private static bool ClaimsMatch(ClaimsPrincipal current, ClaimsPrincipal refreshed)
    {
        string[] currentClaims = current.Claims.Select(claim => claim.Type + "\0" + claim.Value)
            .OrderBy(value => value, StringComparer.Ordinal).ToArray();
        string[] refreshedClaims = refreshed.Claims.Select(claim => claim.Type + "\0" + claim.Value)
            .OrderBy(value => value, StringComparer.Ordinal).ToArray();

        return currentClaims.SequenceEqual(refreshedClaims, StringComparer.Ordinal);
    }

    private static async Task RejectAsync(CookieValidatePrincipalContext context)
    {
        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync("AdminCookie");
    }
}

public sealed class AdminPermissionFilter : IAsyncAuthorizationFilter
{
    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (context.Filters.Any(filter => filter is IAllowAnonymousFilter))
            return Task.CompletedTask;

        var route = context.RouteData.Values;
        string area = Convert.ToString(route.GetValueOrDefault("area")) ?? "";
        string controller = Convert.ToString(route.GetValueOrDefault("controller")) ?? "";
        string action = Convert.ToString(route.GetValueOrDefault("action")) ?? "";
        string? feature = Convert.ToString(route.GetValueOrDefault("feature"));

        if (string.IsNullOrWhiteSpace(feature)) feature = context.HttpContext.Request.Query["feature"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(feature)
            && string.Equals(area, "Admin", StringComparison.OrdinalIgnoreCase)
            && string.Equals(controller, "Users", StringComparison.OrdinalIgnoreCase)
            && string.Equals(action, "Index", StringComparison.OrdinalIgnoreCase))
        {
            string? section = context.HttpContext.Request.Query["section"].FirstOrDefault();
            feature = string.IsNullOrWhiteSpace(section) ? "users" : section;
        }

        if (string.Equals(area, "Admin", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(controller, "UsersApi", StringComparison.OrdinalIgnoreCase))
        {
            string? section = action switch
            {
                "GetUsers" => context.HttpContext.Request.Query["section"].FirstOrDefault() ?? "users",
                "SaveUser" or "ResetPassword" => "users",
                "CreateRole" => "roles",
                "SaveRole" => context.HttpContext.Request.Query["section"].FirstOrDefault() ?? "roles",
                _ => null
            };
            if (string.IsNullOrWhiteSpace(section) && action is "GetUsers" or "SaveRole")
                section = action == "GetUsers" ? "users" : "roles";
            if (!string.IsNullOrWhiteSpace(section))
            {
                area = "Admin";
                controller = "Users";
                action = "Index";
                feature = section;
            }
        }

        if (string.Equals(area, "Store", StringComparison.OrdinalIgnoreCase)
            && string.Equals(controller, "StorePaidOrdersDispatchApi", StringComparison.OrdinalIgnoreCase)
            && string.Equals(action, "List", StringComparison.OrdinalIgnoreCase)
            && context.HttpContext.Request.Query["warehouseQueue"] == "true")
        {
            area = "Warehouse";
            controller = "Features";
            action = "Index";
            feature = "order-confirmation";
        }

        if (string.Equals(area, "Store", StringComparison.OrdinalIgnoreCase) &&
            (string.Equals(controller, "Catalog", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(controller, "StoreCatalogAdminApi", StringComparison.OrdinalIgnoreCase)) &&
            string.Equals(action, "ProductDetails", StringComparison.OrdinalIgnoreCase))
        {
            controller = "Catalog";
            action = "Products";
            feature = null;
        }

        if (string.Equals(area, "Store", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(controller, "StoreProductBadgesApi", StringComparison.OrdinalIgnoreCase))
        {
            controller = "Catalog";
            action = "Badges";
            feature = null;
        }

        if (string.Equals(area, "Store", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(controller, "StoreCatalogFeaturesApi", StringComparison.OrdinalIgnoreCase))
        {
            controller = "Catalog";
            action = "Attributes";
            feature = null;
        }

        if (string.Equals(area, "Warehouse", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(controller, "WarehouseDefinitionsApi", StringComparison.OrdinalIgnoreCase))
        {
            action = action switch
            {
                "GetVariantNames" or "GetVariantOptions" => "Get", "CreateVariantName" => "Create",
                "UpdateVariantName" => "Save", _ => action
            };
            feature = null;
        }

        if (string.Equals(controller, "Auth", StringComparison.OrdinalIgnoreCase)
            || (string.IsNullOrEmpty(area)
                && string.Equals(controller, "Home", StringComparison.OrdinalIgnoreCase)))
            return Task.CompletedTask;

        string required = AdminAuthentication.RoutePermission(area, controller, action, feature);
        ClaimsPrincipal user = context.HttpContext.User;
        bool productListAccess = string.Equals(area, "Store", StringComparison.OrdinalIgnoreCase)
                                 && string.Equals(controller, "StoreCatalogAdminApi",
                                     StringComparison.OrdinalIgnoreCase)
                                 && string.Equals(action, "GetSection", StringComparison.OrdinalIgnoreCase)
                                 && string.Equals(Convert.ToString(route.GetValueOrDefault("section")), "products",
                                     StringComparison.OrdinalIgnoreCase)
                                 && user.FindAll("admin_permission").Any(claim => string.Equals(claim.Value,
                                     AdminAuthentication.RoutePermission("Store", "Catalog", "Products"),
                                     StringComparison.OrdinalIgnoreCase));

        if (user.Identity?.IsAuthenticated == true
            && (productListAccess || user.IsInRole("system.full-access") || user.FindAll("admin_permission")
                .Any(claim =>
                    string.Equals(claim.Value, required, StringComparison.OrdinalIgnoreCase))))
            return Task.CompletedTask;

        context.Result = new ForbidResult();

        return Task.CompletedTask;
    }
}
