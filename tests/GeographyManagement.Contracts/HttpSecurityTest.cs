using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Shop.Database.Domain.Admin.Entities;
using Shop.Database.Infrastructure.Persistence;

internal static class HttpSecurityTest
{
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
