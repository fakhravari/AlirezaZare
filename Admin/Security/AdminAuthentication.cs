using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Shop.Data.Domain.Admin.Entities;
using Shop.Data.Infrastructure.Persistence;

namespace Shop.Admin.Security;

public sealed class AdminAuthentication(ShopDbContext db, PasswordHasher<AdminSystemUser> passwordHasher)
{
    public async Task<ClaimsPrincipal?> SignInAsync(string userName, string password,
        CancellationToken cancellationToken)
    {
        string normalizedName = userName.Trim().ToUpperInvariant();
        AdminSystemUser? user = await db.AdminSystemUsers
            .Include(x => x.WarehouseAccess)
            .Include(x => x.Roles).ThenInclude(x => x.Role)
            .ThenInclude(x => x.Permissions).ThenInclude(x => x.Permission)
            .SingleOrDefaultAsync(x => x.NormalizedUserName == normalizedName && x.IsActive, cancellationToken);

        if (user is null || passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password) ==
            PasswordVerificationResult.Failed)
            return null;

        user.LastLoginAtUtc = DateTime.UtcNow;
        user.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName),
            new("display_name", user.DisplayName),
            new("admin_security_stamp", user.SecurityStamp.ToString("D"))
        };

        foreach (long warehouseId in user.WarehouseAccess.Select(x => x.IdWarehouse))
            claims.Add(new Claim("admin_warehouse", warehouseId.ToString()));

        AdminPermission[] allPermissions =
            await db.AdminPermissions.AsNoTracking().Where(x => x.IsActive).ToArrayAsync(cancellationToken);
        var children = allPermissions.GroupBy(x => x.ParentKey ?? "")
            .ToDictionary(x => x.Key, x => x.ToArray(), StringComparer.OrdinalIgnoreCase);
        var granted = new Dictionary<string, AdminPermission>(StringComparer.OrdinalIgnoreCase);

        void Grant(AdminPermission permission)
        {
            if (!granted.TryAdd(permission.PermissionKey, permission)) return;
            if (children.TryGetValue(permission.PermissionKey, out AdminPermission[]? descendants))
                foreach (AdminPermission descendant in descendants)
                    Grant(descendant);
        }

        foreach (AdminRole role in user.Roles.Select(x => x.Role).Where(x => x.IsActive))
        {
            claims.Add(new Claim(ClaimTypes.Role, role.RoleKey));
            foreach (AdminPermission permission in role.Permissions.Select(x => x.Permission).Where(x => x.IsActive))
                Grant(permission);
        }

        foreach (AdminPermission permission in granted.Values.Where(x => !string.IsNullOrWhiteSpace(x.Area)
                                                                         && !string.IsNullOrWhiteSpace(x.Controller) &&
                                                                         !string.IsNullOrWhiteSpace(x.Action)))
            claims.Add(new Claim("admin_permission",
                RoutePermission(permission.Area, permission.Controller, permission.Action, permission.FeatureKey)));

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
        ShopDbContext db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
        if (await db.AdminSystemUsers.AnyAsync(cancellationToken))
            return;

        AdminRole? fullAccess =
            await db.AdminRoles.SingleOrDefaultAsync(x => x.RoleKey == "system.full-access", cancellationToken);
        if (fullAccess is null)
        {
            logger.LogError("Admin schema is not installed; the configured initial system user was not created.");
            return;
        }

        var user = new AdminSystemUser
        {
            UserName = userName.Trim(),
            NormalizedUserName = userName.Trim().ToUpperInvariant(),
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? userName.Trim() : displayName.Trim(),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        user.PasswordHash = new PasswordHasher<AdminSystemUser>().HashPassword(user, password);
        db.AdminSystemUsers.Add(user);
        db.AdminUserRoles.Add(new AdminUserRole { User = user, Role = fullAccess });
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Created the initial system user from AdminBootstrap configuration.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

public sealed class AdminCookieEvents(ShopDbContext db) : CookieAuthenticationEvents
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

        bool valid = await db.AdminSystemUsers.AsNoTracking().AnyAsync(x =>
            x.Id == userId && x.IsActive && x.SecurityStamp == stamp, context.HttpContext.RequestAborted);
        if (!valid) await RejectAsync(context);
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

        RouteValueDictionary route = context.RouteData.Values;
        string area = Convert.ToString(route.GetValueOrDefault("area")) ?? "";
        string controller = Convert.ToString(route.GetValueOrDefault("controller")) ?? "";
        string action = Convert.ToString(route.GetValueOrDefault("action")) ?? "";
        string? feature = Convert.ToString(route.GetValueOrDefault("feature"));
        if (string.IsNullOrWhiteSpace(feature)) feature = context.HttpContext.Request.Query["feature"].FirstOrDefault();
        if (string.Equals(controller, "Auth", StringComparison.OrdinalIgnoreCase)
            || (string.IsNullOrEmpty(area) && string.Equals(controller, "Home", StringComparison.OrdinalIgnoreCase)))
            return Task.CompletedTask;
        string required = AdminAuthentication.RoutePermission(area, controller, action, feature);
        ClaimsPrincipal user = context.HttpContext.User;

        if (user.Identity?.IsAuthenticated == true
            && (user.IsInRole("system.full-access") || user.FindAll("admin_permission").Any(claim =>
                string.Equals(claim.Value, required, StringComparison.OrdinalIgnoreCase))))
            return Task.CompletedTask;

        context.Result = new ForbidResult();
        return Task.CompletedTask;
    }
}