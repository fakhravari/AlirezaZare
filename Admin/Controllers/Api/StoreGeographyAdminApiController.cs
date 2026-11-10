using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Data.Infrastructure.Persistence;

namespace Shop.Admin.Controllers.Api;

[Area("Store"), ApiController, Authorize(Roles = "system.full-access"), Route("api/admin/store/geography")]
public sealed class StoreGeographyAdminApiController(ShopDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var countries = await db.Database.SqlQueryRaw<Place>("SELECT [Id], [NameFa], [NameEn], [CountryCode] AS [Code], CAST(NULL AS bigint) AS [CountryId], CAST(NULL AS bigint) AS [ProvinceId], CAST(NULL AS nvarchar(150)) AS [ParentName], [FlagPath], CAST(NULL AS nvarchar(150)) AS [SourceCountryName], CAST(NULL AS decimal(12,8)) AS [Latitude], CAST(NULL AS decimal(12,8)) AS [Longitude], CAST(NULL AS bit) AS [IsProvinceCenter] FROM [Geo].[Country] ORDER BY [NameFa]").ToListAsync(ct);
        var provinces = await db.Database.SqlQueryRaw<Place>("SELECT p.[Id], p.[NameFa], p.[NameEn], CAST(NULL AS nvarchar(8)) AS [Code], p.[IdCountry] AS [CountryId], CAST(NULL AS bigint) AS [ProvinceId], c.[NameFa] AS [ParentName], CAST(NULL AS nvarchar(500)) AS [FlagPath], CAST(NULL AS nvarchar(150)) AS [SourceCountryName], p.[Latitude], p.[Longitude], CAST(NULL AS bit) AS [IsProvinceCenter] FROM [Geo].[Province] p JOIN [Geo].[Country] c ON c.[Id]=p.[IdCountry] ORDER BY p.[NameFa]").ToListAsync(ct);
        var cities = await db.Database.SqlQueryRaw<Place>("SELECT x.[Id], x.[NameFa], x.[NameEn], CAST(NULL AS nvarchar(8)) AS [Code], x.[IdCountry] AS [CountryId], x.[IdProvince] AS [ProvinceId], CONCAT(c.[NameFa], N' / ', p.[NameFa]) AS [ParentName], CAST(NULL AS nvarchar(500)) AS [FlagPath], x.[SourceCountryName], x.[Latitude], x.[Longitude], x.[IsProvinceCenter] FROM [Geo].[City] x JOIN [Geo].[Province] p ON p.[Id]=x.[IdProvince] JOIN [Geo].[Country] c ON c.[Id]=x.[IdCountry] ORDER BY x.[NameFa]").ToListAsync(ct);
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
        string? flag = string.IsNullOrWhiteSpace(input.FlagPath) ? null : input.FlagPath.Trim();
        if (flag?.Length > 500 || (flag is not null && !flag.StartsWith("content/", StringComparison.OrdinalIgnoreCase) && !(Uri.TryCreate(flag, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))))
            return BadRequest(new { title = "مسیر پرچم باید مسیر content/ یا نشانی HTTP معتبر باشد." });
        if (input.Latitude is < -90 or > 90 || input.Longitude is < -180 or > 180)
            return BadRequest(new { title = "مختصات جغرافیایی معتبر نیست." });
        string? latitude = input.Latitude?.ToString(System.Globalization.CultureInfo.InvariantCulture);
        string? longitude = input.Longitude?.ToString(System.Globalization.CultureInfo.InvariantCulture);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            if (id == null)
            {
                if (kind == "countries") await db.Database.ExecuteSqlInterpolatedAsync($"INSERT [Geo].[Country] ([Id],[NameFa],[NameEn],[CountryCode],[FlagPath]) SELECT COALESCE(MAX([Id]),0)+1,{fa},{en},{code},{flag} FROM [Geo].[Country] WITH (UPDLOCK,HOLDLOCK)", ct);
                else if (kind == "provinces") await db.Database.ExecuteSqlInterpolatedAsync($"INSERT [Geo].[Province] ([Id],[IdCountry],[NameFa],[NameEn],[Latitude],[Longitude]) SELECT COALESCE(MAX([Id]),0)+1,{input.CountryId},{fa},{en},CONVERT(decimal(12,8),{latitude}),CONVERT(decimal(12,8),{longitude}) FROM [Geo].[Province] WITH (UPDLOCK,HOLDLOCK)", ct);
                else
                {
                    var parent = await db.Database.SqlQuery<Parent>($"SELECT [IdCountry], (SELECT [NameFa] FROM [Geo].[Country] WHERE [Id]=p.[IdCountry]) AS [CountryName] FROM [Geo].[Province] p WHERE [Id]={input.ProvinceId}").SingleOrDefaultAsync(ct);
                    if (parent == null || parent.IdCountry != input.CountryId) return BadRequest(new { title = "استان و کشور معتبر را انتخاب کنید." });
                    await db.Database.ExecuteSqlInterpolatedAsync($"INSERT [Geo].[City] ([Id],[IdCountry],[IdProvince],[SourceCountryName],[NameFa],[NameEn],[Latitude],[Longitude],[IsProvinceCenter]) SELECT COALESCE(MAX([Id]),0)+1,{parent.IdCountry},{input.ProvinceId},{parent.CountryName},{fa},{en},CONVERT(decimal(12,8),{latitude}),CONVERT(decimal(12,8),{longitude}),{input.IsProvinceCenter ?? false} FROM [Geo].[City] WITH (UPDLOCK,HOLDLOCK)", ct);
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
                    "countries" => await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE [Geo].[Country] SET [NameFa]={fa},[NameEn]={en},[CountryCode]={code},[FlagPath]=CASE WHEN {input.UpdateDetails}=1 THEN {flag} ELSE [FlagPath] END WHERE [Id]={id}", ct),
                    "provinces" => await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE [Geo].[Province] SET [NameFa]={fa},[NameEn]={en},[Latitude]=CASE WHEN {input.UpdateDetails}=1 THEN CONVERT(decimal(12,8),{latitude}) ELSE [Latitude] END,[Longitude]=CASE WHEN {input.UpdateDetails}=1 THEN CONVERT(decimal(12,8),{longitude}) ELSE [Longitude] END WHERE [Id]={id}", ct),
                    _ => await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE [Geo].[City] SET [NameFa]={fa},[NameEn]={en},[Latitude]=CASE WHEN {input.UpdateDetails}=1 THEN CONVERT(decimal(12,8),{latitude}) ELSE [Latitude] END,[Longitude]=CASE WHEN {input.UpdateDetails}=1 THEN CONVERT(decimal(12,8),{longitude}) ELSE [Longitude] END,[IsProvinceCenter]=CASE WHEN {input.UpdateDetails}=1 THEN {input.IsProvinceCenter ?? false} ELSE [IsProvinceCenter] END WHERE [Id]={id}", ct)
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
    public sealed record Input(string? NameFa, string? NameEn, string? Code, long? CountryId, long? ProvinceId, string? FlagPath = null, decimal? Latitude = null, decimal? Longitude = null, bool? IsProvinceCenter = null, bool UpdateDetails = false);
    public sealed class Place { public long Id { get; set; } public string NameFa { get; set; } = ""; public string NameEn { get; set; } = ""; public string? Code { get; set; } public long? CountryId { get; set; } public long? ProvinceId { get; set; } public string? ParentName { get; set; } public string? FlagPath { get; set; } public string? SourceCountryName { get; set; } public decimal? Latitude { get; set; } public decimal? Longitude { get; set; } public bool? IsProvinceCenter { get; set; } }
    public sealed class Parent { public long IdCountry { get; set; } public string CountryName { get; set; } = ""; }
}
