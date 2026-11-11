using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Data.Domain.Store.Entities;
using Shop.Data.Infrastructure.Persistence;

namespace Shop.Admin.Controllers.Api;

[Area("Store"), ApiController, Authorize(Roles = "system.full-access"), Route("api/admin/store/geography")]
public sealed class StoreGeographyAdminApiController(ShopDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var countries = await db.GeoCountries.AsNoTracking().OrderBy(x => x.NameFa)
            .Select(x => new Place(x.Id, x.NameFa, x.NameEn, x.CountryCode, null, null, null, x.FlagPath, null, null, null, null)).ToListAsync(ct);
        var provinces = await (from province in db.GeoProvinces.AsNoTracking()
                               join country in db.GeoCountries.AsNoTracking() on province.IdCountry equals country.Id
                               orderby province.NameFa
                               select new Place(province.Id, province.NameFa, province.NameEn, null, province.IdCountry, null,
                                   country.NameFa, null, null, province.Latitude, province.Longitude, null)).ToListAsync(ct);
        var cities = await (from city in db.GeoCities.AsNoTracking()
                            join province in db.GeoProvinces.AsNoTracking() on city.IdProvince equals province.Id
                            join country in db.GeoCountries.AsNoTracking() on city.IdCountry equals country.Id
                            orderby city.NameFa
                            select new Place(city.Id, city.NameFa, city.NameEn, null, city.IdCountry, city.IdProvince,
                                country.NameFa + " / " + province.NameFa, null, city.SourceCountryName,
                                city.Latitude, city.Longitude, city.IsProvinceCenter)).ToListAsync(ct);
        return Ok(new { countries, provinces, cities });
    }

    [HttpPost("{kind}"), ValidateAntiForgeryToken]
    public Task<IActionResult> Create(string kind, Input input, CancellationToken ct) => Save(kind, null, input, ct);

    [HttpPut("{kind}/{id:long}"), ValidateAntiForgeryToken]
    public Task<IActionResult> Update(string kind, long id, Input input, CancellationToken ct) => Save(kind, id, input, ct);

    private async Task<IActionResult> Save(string kind, long? id, Input input, CancellationToken ct)
    {
        string fa = input.NameFa?.Trim() ?? "", en = input.NameEn?.Trim() ?? "", code = input.Code?.Trim().ToUpperInvariant() ?? "";
        if (kind is not ("countries" or "provinces" or "cities")) return BadRequest();
        int maxNameLength = kind == "cities" ? 200 : 150;
        if (fa.Length < 1 || fa.Length > maxNameLength || en.Length < 1 || en.Length > maxNameLength ||
            (kind == "countries" && (code.Length is < 1 or > 8 || !code.All(char.IsAsciiLetterOrDigit))))
            return BadRequest(new { title = "اطلاعات واردشده معتبر نیست." });
        string? flag = string.IsNullOrWhiteSpace(input.FlagPath) ? null : input.FlagPath.Trim();
        if (flag?.Length > 500 || (flag is not null && !flag.StartsWith("content/", StringComparison.OrdinalIgnoreCase) &&
            !(Uri.TryCreate(flag, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https")))
            return BadRequest(new { title = "مسیر پرچم باید مسیر content/ یا نشانی HTTP معتبر باشد." });
        if (input.Latitude is < -90 or > 90 || input.Longitude is < -180 or > 180)
            return BadRequest(new { title = "مختصات جغرافیایی معتبر نیست." });

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            if (id is null)
            {
                switch (kind)
                {
                    case "countries":
                        db.GeoCountries.Add(new GeoCountry
                        {
                            Id = await db.GeoCountries.Select(x => (long?)x.Id).MaxAsync(ct) + 1 ?? 1,
                            CountryCode = code, NameFa = fa, NameEn = en, FlagPath = flag
                        });
                        break;
                    case "provinces":
                        if (input.CountryId is not long countryId || !await db.GeoCountries.AnyAsync(x => x.Id == countryId, ct))
                            return BadRequest(new { title = "کشور معتبر را انتخاب کنید." });
                        db.GeoProvinces.Add(new GeoProvince
                        {
                            Id = await db.GeoProvinces.Select(x => (long?)x.Id).MaxAsync(ct) + 1 ?? 1,
                            IdCountry = countryId, NameFa = fa, NameEn = en,
                            Latitude = input.Latitude, Longitude = input.Longitude
                        });
                        break;
                    case "cities":
                        var parent = await db.GeoProvinces.Where(x => x.Id == input.ProvinceId)
                            .Join(db.GeoCountries, province => province.IdCountry, country => country.Id,
                                (province, country) => new { province.Id, province.IdCountry, CountryName = country.NameFa })
                            .SingleOrDefaultAsync(ct);
                        if (parent is null || parent.IdCountry != input.CountryId)
                            return BadRequest(new { title = "استان و کشور معتبر را انتخاب کنید." });
                        db.GeoCities.Add(new GeoCity
                        {
                            Id = await db.GeoCities.Select(x => (long?)x.Id).MaxAsync(ct) + 1 ?? 1,
                            IdCountry = parent.IdCountry, IdProvince = parent.Id,
                            SourceCountryName = parent.CountryName, NameFa = fa, NameEn = en,
                            Latitude = input.Latitude, Longitude = input.Longitude,
                            IsProvinceCenter = input.IsProvinceCenter ?? false
                        });
                        break;
                }
            }
            else
            {
                if (kind == "countries")
                {
                    var country = await db.GeoCountries.SingleOrDefaultAsync(x => x.Id == id, ct);
                    if (country is null) return NotFound(new { title = "رکورد پیدا نشد." });
                    if (country.CountryCode == "IR" && code != "IR")
                        return Conflict(new { title = "کد کشور ایران برای ثبت نشانی‌ها باید IR باقی بماند." });
                    country.NameFa = fa; country.NameEn = en; country.CountryCode = code;
                    if (input.UpdateDetails) country.FlagPath = flag;
                }
                else if (kind == "provinces")
                {
                    var province = await db.GeoProvinces.SingleOrDefaultAsync(x => x.Id == id, ct);
                    if (province is null) return NotFound(new { title = "رکورد پیدا نشد." });
                    province.NameFa = fa; province.NameEn = en;
                    if (input.UpdateDetails) { province.Latitude = input.Latitude; province.Longitude = input.Longitude; }
                }
                else
                {
                    var city = await db.GeoCities.SingleOrDefaultAsync(x => x.Id == id, ct);
                    if (city is null) return NotFound(new { title = "رکورد پیدا نشد." });
                    city.NameFa = fa; city.NameEn = en;
                    if (input.UpdateDetails)
                    {
                        city.Latitude = input.Latitude; city.Longitude = input.Longitude;
                        city.IsProvinceCenter = input.IsProvinceCenter ?? false;
                    }
                }
            }
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return Ok(new { message = "اطلاعات ذخیره شد." });
        }
        catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 2601 or 2627 or 547 or 515 })
        {
            return Conflict(new { title = "اطلاعات تکراری است یا وابستگی معتبر نیست." });
        }
    }

    [HttpDelete("{kind}/{id:long}"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string kind, long id, CancellationToken ct)
    {
        int deleted = kind switch
        {
            "countries" => await DeleteAsync(db.GeoCountries, id, ct),
            "provinces" => await DeleteAsync(db.GeoProvinces, id, ct),
            "cities" => await DeleteAsync(db.GeoCities, id, ct),
            _ => -1
        };
        return deleted == -1 ? BadRequest() : deleted == -2
            ? Conflict(new { title = "این مورد در داده‌های دیگر استفاده شده و قابل حذف نیست." }) : deleted == 0
            ? NotFound(new { title = "رکورد پیدا نشد." })
            : Ok(new { message = "رکورد حذف شد." });
    }

    private async Task<int> DeleteAsync<TEntity>(DbSet<TEntity> set, long id, CancellationToken ct) where TEntity : class
    {
        var entity = await set.FindAsync([id], ct);
        if (entity is null) return 0;
        set.Remove(entity);
        try { return await db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 547 })
        { return -2; }
    }

    public sealed record Input(string? NameFa, string? NameEn, string? Code, long? CountryId, long? ProvinceId,
        string? FlagPath = null, decimal? Latitude = null, decimal? Longitude = null,
        bool? IsProvinceCenter = null, bool UpdateDetails = false);
    public sealed record Place(long Id, string NameFa, string NameEn, string? Code, long? CountryId,
        long? ProvinceId, string? ParentName, string? FlagPath, string? SourceCountryName,
        decimal? Latitude, decimal? Longitude, bool? IsProvinceCenter);
}

