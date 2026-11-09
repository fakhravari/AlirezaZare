using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Shop.Admin.Controllers.Api;
using Shop.Database.Infrastructure.Persistence;

string cs = Environment.GetEnvironmentVariable("SHOP_TEST_CONNECTION") ?? throw new InvalidOperationException("SHOP_TEST_CONNECTION is required.");
if (!new SqlConnectionStringBuilder(cs).InitialCatalog.StartsWith("ShopVerify_", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("A ShopVerify_ database is required.");
await using var db = new ShopDbContext(new DbContextOptionsBuilder<ShopDbContext>().UseSqlServer(cs).Options);
var controller = new StoreGeographyAdminApiController(db);
string key = "X" + Guid.NewGuid().ToString("N")[..7].ToUpperInvariant();
string name = "Geography test " + key;
long country = 0, province = 0, city = 0;
try
{
    var input = new StoreGeographyAdminApiController.Input(name, name, key, null, null);
    if (await controller.Create("countries", input with { NameFa = new string('a', 151) }, default) is not BadRequestObjectResult) throw new Exception("Oversized country name accepted.");
    if (await controller.Create("countries", input, default) is not OkObjectResult) throw new Exception("Country create failed.");
    country = await db.Database.SqlQuery<long>($"SELECT [Id] AS [Value] FROM [Geo].[Country] WHERE [CountryCode]={key}").SingleAsync();
    if (await controller.Create("countries", input, default) is not ConflictObjectResult) throw new Exception("Duplicate country code accepted.");
    if (await controller.Update("countries", country, input with { NameEn = "Updated country" }, default) is not OkObjectResult) throw new Exception("Country update failed.");
    if (await db.Database.SqlQuery<string>($"SELECT [NameEn] AS [Value] FROM [Geo].[Country] WHERE [Id]={country}").SingleAsync() != "Updated country") throw new Exception("Country update not persisted.");
    if (await controller.Create("provinces", input with { CountryId = country }, default) is not OkObjectResult) throw new Exception("Province create failed.");
    province = await db.Database.SqlQuery<long>($"SELECT [Id] AS [Value] FROM [Geo].[Province] WHERE [IdCountry]={country} AND [NameFa]={name}").SingleAsync();
    if (await controller.Update("provinces", province, input with { NameEn = "Updated province", CountryId = country + 1000000 }, default) is not OkObjectResult) throw new Exception("Province update failed.");
    if (await db.Database.SqlQuery<long>($"SELECT [IdCountry] AS [Value] FROM [Geo].[Province] WHERE [Id]={province}").SingleAsync() != country) throw new Exception("Province parent changed during update.");
    if (await controller.Create("cities", input with { CountryId = country + 1000000, ProvinceId = province }, default) is not BadRequestObjectResult) throw new Exception("Mismatched parent accepted.");
    if (await controller.Create("cities", input with { CountryId = country, ProvinceId = province }, default) is not OkObjectResult) throw new Exception("City create failed.");
    city = await db.Database.SqlQuery<long>($"SELECT [Id] AS [Value] FROM [Geo].[City] WHERE [IdProvince]={province} AND [NameFa]={name}").SingleAsync();
    if (await controller.Update("cities", city, input with { NameFa = new string('a', 200), CountryId = 1, ProvinceId = 1 }, default) is not OkObjectResult) throw new Exception("Valid city name rejected.");
    if (await controller.Update("cities", city, input with { NameFa = new string('a', 201) }, default) is not BadRequestObjectResult) throw new Exception("Oversized city name accepted.");
    var cityParent = await db.Database.SqlQuery<long>($"SELECT [IdProvince] AS [Value] FROM [Geo].[City] WHERE [Id]={city}").SingleAsync();
    if (cityParent != province) throw new Exception("City parent changed during update.");
    if (await controller.Delete("countries", country, default) is not ConflictObjectResult || await controller.Delete("provinces", province, default) is not ConflictObjectResult) throw new Exception("Referenced parent deletion accepted.");
    if (await controller.List(default) is not OkObjectResult) throw new Exception("Geography list failed.");
    if (await controller.Delete("cities", city, default) is not OkObjectResult) throw new Exception("City delete failed."); city = 0;
    if (await controller.Delete("provinces", province, default) is not OkObjectResult) throw new Exception("Province delete failed."); province = 0;
    if (await controller.Delete("countries", country, default) is not OkObjectResult) throw new Exception("Country delete failed."); country = 0;
    Console.WriteLine("Geography CRUD, validation and dependency checks passed.");
}
finally
{
    if (city != 0) await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [Geo].[City] WHERE [Id]={city} AND [IdProvince]={province}");
    if (province != 0) await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [Geo].[Province] WHERE [Id]={province} AND [IdCountry]={country}");
    if (country != 0) await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [Geo].[Country] WHERE [Id]={country} AND [CountryCode]={key}");
}

if (Environment.GetEnvironmentVariable("SHOP_TEST_ADMIN_DLL") is { Length: > 0 } adminDll) await HttpSecurityTest.RunAsync(db, cs, adminDll);
