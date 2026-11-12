using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FluentFTP;
using Shop.Admin.Services;
using Shop.Data.Domain.Store.Entities;
using Shop.Data.Infrastructure;
using Shop.Data.Infrastructure.Persistence;

var repo = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory()));
var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { ContentRootPath = repo, ApplicationName = "Admin" });
builder.Configuration.Sources.Clear();
builder.Configuration.AddJsonFile(Path.Combine(repo, "Admin", "appsettings.json"), optional: false)
    .AddEnvironmentVariables();
builder.Services.AddShopDatabase(builder.Configuration);
builder.Services.AddDataProtection().SetApplicationName("Admin");
builder.Services.AddScoped<FtpMediaStorage>();
using var host = builder.Build();
using var scope = host.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
var storage = scope.ServiceProvider.GetRequiredService<FtpMediaStorage>();
var cancellation = CancellationToken.None;

var sql = await File.ReadAllTextAsync(Path.Combine(repo, "Shop", "Features", "Store", "Database", "Migrations", "060-order-payment-bank.sql"), cancellation);
await db.Database.ExecuteSqlRawAsync(sql, cancellation);
var assets = new (string Code, string File)[]
{
    ("snapp-pay", "snapp-pay.svg")
};

using var ftp = await storage.ConnectAsync(cancellation);
int uploaded = 0, alreadyMapped = 0;
foreach (var asset in assets)
{
    var bank = await db.StoreBanks.SingleAsync(x => x.Code == asset.Code, cancellation);
    if (!string.IsNullOrWhiteSpace(bank.LogoPath) ) { alreadyMapped++; continue; }
    var bytes = await File.ReadAllBytesAsync(Path.Combine(repo, ".tmp-payment-icons", asset.File), cancellation);
    string extension = Path.GetExtension(asset.File);
    string path = $"content/payments/{asset.Code}-{Guid.NewGuid():N}{extension}";
    var status = await ftp.UploadBytes(bytes, "/" + path, FtpRemoteExists.Skip, true, null, cancellation);
    if (status != FtpStatus.Success) throw new IOException($"Upload failed for {asset.Code}.");
    try
    {
        bank.LogoPath = path;
        await db.SaveChangesAsync(cancellation);
        uploaded++;
    }
    catch
    {
        try { await ftp.DeleteFile("/" + path, cancellation); } catch { }
        throw;
    }
}

int total = await db.StoreBanks.CountAsync(cancellation);
int orders = await db.StoreCustomerOrders.CountAsync(cancellation);
int linkedOrders = await db.StoreCustomerOrders.CountAsync(cancellation);
Console.WriteLine($"Orders with a bank link: {linkedOrders}/{orders}; current Zarinpal Id: {await db.StoreBanks.Where(x => x.Code == "zarinpal").Select(x => x.Id).SingleAsync(cancellation)}.");
Console.WriteLine($"Migrations applied idempotently. Uploaded and mapped {uploaded} logos; {alreadyMapped} were already mapped; {total} providers are in the catalog.");






