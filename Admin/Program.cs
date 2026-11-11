using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Shop.Admin.Security;
using Shop.Admin.Filters;
using Shop.Data.Domain.Admin.Entities;
using Shop.Data.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddScoped<MediaCdnViewDataFilter>();
builder.Services.AddControllersWithViews(options => options.Filters.AddService<MediaCdnViewDataFilter>());
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
builder.Services.AddScoped<Shop.Admin.Services.FtpMediaStorage>();
builder.Services.AddScoped<Shop.Admin.Services.WarehouseStockLedger>();
builder.Services.Configure<MvcOptions>(options => options.Filters.Add<AdminPermissionFilter>());

builder.Services.AddShopDatabase(builder.Configuration);
builder.Services.AddHostedService<AdminBootstrapper>();

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
