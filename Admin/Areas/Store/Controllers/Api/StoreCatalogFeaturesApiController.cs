using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Data.Domain.Store.Entities;
using Shop.Data.Infrastructure.Persistence;

namespace Shop.Admin.Areas.Store.Controllers.Api;

[Area("Store")]
[ApiController]
[Route("api/admin/store/catalog-features")]
public sealed class StoreCatalogFeaturesApiController(ShopDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        if (!CanManageFeatures()) return Forbid();
        var features = await db.StoreCatalogFeatures.AsNoTracking().OrderBy(x => x.Kind).ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);
        var values = await db.StoreCatalogFeatureValues.AsNoTracking().OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
        return Ok(features.Select(feature => new
        {
            feature.Id, feature.Name, feature.Kind, feature.IsActive,
            values = values.Where(value => value.IdFeature == feature.Id)
                .Select(value => new { value.Id, value.Name, value.IsActive })
        }));
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
        if (await db.StoreCatalogFeatures.AnyAsync(x => x.Kind == input.Kind && x.Name == name, cancellationToken))
            return Conflict(new { title = "این ویژگی از قبل تعریف شده است." });
        var feature = new StoreCatalogFeature { Name = name, Kind = input.Kind };
        db.StoreCatalogFeatures.Add(feature);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict(new { title = "ثبت ویژگی انجام نشد؛ نام تکراری را بررسی کنید." });
        }

        return Ok(new { feature.Id, feature.Name, feature.Kind });
    }

    [HttpPost("{id:long}/values")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateValue(long id, [FromBody] FeatureValueInput input,
        CancellationToken cancellationToken)
    {
        if (!CanManageFeatures()) return Forbid();
        string name = (input.Name ?? "").Trim();
        if (name.Length is < 1 or > 300) return BadRequest(new { title = "نام مقدار معتبر نیست." });
        var feature = await db.StoreCatalogFeatures.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);
        if (feature is null) return NotFound();
        if (feature.Kind == StoreCatalogFeatureKinds.PriceVariant && name.Length > 100)
            return BadRequest(new { title = "مقدار تنوع قیمتی نباید بیش از ۱۰۰ نویسه باشد." });
        if (await db.StoreCatalogFeatureValues.AnyAsync(x => x.IdFeature == id && x.Name == name, cancellationToken))
            return Conflict(new { title = "این مقدار از قبل تعریف شده است." });
        var value = new StoreCatalogFeatureValue { IdFeature = id, Name = name };
        db.StoreCatalogFeatureValues.Add(value);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict(new { title = "ثبت مقدار انجام نشد؛ مقدار تکراری را بررسی کنید." });
        }

        return Ok(new { value.Id, value.Name });
    }

    [HttpPut("{id:long}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rename(long id, [FromBody] FeatureValueInput input,
        CancellationToken cancellationToken)
    {
        if (!CanManageFeatures()) return Forbid();
        string name = (input.Name ?? "").Trim();
        await using var transaction =
            await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        var feature = await db.StoreCatalogFeatures.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (feature is null) return NotFound();
        if (name.Length < 1 || name.Length > (feature.Kind == StoreCatalogFeatureKinds.PriceVariant ? 80 : 200))
            return BadRequest(new { title = "نام ویژگی معتبر نیست." });
        if (await db.StoreCatalogFeatures.AnyAsync(x => x.Id != id && x.Kind == feature.Kind && x.Name == name,
                cancellationToken)) return Conflict(new { title = "نام ویژگی تکراری است." });
        if (feature.Kind == StoreCatalogFeatureKinds.PriceVariant)
        {
            var attributeIds = await db.StoreVariantCatalogFeatureValues
                .AsNoTracking()
                .Join(
                    db.ProductVariantAttributeValues.AsNoTracking(),
                    selected => selected.IdProductVariant,
                    oldLink => oldLink.IdProductVariant,
                    (selected, oldLink) => new { selected, oldLink })
                .Join(
                    db.ProductAttributes.AsNoTracking(),
                    selectedLink => selectedLink.oldLink.IdProductAttribute,
                    attribute => attribute.Id,
                    (selectedLink, attribute) => new { selectedLink.selected, attribute })
                .Where(selectedAttribute => selectedAttribute.selected.IdFeature == id &&
                                            selectedAttribute.attribute.Label == feature.Name)
                .Select(selectedAttribute => selectedAttribute.attribute.Id)
                .Distinct()
                .ToListAsync(cancellationToken);
            var attributes = await db.ProductAttributes.Where(x => attributeIds.Contains(x.Id))
                .ToListAsync(cancellationToken);
            foreach (var attribute in attributes) attribute.Label = name;
        }

        feature.Name = name;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok(new { message = "نام ویژگی ذخیره شد." });
    }

    [HttpPut("{id:long}/values/{valueId:long}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RenameValue(long id, long valueId, [FromBody] FeatureValueInput input,
        CancellationToken cancellationToken)
    {
        if (!CanManageFeatures()) return Forbid();
        string name = (input.Name ?? "").Trim();
        await using var transaction =
            await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        var feature = await db.StoreCatalogFeatures.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        var value = await db.StoreCatalogFeatureValues.SingleOrDefaultAsync(x => x.Id == valueId && x.IdFeature == id,
            cancellationToken);
        if (feature is null || value is null) return NotFound();
        if (name.Length < 1 || name.Length > (feature.Kind == StoreCatalogFeatureKinds.PriceVariant ? 100 : 300))
            return BadRequest(new { title = "نام مقدار معتبر نیست." });
        if (await db.StoreCatalogFeatureValues.AnyAsync(x => x.Id != valueId && x.IdFeature == id && x.Name == name,
                cancellationToken)) return Conflict(new { title = "نام مقدار تکراری است." });
        if (feature.Kind == StoreCatalogFeatureKinds.PriceVariant)
        {
            var valueIds = await db.StoreVariantCatalogFeatureValues
                .AsNoTracking()
                .Join(
                    db.ProductVariantAttributeValues.AsNoTracking(),
                    selected => selected.IdProductVariant,
                    oldLink => oldLink.IdProductVariant,
                    (selected, oldLink) => new { selected, oldLink })
                .Join(
                    db.ProductAttributes.AsNoTracking(),
                    selectedLink => selectedLink.oldLink.IdProductAttribute,
                    attribute => attribute.Id,
                    (selectedLink, attribute) => new { selectedLink.selected, selectedLink.oldLink, attribute })
                .Join(
                    db.ProductAttributeValues.AsNoTracking(),
                    selectedAttribute => selectedAttribute.oldLink.IdProductAttributeValue,
                    oldValue => oldValue.Id,
                    (selectedAttribute, oldValue) => new { selectedAttribute.selected,
                        selectedAttribute.attribute, oldValue })
                .Where(selectedAttributeValue =>
                    selectedAttributeValue.selected.IdFeature == id &&
                    selectedAttributeValue.selected.IdFeatureValue == valueId &&
                    selectedAttributeValue.attribute.Label == feature.Name &&
                    selectedAttributeValue.oldValue.DisplayValue == value.Name)
                .Select(selectedAttributeValue => selectedAttributeValue.oldValue.Id)
                .Distinct()
                .ToListAsync(cancellationToken);
            foreach (var oldValue in await db.ProductAttributeValues.Where(x => valueIds.Contains(x.Id))
                         .ToListAsync(cancellationToken))
            {
                oldValue.DisplayValue = name;
                oldValue.Label = name;
            }
        }

        value.Name = name;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok(new { message = "نام مقدار ذخیره شد." });
    }

    [HttpGet("categories/{key}")]
    public async Task<IActionResult> Category(string key, CancellationToken cancellationToken)
    {
        if (!CanManageFeatures()) return Forbid();
        long categoryId = await db.StoreCategories.Where(x => x.CategoryKey == key).Select(x => x.Id)
            .SingleOrDefaultAsync(cancellationToken);
        if (categoryId == 0) return NotFound();
        var chain = await CategoryChain(categoryId, cancellationToken);
        if (chain is null) return NotFound();
        var links = await db.StoreCategoryCatalogFeatures.AsNoTracking().Where(x => chain.Contains(x.IdCategory))
            .ToListAsync(cancellationToken);
        return Ok(new
        {
            direct = links.Where(x => x.IdCategory == categoryId).Select(x => x.IdFeature).Distinct(),
            inherited = links.Where(x => x.IdCategory != categoryId).Select(x => x.IdFeature).Distinct()
        });
    }

    [HttpPut("categories/{key}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveCategory(string key, [FromBody] CategoryFeaturesInput input,
        CancellationToken cancellationToken)
    {
        if (!CanManageFeatures()) return Forbid();
        long categoryId = await db.StoreCategories.Where(x => x.CategoryKey == key).Select(x => x.Id)
            .SingleOrDefaultAsync(cancellationToken);
        if (categoryId == 0) return NotFound();
        long[] ids = (input.FeatureIds ?? []).Distinct().ToArray();
        if (ids.Length > 500 || ids.Any(x => x < 1)) return BadRequest();
        await using var transaction =
            await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        if (await CategoryChain(categoryId, cancellationToken) is null) return NotFound();
        if (await db.StoreCatalogFeatures.CountAsync(x => ids.Contains(x.Id) && x.IsActive, cancellationToken) !=
            ids.Length)
            return BadRequest(new { title = "یک یا چند ویژگی انتخاب‌شده معتبر یا فعال نیست." });
        var existing = await db.StoreCategoryCatalogFeatures.Where(x => x.IdCategory == categoryId)
            .ToListAsync(cancellationToken);
        // Existing assignments cannot be removed until dependent products and variants
        // have been reconciled; this prevents silently orphaning catalog selections.
        long[] removed = existing.Where(x => !ids.Contains(x.IdFeature)).Select(x => x.IdFeature).ToArray();
        var nodes = await db.StoreCategories.AsNoTracking().Select(x => new { x.Id, x.IdParentCategory })
            .ToListAsync(cancellationToken);
        var affected = new HashSet<long> { categoryId };
        bool added;
        do
        {
            added = false;
            foreach (var node in nodes)
                if (node.IdParentCategory is long parent && affected.Contains(parent) && affected.Add(node.Id))
                    added = true;
        } while (added);

        if (removed.Length != 0 && (await db.StoreProductCatalogFeatureValues
                                        .AsNoTracking()
                                        .Join(
                                            db.Products.AsNoTracking(),
                                            value => value.IdProduct,
                                            product => product.Id,
                                            (value, product) => new { value, product })
                                        .AnyAsync(valueProduct =>
                                                valueProduct.product.IdCategory.HasValue &&
                                                affected.Contains(valueProduct.product.IdCategory.Value) &&
                                                removed.Contains(valueProduct.value.IdFeature),
                                            cancellationToken) ||
                                    await db.StoreVariantCatalogFeatureValues
                                        .AsNoTracking()
                                        .Join(
                                            db.ProductVariants.AsNoTracking(),
                                            value => value.IdProductVariant,
                                            variant => variant.Id,
                                            (value, variant) => new { value, variant })
                                        .Join(
                                            db.Products.AsNoTracking(),
                                            valueVariant => valueVariant.variant.IdProduct,
                                            product => product.Id,
                                            (valueVariant, product) => new
                                            {
                                                valueVariant.value,
                                                product
                                            })
                                        .AnyAsync(valueProduct =>
                                                valueProduct.product.IdCategory.HasValue &&
                                                affected.Contains(valueProduct.product.IdCategory.Value) &&
                                                removed.Contains(valueProduct.value.IdFeature),
                                            cancellationToken)))
            return Conflict(
                new { title = "این ویژگی در کالایی استفاده شده و حذف تخصیص آن نیازمند بررسی وابستگی‌هاست." });
        db.StoreCategoryCatalogFeatures.RemoveRange(existing.Where(x => !ids.Contains(x.IdFeature)));
        db.StoreCategoryCatalogFeatures.AddRange(ids.Where(id => existing.All(x => x.IdFeature != id))
            .Select(id => new StoreCategoryCatalogFeature { IdCategory = categoryId, IdFeature = id }));
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok(new { message = "ویژگی‌های دسته ذخیره شدند." });
    }

    [HttpGet("products/{productId:long}")]
    public async Task<IActionResult> Product(long productId, CancellationToken cancellationToken)
    {
        if (!CanManageFeatures()) return Forbid();
        var product = await db.Products.AsNoTracking().Where(x => x.Id == productId).Select(x => new { x.IdCategory })
            .SingleOrDefaultAsync(cancellationToken);
        if (product?.IdCategory is not long categoryId) return NotFound();
        var chain = await CategoryChain(categoryId, cancellationToken);
        if (chain is null) return Conflict(new { title = "ساختار دستهٔ کالا معتبر نیست." });
        var featureIds = await db.StoreCategoryCatalogFeatures.AsNoTracking().Where(x => chain.Contains(x.IdCategory))
            .Select(x => x.IdFeature).Distinct().ToListAsync(cancellationToken);
        var selections = await db.StoreProductCatalogFeatureValues.AsNoTracking().Where(x => x.IdProduct == productId)
            .Select(x => new { x.IdFeature, x.IdFeatureValue }).ToListAsync(cancellationToken);
        return Ok(new { featureIds, selections });
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
        await using var transaction =
            await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        var product = await db.Products.Where(x => x.Id == productId).Select(x => new { x.IdCategory })
            .SingleOrDefaultAsync(cancellationToken);
        if (product?.IdCategory is not long categoryId) return NotFound();
        var chain = await CategoryChain(categoryId, cancellationToken);
        if (chain is null) return Conflict(new { title = "ساختار دستهٔ کالا معتبر نیست." });
        var allowed = (await db.StoreCategoryCatalogFeatures.Where(x => chain.Contains(x.IdCategory))
            .Select(x => x.IdFeature).Distinct().ToListAsync(cancellationToken)).ToHashSet();
        var valueIds = selections.Select(x => x.ValueId).Distinct().ToArray();
        var valid = await db.StoreCatalogFeatureValues
            .AsNoTracking()
            .Join(
                db.StoreCatalogFeatures.AsNoTracking(),
                value => value.IdFeature,
                feature => feature.Id,
                (value, feature) => new { value, feature })
            .Where(valueFeature => valueIds.Contains(valueFeature.value.Id) && valueFeature.value.IsActive &&
                                   valueFeature.feature.IsActive &&
                                   valueFeature.feature.Kind == StoreCatalogFeatureKinds.Display)
            .Select(valueFeature => new { valueFeature.value.Id, valueFeature.value.IdFeature })
            .ToDictionaryAsync(row => row.Id, row => row.IdFeature, cancellationToken);
        if (selections.Any(x =>
                !allowed.Contains(x.FeatureId) || !valid.TryGetValue(x.ValueId, out long featureId) ||
                featureId != x.FeatureId))
            return BadRequest(new { title = "ویژگی یا مقدار انتخاب‌شده برای دستهٔ کالا معتبر نیست." });
        var existing = await db.StoreProductCatalogFeatureValues.Where(x => x.IdProduct == productId)
            .ToListAsync(cancellationToken);
        db.StoreProductCatalogFeatureValues.RemoveRange(existing.Where(x =>
            !selections.Any(y => y.FeatureId == x.IdFeature && y.ValueId == x.IdFeatureValue)));
        db.StoreProductCatalogFeatureValues.AddRange(selections
            .Where(x => existing.All(y => y.IdFeature != x.FeatureId || y.IdFeatureValue != x.ValueId))
            .Select(x => new StoreProductCatalogFeatureValue
                { IdProduct = productId, IdFeature = x.FeatureId, IdFeatureValue = x.ValueId }));
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok(new { message = "ویژگی‌های کالا ذخیره شدند." });
    }

    private bool CanManageFeatures() => User.IsInRole("system.full-access") ||
                                        User.HasClaim("admin_permission", "Store:Catalog:Attributes");

    private async Task<List<long>?> CategoryChain(long id, CancellationToken cancellationToken)
    {
        var categories = await db.StoreCategories.AsNoTracking().Select(x => new { x.Id, x.IdParentCategory })
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        var chain = new List<long>();
        var seen = new HashSet<long>();
        long? current = id;
        while (current is long next)
        {
            if (!seen.Add(next) || !categories.TryGetValue(next, out var category)) return null;
            chain.Add(next);
            current = category.IdParentCategory;
        }

        return chain;
    }
}

public sealed record FeatureInput(string? Name, byte Kind);

public sealed record FeatureValueInput(string? Name);

public sealed record CategoryFeaturesInput(long[]? FeatureIds);

public sealed record ProductFeatureSelection(long FeatureId, long ValueId);

public sealed record ProductFeaturesInput(ProductFeatureSelection[]? Selections);
