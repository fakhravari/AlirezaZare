using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Database.Infrastructure.Persistence;

namespace Shop.Admin.Controllers.Api;

[Area("Store"), ApiController, Authorize(Roles = "system.full-access"), Route("api/admin/store/geography")]
public sealed class StoreGeographyAdminApiController(ShopDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var countries = await db.Database.SqlQueryRaw<Place>("SELECT [Id], [NameFa], [NameEn], [CountryCode] AS [Code], CAST(NULL AS bigint) AS [CountryId], CAST(NULL AS bigint) AS [ProvinceId], CAST(NULL AS nvarchar(150)) AS [ParentName] FROM [Geo].[Country] ORDER BY [NameFa]").ToListAsync(ct);
        var provinces = await db.Database.SqlQueryRaw<Place>("SELECT p.[Id], p.[NameFa], p.[NameEn], CAST(NULL AS nvarchar(8)) AS [Code], p.[IdCountry] AS [CountryId], CAST(NULL AS bigint) AS [ProvinceId], c.[NameFa] AS [ParentName] FROM [Geo].[Province] p JOIN [Geo].[Country] c ON c.[Id]=p.[IdCountry] ORDER BY p.[NameFa]").ToListAsync(ct);
        var cities = await db.Database.SqlQueryRaw<Place>("SELECT x.[Id], x.[NameFa], x.[NameEn], CAST(NULL AS nvarchar(8)) AS [Code], x.[IdCountry] AS [CountryId], x.[IdProvince] AS [ProvinceId], CONCAT(c.[NameFa], N' / ', p.[NameFa]) AS [ParentName] FROM [Geo].[City] x JOIN [Geo].[Province] p ON p.[Id]=x.[IdProvince] JOIN [Geo].[Country] c ON c.[Id]=x.[IdCountry] ORDER BY x.[NameFa]").ToListAsync(ct);
        return Ok(new { countries, provinces, cities });
    }
    [HttpPost("{kind}"), ValidateAntiForgeryToken]
    public Task<IActionResult> Create(string kind, Input input, CancellationToken ct) => Save(kind, null, input, ct);
    [HttpPut("{kind}/{id:long}"), ValidateAntiForgeryToken]
    public Task<IActionResult> Update(string kind, long id, Input input, CancellationToken ct) => Save(kind, id, input, ct);
    private async Task<IActionResult> Save(string kind, long? id, Input input, CancellationToken ct)
    {
        string fa = input.NameFa?.Trim() ?? "", en = input.NameEn?.Trim() ?? "", code = input.Code?.Trim().ToUpperInvariant() ?? "";
        int maxNameLength = kind == "cities" ? 200 : 150;
        if (kind is not ("countries" or "provinces" or "cities") || fa.Length < 1 || fa.Length > maxNameLength || en.Length < 1 || en.Length > maxNameLength || (kind == "countries" && (code.Length is < 1 or > 8 || !code.All(char.IsAsciiLetterOrDigit)))) return BadRequest(new { title = "اطلاعات واردشده معتبر نیست." });
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            if (id == null)
            {
                if (kind == "countries") await db.Database.ExecuteSqlInterpolatedAsync($"INSERT [Geo].[Country] ([Id],[NameFa],[NameEn],[CountryCode]) SELECT COALESCE(MAX([Id]),0)+1,{fa},{en},{code} FROM [Geo].[Country] WITH (UPDLOCK,HOLDLOCK)", ct);
                else if (kind == "provinces") await db.Database.ExecuteSqlInterpolatedAsync($"INSERT [Geo].[Province] ([Id],[IdCountry],[NameFa],[NameEn]) SELECT COALESCE(MAX([Id]),0)+1,{input.CountryId},{fa},{en} FROM [Geo].[Province] WITH (UPDLOCK,HOLDLOCK)", ct);
                else
                {
                    var parent = await db.Database.SqlQuery<Parent>($"SELECT [IdCountry], (SELECT [NameFa] FROM [Geo].[Country] WHERE [Id]=p.[IdCountry]) AS [CountryName] FROM [Geo].[Province] p WHERE [Id]={input.ProvinceId}").SingleOrDefaultAsync(ct);
                    if (parent == null || parent.IdCountry != input.CountryId) return BadRequest(new { title = "استان و کشور معتبر را انتخاب کنید." });
                    await db.Database.ExecuteSqlInterpolatedAsync($"INSERT [Geo].[City] ([Id],[IdCountry],[IdProvince],[SourceCountryName],[NameFa],[NameEn]) SELECT COALESCE(MAX([Id]),0)+1,{parent.IdCountry},{input.ProvinceId},{parent.CountryName},{fa},{en} FROM [Geo].[City] WITH (UPDLOCK,HOLDLOCK)", ct);
                }
            }
            else
            {
                if (kind == "countries" && code != "IR")
                {
                    int iran = await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM [Geo].[Country] WHERE [Id]={id} AND [CountryCode]=N'IR'").SingleAsync(ct);
                    if (iran != 0) return Conflict(new { title = "کد کشور ایران برای ثبت نشانی‌ها باید IR باقی بماند." });
                }
                int count = kind switch {
                    "countries" => await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE [Geo].[Country] SET [NameFa]={fa},[NameEn]={en},[CountryCode]={code} WHERE [Id]={id}", ct),
                    "provinces" => await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE [Geo].[Province] SET [NameFa]={fa},[NameEn]={en} WHERE [Id]={id}", ct),
                    _ => await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE [Geo].[City] SET [NameFa]={fa},[NameEn]={en} WHERE [Id]={id}", ct)
                };
                if (count == 0) return NotFound(new { title = "رکورد پیدا نشد." });
            }
            await transaction.CommitAsync(ct); return Ok(new { message = "اطلاعات ذخیره شد." });
        }
        catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number is 2601 or 2627 or 547 or 515) { return Conflict(new { title = "اطلاعات تکراری است یا وابستگی معتبر نیست." }); }
    }
    [HttpDelete("{kind}/{id:long}"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string kind, long id, CancellationToken ct)
    {
        try
        {
            int count = kind switch {
                "countries" => await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [Geo].[Country] WHERE [Id]={id}", ct),
                "provinces" => await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [Geo].[Province] WHERE [Id]={id}", ct),
                "cities" => await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [Geo].[City] WHERE [Id]={id}", ct),
                _ => -1
            };
            return count < 0 ? BadRequest() : count == 0 ? NotFound(new { title = "رکورد پیدا نشد." }) : Ok(new { message = "رکورد حذف شد." });
        }
        catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number == 547) { return Conflict(new { title = "این مورد در داده‌های دیگر استفاده شده و قابل حذف نیست." }); }
    }
    public sealed record Input(string? NameFa, string? NameEn, string? Code, long? CountryId, long? ProvinceId);
    public sealed class Place { public long Id { get; set; } public string NameFa { get; set; } = ""; public string NameEn { get; set; } = ""; public string? Code { get; set; } public long? CountryId { get; set; } public long? ProvinceId { get; set; } public string? ParentName { get; set; } }
    public sealed class Parent { public long IdCountry { get; set; } public string CountryName { get; set; } = ""; }
}
