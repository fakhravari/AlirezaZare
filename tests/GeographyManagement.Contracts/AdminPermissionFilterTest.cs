using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Shop.Admin.Security;

internal static class AdminPermissionFilterTest
{
    public static async Task RunAsync()
    {
        await Verify("anonymous is forbidden", new ClaimsPrincipal(new ClaimsIdentity()), "Admin", "Users", "Index", null, true);
        await Verify("authenticated user without a grant is forbidden", Authenticated(), "Admin", "Users", "Index", null, true);
        await Verify("page permission is allowed", Authenticated(new Claim("admin_permission", "Admin:Users:Index:users")), "Admin", "Users", "Index", null, false);
        await Verify("another user page permission is forbidden", Authenticated(new Claim("admin_permission", "Admin:Users:Index:roles")), "Admin", "Users", "Index", null, true);
        await Verify("role page permission is allowed", Authenticated(new Claim("admin_permission", "Admin:Users:Index:roles")), "Admin", "Users", "Index", null, false, querySection: "roles");
        await Verify("user API uses page permission", Authenticated(new Claim("admin_permission", "Admin:Users:Index:permissions")), "Admin", "UsersApi", "GetUsers", null, false, querySection: "permissions");
        await Verify("user API rejects unrelated page permission", Authenticated(new Claim("admin_permission", "Admin:Users:Index:users")), "Admin", "UsersApi", "GetUsers", null, true, querySection: "permissions");
        await Verify("role API uses role permission", Authenticated(new Claim("admin_permission", "Admin:Users:Index:roles")), "Admin", "UsersApi", "SaveRole", null, false, querySection: "roles");
        await Verify("role API can save permission grants", Authenticated(new Claim("admin_permission", "Admin:Users:Index:permissions")), "Admin", "UsersApi", "SaveRole", null, false, querySection: "permissions");
        await Verify("unrelated feature permission is forbidden", Authenticated(new Claim("admin_permission", "Admin:Store:Features:faq")), "Admin", "Store", "Features", "content", true);
        await Verify("exact feature permission is allowed", Authenticated(new Claim("admin_permission", "Admin:Store:Features:content")), "Admin", "Store", "Features", "content", false);
        await Verify("full-access role is allowed", Authenticated(new Claim(ClaimTypes.Role, "system.full-access")), "Warehouse", "Reports", "Index", null, false);
        await Verify("allow-anonymous route is allowed", new ClaimsPrincipal(new ClaimsIdentity()), "Admin", "Users", "Index", null, false, [new AllowAnonymousFilter()]);
        Console.WriteLine("Admin permission filter checks passed (13 cases).");
    }

    private static ClaimsPrincipal Authenticated(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, "test"));

    private static async Task Verify(string name, ClaimsPrincipal user, string area, string controller,
        string action, string? feature, bool expectForbid, IFilterMetadata[]? filters = null,
        string? querySection = null)
    {
        var http = new DefaultHttpContext { User = user };
        var query = new List<string>();
        if (feature is not null) query.Add("feature=" + feature);
        if (querySection is not null) query.Add("section=" + querySection);
        if (query.Count > 0) http.Request.QueryString = new QueryString("?" + string.Join("&", query));
        var route = new RouteData();
        route.Values["area"] = area;
        route.Values["controller"] = controller;
        route.Values["action"] = action;
        var context = new AuthorizationFilterContext(
            new ActionContext(http, route, new ActionDescriptor()), filters ?? []);

        await new AdminPermissionFilter().OnAuthorizationAsync(context);
        if ((context.Result is ForbidResult) != expectForbid)
            throw new InvalidOperationException($"Permission filter case failed: {name}.");
    }
}
