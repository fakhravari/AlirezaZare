using Microsoft.Extensions.Configuration;
using Shop.Database.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddAuthorization();

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
    builder.Services.AddShopDatabase(databaseConfiguration);

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
app.UseAuthorization();
app.MapControllers();
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
