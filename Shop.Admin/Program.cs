using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Shop.Admin.Security;
using Shop.Database.Domain.Admin.Entities;
using Shop.Database.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddAuthentication("AdminCookie")
    .AddCookie("AdminCookie", options =>
    {
        options.Cookie.Name = "Shop.Admin.Session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.LoginPath = "/auth/login";
        options.AccessDeniedPath = "/auth/denied";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.EventsType = typeof(AdminCookieEvents);
    });
builder.Services.AddAuthorization(options =>
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("admin-login", limiter =>
    {
        limiter.PermitLimit = 5;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
        limiter.AutoReplenishment = true;
    });
});
builder.Services.AddSingleton<PasswordHasher<AdminSystemUser>>();
builder.Services.AddScoped<AdminAuthentication>();
builder.Services.AddScoped<AdminCookieEvents>();
builder.Services.Configure<MvcOptions>(options => options.Filters.Add<AdminPermissionFilter>());

IConfiguration databaseConfiguration = builder.Configuration;
string? connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    string shopSettings = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "Shop", "appsettings.json"));
    IConfiguration sharedSettings = new ConfigurationBuilder()
        .AddJsonFile(shopSettings, optional: true, reloadOnChange: true)
        .Build();

    connectionString = sharedSettings.GetConnectionString("DefaultConnection");
    if (!string.IsNullOrWhiteSpace(connectionString))
    {
        databaseConfiguration = new ConfigurationBuilder()
            .AddConfiguration(builder.Configuration)
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connectionString
            })
            .Build();
    }
}

if (!string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddShopDatabase(databaseConfiguration);
    builder.Services.AddHostedService<AdminBootstrapper>();
}

var app = builder.Build();

app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        context.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate, max-age=0";
        context.Response.Headers.Pragma = "no-cache";
        context.Response.Headers.Expires = "0";
        return Task.CompletedTask;
    });

    await next();
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
