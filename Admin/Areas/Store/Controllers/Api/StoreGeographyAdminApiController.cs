using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Data.Application.Store;
using Shop.Data.Application.Store.Contracts;

namespace Shop.Admin.Areas.Store.Controllers.Api;

[Area("Store"), ApiController, Authorize(Roles = "system.full-access"), Route("api/admin/store/geography")]
public sealed class StoreGeographyAdminApiController(IStoreGeographyAdminData data, IMediaCdnBaseUrlProvider mediaCdn)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var geography = await data.ReadAsync(ct);
        string cdnBaseUrl = await mediaCdn.GetBaseUrlAsync(ct);
        var countryItems = geography.Countries.Select(x => new
        {
            x.Id, x.NameFa, x.NameEn, x.Code, x.FlagPath,
            FlagUrl = string.IsNullOrWhiteSpace(x.FlagPath) ? null : ContentCdn.ToUrl(x.FlagPath, cdnBaseUrl)
        }).ToArray();
        return Ok(new { countries = countryItems, provinces = geography.Provinces, cities = geography.Cities });
    }

    [HttpPost("{kind}"), ValidateAntiForgeryToken]
    public Task<IActionResult> Create(string kind, Input input, CancellationToken ct) => Save(kind, null, input, ct);

    [HttpPut("{kind}/{id:long}"), ValidateAntiForgeryToken]
    public Task<IActionResult> Update(string kind, long id, Input input, CancellationToken ct) =>
        Save(kind, id, input, ct);

    private async Task<IActionResult> Save(string kind, long? id, Input input, CancellationToken ct)
    {
        string fa = input.NameFa?.Trim() ?? "",
            en = input.NameEn?.Trim() ?? "",
            code = input.Code?.Trim().ToUpperInvariant() ?? "";
        if (kind is not ("countries" or "provinces" or "cities")) return BadRequest();

        int maxNameLength = kind == "cities" ? 200 : 150;
        if (fa.Length < 1 || fa.Length > maxNameLength || en.Length < 1 || en.Length > maxNameLength ||
            (kind == "countries" && (code.Length is < 1 or > 8 || !code.All(char.IsAsciiLetterOrDigit))))
            return BadRequest(new { title = "اطلاعات واردشده معتبر نیست." });

        string? flag = string.IsNullOrWhiteSpace(input.FlagPath) ? null : input.FlagPath.Trim();
        if (flag?.Length > 500 || (flag is not null &&
                                   !flag.StartsWith("content/", StringComparison.OrdinalIgnoreCase) &&
                                   !(Uri.TryCreate(flag, UriKind.Absolute, out var uri) &&
                                     uri.Scheme is "http" or "https")))
            return BadRequest(new { title = "مسیر پرچم باید مسیر content/ یا نشانی HTTP معتبر باشد." });

        if (input.Latitude is < -90 or > 90 || input.Longitude is < -180 or > 180)
            return BadRequest(new { title = "مختصات جغرافیایی معتبر نیست." });

        var result = await data.SaveAsync(kind, id,
            new GeographyInputData(input.CountryId, input.ProvinceId, input.Latitude, input.Longitude,
                input.IsProvinceCenter, input.UpdateDetails), fa, en, code, flag, ct);
        return result.Result switch
        {
            GeographySaveResult.Saved => Ok(new { message = "اطلاعات ذخیره شد.", id = result.Id }),
            GeographySaveResult.NotFound => NotFound(new { title = "رکورد پیدا نشد." }),
            GeographySaveResult.InvalidCountry => BadRequest(new { title = "کشور معتبر را انتخاب کنید." }),
            GeographySaveResult.InvalidProvince => BadRequest(new { title = "استان و کشور معتبر را انتخاب کنید." }),
            GeographySaveResult.IranCodeFixed => Conflict(new { title = "کد کشور ایران برای ثبت نشانی‌ها باید IR باقی بماند." }),
            _ => Conflict(new { title = "اطلاعات تکراری است یا وابستگی معتبر نیست." })
        };
    }
    [HttpDelete("{kind}/{id:long}"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string kind, long id, CancellationToken ct)
    {
        var result = await data.DeleteAsync(kind, id, ct);
        return result switch
        {
            GeographyDeleteResult.InvalidKind => BadRequest(),
            GeographyDeleteResult.InUse => Conflict(new { title = "این مورد در داده‌های دیگر استفاده شده و قابل حذف نیست." }),
            GeographyDeleteResult.NotFound => NotFound(new { title = "رکورد پیدا نشد." }),
            _ => Ok(new { message = "رکورد حذف شد." })
        };
    }

    public sealed record Input(
        string? NameFa,
        string? NameEn,
        string? Code,
        long? CountryId,
        long? ProvinceId,
        string? FlagPath = null,
        decimal? Latitude = null,
        decimal? Longitude = null,
        bool? IsProvinceCenter = null,
        bool UpdateDetails = false);

}
