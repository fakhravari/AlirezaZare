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
        await Verify("exact permission is allowed", Authenticated(new Claim("admin_permission", "Admin:Users:Index")), "Admin", "Users", "Index", null, false);
        await Verify("unrelated feature permission is forbidden", Authenticated(new Claim("admin_permission", "Admin:Store:Features:faq")), "Admin", "Store", "Features", "content", true);
        await Verify("exact feature permission is allowed", Authenticated(new Claim("admin_permission", "Admin:Store:Features:content")), "Admin", "Store", "Features", "content", false);
        await Verify("full-access role is allowed", Authenticated(new Claim(ClaimTypes.Role, "system.full-access")), "Warehouse", "Reports", "Index", null, false);
        await Verify("allow-anonymous route is allowed", new ClaimsPrincipal(new ClaimsIdentity()), "Admin", "Users", "Index", null, false, [new AllowAnonymousFilter()]);
        Console.WriteLine("Admin permission filter checks passed (7 cases).");
    }

    private static ClaimsPrincipal Authenticated(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, "test"));

    private static async Task Verify(string name, ClaimsPrincipal user, string area, string controller,
        string action, string? feature, bool expectForbid, IFilterMetadata[]? filters = null)
    {
        var http = new DefaultHttpContext { User = user };
        if (feature is not null) http.Request.QueryString = new QueryString("?feature=" + feature);
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
