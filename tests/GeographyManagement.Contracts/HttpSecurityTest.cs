using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Shop.Data.Domain.Admin.Entities;
using Shop.Data.Infrastructure.Persistence;

internal static class HttpSecurityTest
{
    public static async Task RunLimitedRoleAuthorizationAsync(ShopDbContext db, string connection, string adminDll)
    {
        if (!File.Exists(adminDll)) throw new FileNotFoundException("Build the Admin project before this security test.", adminDll);

        string suffix = Guid.NewGuid().ToString("N");
        string roleKey = "test.limited." + suffix;
        string userName = "limited-" + suffix;
        string password = "Test-" + Guid.NewGuid().ToString("N");
        AdminRole? temporaryRole = null;
        AdminSystemUser? temporaryUser = null;
        Process? server = null;

        try
        {
            AdminPermission[] grantedPermissions = await db.AdminPermissions.Where(permission => permission.IsActive &&
                ((permission.Area == "Store" && permission.Controller == "StoreFeedbackApi"
                  && permission.Action == "Reviews" && permission.FeatureKey == null)
                 || (permission.Area == "Store" && permission.Controller == "Features"
                     && permission.Action == "Index" && permission.FeatureKey == "reviews"))).ToArrayAsync();
            if (grantedPermissions.Length != 2)
                throw new InvalidOperationException("Both review API and review feature permissions must exist and be active.");
            temporaryRole = new AdminRole { RoleKey = roleKey, Name = "Temporary limited security test", IsActive = true };
            temporaryUser = new AdminSystemUser
            {
                UserName = userName,
                NormalizedUserName = userName.ToUpperInvariant(),
                DisplayName = "Temporary authorization test",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            temporaryUser.PasswordHash = new PasswordHasher<AdminSystemUser>().HashPassword(temporaryUser, password);
            db.AdminRoles.Add(temporaryRole);
            db.AdminSystemUsers.Add(temporaryUser);
            foreach (AdminPermission permission in grantedPermissions)
                db.AdminRolePermissions.Add(new AdminRolePermission { Role = temporaryRole, Permission = permission });
            db.AdminUserRoles.Add(new AdminUserRole { User = temporaryUser, Role = temporaryRole });
            await db.SaveChangesAsync();

            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();

            var start = new ProcessStartInfo("dotnet")
            {
                UseShellExecute = false,
                WorkingDirectory = Path.GetFullPath("Admin")
            };
            start.ArgumentList.Add(adminDll);
            start.Environment["ConnectionStrings__DefaultConnection"] = connection;
            start.Environment["Logging__LogLevel__Default"] = "Warning";
            start.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
            start.Environment["ASPNETCORE_URLS"] = $"https://localhost:{port}";
            server = Process.Start(start) ?? throw new Exception("Admin test server did not start.");

            HttpClient Client() => new(new HttpClientHandler
            {
                AllowAutoRedirect = false,
                CookieContainer = new CookieContainer(),
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            }) { BaseAddress = new Uri($"https://localhost:{port}"), Timeout = TimeSpan.FromSeconds(15) };

            using var anonymous = Client();
            HttpResponseMessage? probe = null;
            for (int attempt = 0; attempt < 80; attempt++)
            {
                try { probe = await anonymous.GetAsync("/api/admin/store/feedback/reviews"); break; }
                catch (HttpRequestException)
                {
                    if (server.HasExited) throw new Exception("Admin test server exited before becoming ready.");
                    await Task.Delay(250);
                }
            }
            if (probe?.StatusCode != HttpStatusCode.Unauthorized)
                throw new Exception("Anonymous access to feedback API was not rejected with 401.");

            async Task<string> Token(HttpClient client)
            {
                string html = await client.GetStringAsync("/auth/login");
                var match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
                return match.Success ? WebUtility.HtmlDecode(match.Groups[1].Value)
                    : throw new Exception("Login anti-forgery token was missing.");
            }

            using var restricted = Client();
            string csrf = await Token(restricted);
            using var login = await restricted.PostAsync("/auth/login", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["UserName"] = userName,
                ["Password"] = password,
                ["__RequestVerificationToken"] = csrf
            }));
            if (login.StatusCode != HttpStatusCode.Redirect)
                throw new Exception("Temporary limited account could not sign in.");

            if ((await restricted.GetAsync("/api/admin/store/feedback/reviews")).StatusCode != HttpStatusCode.OK)
                throw new Exception("Limited role could not access its granted reviews permission.");
            if ((await restricted.GetAsync("/api/admin/store/feedback/questions")).StatusCode != HttpStatusCode.Forbidden)
                throw new Exception("Limited role could access the ungranted questions permission.");
            if ((await restricted.GetAsync("/Store/Features?feature=reviews")).StatusCode != HttpStatusCode.OK)
                throw new Exception("Limited role could not open its granted admin feature page.");
            HttpResponseMessage ungrantedPage = await restricted.GetAsync("/Store/Features?feature=questions");
            if (ungrantedPage.StatusCode != HttpStatusCode.Redirect || ungrantedPage.Headers.Location?.AbsolutePath != "/auth/denied")
                throw new Exception($"An ungranted admin feature did not redirect to the access-denied page (HTTP {(int)ungrantedPage.StatusCode}, {ungrantedPage.Headers.Location}).");

            Console.WriteLine("HTTP role authorization passed: anonymous 401; granted review routes 200; ungranted question API 403 and page redirects to /auth/denied.");
        }
        finally
        {
            if (server is { HasExited: false })
            {
                server.Kill(entireProcessTree: true);
                await server.WaitForExitAsync();
            }
            server?.Dispose();

            if (temporaryUser is not null)
            {
                db.AdminUserRoles.RemoveRange(await db.AdminUserRoles.Where(item => item.IdUser == temporaryUser.Id).ToArrayAsync());
                db.AdminUserWarehouseAccesses.RemoveRange(await db.AdminUserWarehouseAccesses.Where(item => item.IdUser == temporaryUser.Id).ToArrayAsync());
                db.AdminSystemUsers.Remove(temporaryUser);
            }
            if (temporaryRole is not null)
            {
                db.AdminRolePermissions.RemoveRange(await db.AdminRolePermissions.Where(item => item.IdRole == temporaryRole.Id).ToArrayAsync());
                db.AdminRoles.Remove(temporaryRole);
            }
            await db.SaveChangesAsync();
            if (await db.AdminSystemUsers.AnyAsync(item => item.NormalizedUserName == userName.ToUpperInvariant())
                || await db.AdminRoles.AnyAsync(item => item.RoleKey == roleKey)
                || await db.AdminRolePermissions.AnyAsync(item => item.Role.RoleKey == roleKey)
                || await db.AdminUserRoles.AnyAsync(item => item.User.NormalizedUserName == userName.ToUpperInvariant()))
                throw new InvalidOperationException("Temporary security test records were not fully removed.");
        }
    }

    public static async Task RunAsync(ShopDbContext db, string connection, string adminDll)
    {
        string suffix = Guid.NewGuid().ToString("N"), password = "Test-" + Guid.NewGuid().ToString("N");
        var role = await db.AdminRoles.SingleAsync(x => x.RoleKey == "system.full-access");
        AdminSystemUser NewUser(string prefix) { var user = new AdminSystemUser { UserName = prefix + suffix, NormalizedUserName = (prefix + suffix).ToUpperInvariant(), DisplayName = "HTTP test", IsActive = true, CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow }; user.PasswordHash = new PasswordHasher<AdminSystemUser>().HashPassword(user, password); return user; }
        var full = NewUser("full-"); var limited = NewUser("limited-");
        db.AdminSystemUsers.AddRange(full, limited); db.AdminUserRoles.Add(new AdminUserRole { User = full, Role = role });
        Process? server = null;
        try
        {
            await db.SaveChangesAsync();
            using var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start(); int port = ((IPEndPoint)listener.LocalEndpoint).Port; listener.Stop();
            var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, WorkingDirectory = Path.GetFullPath("Shop.Admin") };
            start.ArgumentList.Add(Path.GetFullPath(adminDll)); start.Environment["ConnectionStrings__DefaultConnection"] = connection;
            start.Environment["Logging__LogLevel__Default"] = "Warning"; start.Environment["ASPNETCORE_ENVIRONMENT"] = "Development"; start.Environment["ASPNETCORE_URLS"] = $"https://localhost:{port}";
            server = Process.Start(start) ?? throw new Exception("Admin server did not start.");
            HttpClient Client() => new(new HttpClientHandler { AllowAutoRedirect = false, CookieContainer = new CookieContainer(), ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator }) { BaseAddress = new Uri($"https://localhost:{port}"), Timeout = TimeSpan.FromSeconds(10) };
            using var anonymous = Client();
            HttpResponseMessage? probe = null;
            for (int i = 0; i < 80; i++) { try { probe = await anonymous.GetAsync("/api/admin/store/geography"); break; } catch (HttpRequestException) { if (server.HasExited) throw new Exception("Admin server exited early."); await Task.Delay(250); } }
            if (probe?.StatusCode != HttpStatusCode.Unauthorized) throw new Exception("Anonymous API access was not rejected.");
            async Task<string> Token(HttpClient client, string path) { string html = await client.GetStringAsync(path); var match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\""); return match.Success ? WebUtility.HtmlDecode(match.Groups[1].Value) : throw new Exception("Anti-forgery token missing."); }
            async Task Login(HttpClient client, AdminSystemUser user) { string token = await Token(client, "/auth/login"); var response = await client.PostAsync("/auth/login", new FormUrlEncodedContent(new Dictionary<string,string> { ["UserName"] = user.UserName, ["Password"] = password, ["__RequestVerificationToken"] = token })); if (response.StatusCode != HttpStatusCode.Redirect) throw new Exception("HTTP login failed."); }
            using var restricted = Client(); await Login(restricted, limited);
            if ((await restricted.GetAsync("/api/admin/store/geography")).StatusCode != HttpStatusCode.Forbidden) throw new Exception("Limited user could read geography.");
            using var manager = Client(); await Login(manager, full);
            if ((await manager.GetAsync("/api/admin/store/geography")).StatusCode != HttpStatusCode.OK) throw new Exception("Manager could not read geography.");
            var body = new { nameFa = "HTTP", nameEn = "HTTP", code = "IR", countryId = (long?)null, provinceId = (long?)null };
            if ((await manager.PostAsJsonAsync("/api/admin/store/geography/countries", body)).StatusCode != HttpStatusCode.BadRequest) throw new Exception("Request without anti-forgery token was accepted.");
            if ((await manager.PutAsJsonAsync("/api/admin/store/geography/countries/0", body)).StatusCode != HttpStatusCode.BadRequest) throw new Exception("Update without anti-forgery token was accepted.");
            if ((await manager.DeleteAsync("/api/admin/store/geography/countries/0")).StatusCode != HttpStatusCode.BadRequest) throw new Exception("Delete without anti-forgery token was accepted.");
            string csrf = await Token(manager, "/Store/Features/Index?feature=geography");
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/admin/store/geography/countries") { Content = JsonContent.Create(body) }; request.Headers.Add("RequestVerificationToken", csrf);
            var valid = await manager.SendAsync(request); if (valid.StatusCode != HttpStatusCode.Conflict) throw new Exception($"Valid anti-forgery request returned {(int)valid.StatusCode}.");
            using var delete = new HttpRequestMessage(HttpMethod.Delete, "/api/admin/store/geography/countries/0"); delete.Headers.Add("RequestVerificationToken", csrf);
            if ((await manager.SendAsync(delete)).StatusCode != HttpStatusCode.NotFound) throw new Exception("Valid anti-forgery delete did not reach the controller.");
            Console.WriteLine("Geography HTTP authentication, authorization and anti-forgery checks passed.");
        }
        finally
        {
            if (server is { HasExited: false }) { server.Kill(entireProcessTree: true); await server.WaitForExitAsync(); } server?.Dispose();
            foreach (var user in new[] { full, limited }) { if (user.Id == 0) continue; await db.AdminUserRoles.Where(x => x.IdUser == user.Id).ExecuteDeleteAsync(); await db.AdminSystemUsers.Where(x => x.Id == user.Id && x.UserName == user.UserName).ExecuteDeleteAsync(); }
        }
    }
}
