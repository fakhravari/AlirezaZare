using Microsoft.AspNetCore.Mvc;
using Shop.Data.Application.Store.Contracts;
using Shop.Data.Domain.Store.Entities;

namespace Shop.Admin.Areas.Store.Controllers.Api;

[Area("Store")]
[ApiController]
[Route("api/admin/store/catalog-features")]
public sealed class StoreCatalogFeaturesApiController(IStoreCatalogFeaturesAdminData featureData) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        if (!CanManageFeatures()) return Forbid();
        return Ok(await featureData.ReadFeaturesAsync(cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromBody] FeatureInput input, CancellationToken cancellationToken)
    {
        if (!CanManageFeatures()) return Forbid();
        string name = (input.Name ?? "").Trim();
        if (name.Length is < 1 or > 200 ||
            input.Kind is not (StoreCatalogFeatureKinds.PriceVariant or StoreCatalogFeatureKinds.Display) ||
            (input.Kind == StoreCatalogFeatureKinds.PriceVariant && name.Length > 80))
            return BadRequest(new { title = "نام و نوع ویژگی معتبر نیست." });
        var result = await featureData.CreateFeatureAsync(name, input.Kind, cancellationToken);
        if (result.Status == CatalogFeatureMutationStatus.Duplicate)
            return Conflict(new { title = "ثبت ویژگی انجام نشد؛ نام تکراری را بررسی کنید." });
        return Ok(new { result.Id, result.Name, result.Kind });
    }

    [HttpPost("{id:long}/values")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateValue(long id, [FromBody] FeatureValueInput input,
        CancellationToken cancellationToken)
    {
        if (!CanManageFeatures()) return Forbid();
        string name = (input.Name ?? "").Trim();
        if (name.Length is < 1 or > 300) return BadRequest(new { title = "نام مقدار معتبر نیست." });
        var result = await featureData.CreateValueAsync(id, name, cancellationToken);
        return result.Status switch
        {
            CatalogFeatureValueMutationStatus.NotFound => NotFound(),
            CatalogFeatureValueMutationStatus.TooLong => BadRequest(new { title = "مقدار تنوع قیمتی نباید بیش از ۱۰۰ نویسه باشد." }),
            CatalogFeatureValueMutationStatus.Duplicate => Conflict(new { title = "ثبت مقدار انجام نشد؛ مقدار تکراری را بررسی کنید." }),
            _ => Ok(new { result.Id, result.Name })
        };
    }

    [HttpPut("{id:long}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rename(long id, [FromBody] FeatureValueInput input,
        CancellationToken cancellationToken)
    {
        if (!CanManageFeatures()) return Forbid();
        string name = (input.Name ?? "").Trim();
        var result = await featureData.RenameFeatureAsync(id, name, cancellationToken);
        return result switch
        {
            CatalogFeatureRenameStatus.NotFound => NotFound(),
            CatalogFeatureRenameStatus.InvalidName => BadRequest(new { title = "نام ویژگی معتبر نیست." }),
            CatalogFeatureRenameStatus.Duplicate => Conflict(new { title = "نام ویژگی تکراری است." }),
            _ => Ok(new { message = "نام ویژگی ذخیره شد." })
        };
    }

    [HttpPut("{id:long}/values/{valueId:long}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RenameValue(long id, long valueId, [FromBody] FeatureValueInput input,
        CancellationToken cancellationToken)
    {
        if (!CanManageFeatures()) return Forbid();
        string name = (input.Name ?? "").Trim();
        var result = await featureData.RenameValueAsync(id, valueId, name, cancellationToken);
        return result switch
        {
            CatalogFeatureRenameStatus.NotFound => NotFound(),
            CatalogFeatureRenameStatus.InvalidName => BadRequest(new { title = "نام مقدار معتبر نیست." }),
            CatalogFeatureRenameStatus.Duplicate => Conflict(new { title = "نام مقدار تکراری است." }),
            _ => Ok(new { message = "نام مقدار ذخیره شد." })
        };
    }

    [HttpGet("categories/{key}")]
    public async Task<IActionResult> Category(string key, CancellationToken cancellationToken)
    {
        if (!CanManageFeatures()) return Forbid();
        var result = await featureData.ReadCategoryFeaturesAsync(key, cancellationToken);
        if (result is null || !result.ValidHierarchy) return NotFound();
        return Ok(new { direct = result.Direct, inherited = result.Inherited });
    }

    [HttpPut("categories/{key}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveCategory(string key, [FromBody] CategoryFeaturesInput input,
        CancellationToken cancellationToken)
    {
        if (!CanManageFeatures()) return Forbid();
        long[] ids = (input.FeatureIds ?? []).Distinct().ToArray();
        if (ids.Length > 500 || ids.Any(x => x < 1)) return BadRequest();
        var result = await featureData.SaveCategoryFeaturesAsync(key, ids, cancellationToken);
        return result switch
        {
            SaveCategoryFeaturesStatus.NotFound => NotFound(),
            SaveCategoryFeaturesStatus.InvalidHierarchy => NotFound(),
            SaveCategoryFeaturesStatus.InvalidFeatures => BadRequest(new { title = "یک یا چند ویژگی انتخاب‌شده معتبر یا فعال نیست." }),
            SaveCategoryFeaturesStatus.InUse => Conflict(new { title = "این ویژگی در کالایی استفاده شده و حذف تخصیص آن نیازمند بررسی وابستگی‌هاست." }),
            _ => Ok(new { message = "ویژگی‌های دسته ذخیره شدند." })
        };
    }

    [HttpGet("products/{productId:long}")]
    public async Task<IActionResult> Product(long productId, CancellationToken cancellationToken)
    {
        if (!CanManageFeatures()) return Forbid();
        var result = await featureData.ReadProductFeaturesAsync(productId, cancellationToken);
        if (!result.Exists) return NotFound();
        if (!result.ValidHierarchy) return Conflict(new { title = "ساختار دستهٔ کالا معتبر نیست." });
        return Ok(new { featureIds = result.FeatureIds, selections = result.Selections });
    }

    [HttpPut("products/{productId:long}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveProduct(long productId, [FromBody] ProductFeaturesInput input,
        CancellationToken cancellationToken)
    {
        if (!CanManageFeatures()) return Forbid();
        var selections = input.Selections ?? [];
        if (selections.Length > 500 || selections.Any(x => x.FeatureId < 1 || x.ValueId < 1) ||
            selections.Select(x => (x.FeatureId, x.ValueId)).Distinct().Count() != selections.Length)
            return BadRequest();
        var dataSelections = selections.Select(x => new ProductFeatureSelectionData(x.FeatureId, x.ValueId)).ToArray();
        var result = await featureData.SaveProductFeaturesAsync(productId, dataSelections, cancellationToken);
        return result switch
        {
            SaveProductFeaturesStatus.NotFound => NotFound(),
            SaveProductFeaturesStatus.InvalidHierarchy => Conflict(new { title = "ساختار دستهٔ کالا معتبر نیست." }),
            SaveProductFeaturesStatus.InvalidSelections => BadRequest(new { title = "ویژگی یا مقدار انتخاب‌شده برای دستهٔ کالا معتبر نیست." }),
            _ => Ok(new { message = "ویژگی‌های کالا ذخیره شدند." })
        };
    }

    private bool CanManageFeatures() => User.IsInRole("system.full-access") ||
                                        User.HasClaim("admin_permission", "Store:Catalog:Attributes");

}

public sealed record FeatureInput(string? Name, byte Kind);

public sealed record FeatureValueInput(string? Name);

public sealed record CategoryFeaturesInput(long[]? FeatureIds);

public sealed record ProductFeatureSelection(long FeatureId, long ValueId);

public sealed record ProductFeaturesInput(ProductFeatureSelection[]? Selections);
