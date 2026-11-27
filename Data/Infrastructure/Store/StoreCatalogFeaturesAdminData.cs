using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Text.RegularExpressions;
using Shop.Data.Application.Store.Contracts;
using Shop.Data.Domain.Store.Entities;
using Shop.Data.Infrastructure.Persistence;

namespace Shop.Data.Infrastructure.Store;

public sealed class StoreCatalogFeaturesAdminData(ShopDbContext db) : IStoreCatalogFeaturesAdminData
{
    public async Task<CatalogFeatureRenameStatus> RenameFeatureAsync(long featureId, string name,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,
            cancellationToken);
        var feature = await db.StoreCatalogFeatures.SingleOrDefaultAsync(x => x.Id == featureId && x.IdParent == null, cancellationToken);
        if (feature is null) return CatalogFeatureRenameStatus.NotFound;
        if (name.Length < 1 || name.Length > (feature.Kind == StoreCatalogFeatureKinds.PriceVariant ? 80 : 200))
            return CatalogFeatureRenameStatus.InvalidName;
        if (IsColorFeature(feature) && name != "رنگ")
            return CatalogFeatureRenameStatus.InvalidMetadata;
        if (await db.StoreCatalogFeatures.AnyAsync(x => x.Id != featureId && x.IdParent == null && x.Kind == feature.Kind && x.Name == name,
                cancellationToken)) return CatalogFeatureRenameStatus.Duplicate;
        if (feature.Kind == StoreCatalogFeatureKinds.PriceVariant)
        {
            var attributeIds = await db.StoreVariantCatalogFeatureValues.AsNoTracking()
                .Join(db.ProductVariantAttributeValues.AsNoTracking(), selected => selected.IdProductVariant,
                    oldLink => oldLink.IdProductVariant, (selected, oldLink) => new { selected, oldLink })
                .Join(db.ProductAttributes.AsNoTracking(), selectedLink => selectedLink.oldLink.IdProductAttribute,
                    attribute => attribute.Id, (selectedLink, attribute) => new { selectedLink.selected, attribute })
                .Where(row => row.selected.IdFeature == featureId && row.attribute.Label == feature.Name)
                .Select(row => row.attribute.Id).Distinct().ToListAsync(cancellationToken);
            var attributes = await db.ProductAttributes.Where(x => attributeIds.Contains(x.Id))
                .ToListAsync(cancellationToken);
            foreach (var attribute in attributes) attribute.Label = name;
        }
        feature.Name = name;
        feature.TreePathNames = feature.Name;
        var childValues = await db.StoreCatalogFeatures.Where(x => x.IdParent == featureId).ToListAsync(cancellationToken);
        foreach (var childValue in childValues)
            childValue.TreePathNames = $"{feature.Name}/{childValue.Name}";
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return CatalogFeatureRenameStatus.Saved;
    }

    public async Task<CatalogFeatureRenameStatus> RenameValueAsync(long featureId, long valueId, string name,
        string? valueKey, string? nameEn, string? hexCode,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,
            cancellationToken);
        var feature = await db.StoreCatalogFeatures.SingleOrDefaultAsync(x => x.Id == featureId && x.IdParent == null, cancellationToken);
        var value = await db.StoreCatalogFeatures.SingleOrDefaultAsync(x => x.Id == valueId && x.IdParent == featureId,
            cancellationToken);
        if (feature is null || value is null) return CatalogFeatureRenameStatus.NotFound;
        if (name.Length < 1 || name.Length > (feature.Kind == StoreCatalogFeatureKinds.PriceVariant ? 100 : 300))
            return CatalogFeatureRenameStatus.InvalidName;
        if (await db.StoreCatalogFeatures.AnyAsync(x => x.Id != valueId && x.IdParent == featureId && x.Name == name,
                cancellationToken)) return CatalogFeatureRenameStatus.Duplicate;
        if (IsColorFeature(feature) && (valueKey is not null || nameEn is not null || hexCode is not null))
        {
            if (!IsValidColorMetadata(valueKey, nameEn, hexCode))
                return CatalogFeatureRenameStatus.InvalidMetadata;
            if (await db.StoreCatalogFeatures.AnyAsync(x => x.Id != valueId && x.IdParent == featureId &&
                    x.ValueKey == valueKey, cancellationToken))
                return CatalogFeatureRenameStatus.Duplicate;
            value.ValueKey = valueKey!.Trim();
            value.NameEn = nameEn!.Trim();
            value.HexCode = hexCode!.Trim().ToUpperInvariant();
        }
        if (feature.Kind == StoreCatalogFeatureKinds.PriceVariant)
        {
            var valueIds = await db.StoreVariantCatalogFeatureValues.AsNoTracking()
                .Join(db.ProductVariantAttributeValues.AsNoTracking(), selected => selected.IdProductVariant,
                    oldLink => oldLink.IdProductVariant, (selected, oldLink) => new { selected, oldLink })
                .Join(db.ProductAttributes.AsNoTracking(), selectedLink => selectedLink.oldLink.IdProductAttribute,
                    attribute => attribute.Id, (selectedLink, attribute) => new { selectedLink.selected, selectedLink.oldLink, attribute })
                .Join(db.ProductAttributeValues.AsNoTracking(), selected => selected.oldLink.IdProductAttributeValue,
                    oldValue => oldValue.Id, (selected, oldValue) => new { selected.selected, selected.attribute, oldValue })
                .Where(row => row.selected.IdFeature == featureId && row.selected.IdFeatureValue == valueId &&
                    row.attribute.Label == feature.Name && row.oldValue.DisplayValue == value.Name)
                .Select(row => row.oldValue.Id).Distinct().ToListAsync(cancellationToken);
            foreach (var oldValue in await db.ProductAttributeValues.Where(x => valueIds.Contains(x.Id))
                         .ToListAsync(cancellationToken))
            {
                oldValue.DisplayValue = name;
                oldValue.Label = name;
            }
        }
        value.Name = name;
        value.TreePathNames = $"{feature.TreePathNames}/{name}";
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return CatalogFeatureRenameStatus.Saved;
    }

    public async Task<SaveCategoryFeaturesStatus> SaveCategoryFeaturesAsync(string key,
        IReadOnlyList<long> featureIds, CancellationToken cancellationToken)
    {
        long categoryId = await db.StoreCategories.Where(x => x.CategoryKey == key).Select(x => x.Id)
            .SingleOrDefaultAsync(cancellationToken);
        if (categoryId == 0) return SaveCategoryFeaturesStatus.NotFound;
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,
            cancellationToken);
        if (await ReadCategoryChainAsync(categoryId, cancellationToken) is null)
            return SaveCategoryFeaturesStatus.InvalidHierarchy;
        long[] ids = featureIds.Distinct().ToArray();
        if (await db.StoreCatalogFeatures.CountAsync(x => ids.Contains(x.Id) && x.IdParent == null && x.IsActive, cancellationToken) != ids.Length)
            return SaveCategoryFeaturesStatus.InvalidFeatures;
        var existing = await db.StoreCategoryCatalogFeatures.Where(x => x.IdCategory == categoryId)
            .ToListAsync(cancellationToken);
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
        if (removed.Length > 0 && (
            await db.StoreProductCatalogFeatureValues.AsNoTracking().Join(db.Products.AsNoTracking(),
                value => value.IdProduct, product => product.Id, (value, product) => new { value, product })
                .AnyAsync(row => row.product.IdCategory.HasValue && affected.Contains(row.product.IdCategory.Value) &&
                    removed.Contains(row.value.IdFeature), cancellationToken) ||
            await db.StoreVariantCatalogFeatureValues.AsNoTracking().Join(db.ProductVariants.AsNoTracking(),
                value => value.IdProductVariant, variant => variant.Id, (value, variant) => new { value, variant })
                .Join(db.Products.AsNoTracking(), row => row.variant.IdProduct, product => product.Id,
                    (row, product) => new { row.value, product })
                .AnyAsync(row => row.product.IdCategory.HasValue && affected.Contains(row.product.IdCategory.Value) &&
                    removed.Contains(row.value.IdFeature), cancellationToken)))
            return SaveCategoryFeaturesStatus.InUse;
        db.StoreCategoryCatalogFeatures.RemoveRange(existing.Where(x => !ids.Contains(x.IdFeature)));
        db.StoreCategoryCatalogFeatures.AddRange(ids.Where(id => existing.All(x => x.IdFeature != id))
            .Select(id => new StoreCategoryCatalogFeature { IdCategory = categoryId, IdFeature = id }));
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return SaveCategoryFeaturesStatus.Saved;
    }

    public async Task<SaveProductFeaturesStatus> SaveProductFeaturesAsync(long productId,
        IReadOnlyList<ProductFeatureSelectionData> selections, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,
            cancellationToken);
        long? categoryId = await db.Products.Where(x => x.Id == productId).Select(x => x.IdCategory)
            .SingleOrDefaultAsync(cancellationToken);
        if (categoryId is null) return SaveProductFeaturesStatus.NotFound;
        var chain = await ReadCategoryChainAsync(categoryId.Value, cancellationToken);
        if (chain is null) return SaveProductFeaturesStatus.InvalidHierarchy;
        var allowed = (await db.StoreCategoryCatalogFeatures.Where(x => chain.Contains(x.IdCategory))
            .Select(x => x.IdFeature).Distinct().ToListAsync(cancellationToken)).ToHashSet();
        long[] valueIds = selections.Select(x => x.ValueId).Distinct().ToArray();
        var valid = await db.StoreCatalogFeatures.AsNoTracking()
            .Where(value => valueIds.Contains(value.Id) && value.IdParent != null && value.IsActive)
            .Join(db.StoreCatalogFeatures.AsNoTracking(), value => value.IdParent, feature => (long?)feature.Id,
                (value, feature) => new { value, feature })
            .Where(row => row.feature.IsActive && row.feature.Kind == StoreCatalogFeatureKinds.Display)
            .Select(row => new { row.value.Id, FeatureId = row.value.IdParent })
            .ToDictionaryAsync(row => row.Id, row => row.FeatureId!.Value, cancellationToken);
        if (selections.Any(x => !allowed.Contains(x.FeatureId) ||
            !valid.TryGetValue(x.ValueId, out long featureId) || featureId != x.FeatureId))
            return SaveProductFeaturesStatus.InvalidSelections;
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
        return SaveProductFeaturesStatus.Saved;
    }

    public async Task<CatalogFeatureMutationResult> CreateFeatureAsync(string name, byte kind,
        CancellationToken cancellationToken)
    {
        if (await db.StoreCatalogFeatures.AnyAsync(x => x.IdParent == null && x.Kind == kind && x.Name == name, cancellationToken))
            return new CatalogFeatureMutationResult(CatalogFeatureMutationStatus.Duplicate, 0, name, kind);
        var feature = new StoreCatalogFeature { Name = name, Kind = kind };
        db.StoreCatalogFeatures.Add(feature);
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException)
        {
            return new CatalogFeatureMutationResult(CatalogFeatureMutationStatus.Duplicate, 0, name, kind);
        }
        feature.TreePathIds = feature.Id.ToString();
        feature.TreePathNames = feature.Name;
        await db.SaveChangesAsync(cancellationToken);
        return new CatalogFeatureMutationResult(CatalogFeatureMutationStatus.Created, feature.Id, feature.Name,
            feature.Kind);
    }

    public async Task<CatalogFeatureValueMutationResult> CreateValueAsync(long featureId, string name,
        string? valueKey, string? nameEn, string? hexCode,
        CancellationToken cancellationToken)
    {
        var feature = await db.StoreCatalogFeatures.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == featureId && x.IdParent == null && x.IsActive, cancellationToken);
        if (feature is null)
            return new CatalogFeatureValueMutationResult(CatalogFeatureValueMutationStatus.NotFound, 0, name);
        if (feature.Kind == StoreCatalogFeatureKinds.PriceVariant && name.Length > 100)
            return new CatalogFeatureValueMutationResult(CatalogFeatureValueMutationStatus.TooLong, 0, name);
        bool isColor = IsColorFeature(feature);
        if (isColor && !IsValidColorMetadata(valueKey, nameEn, hexCode))
            return new CatalogFeatureValueMutationResult(CatalogFeatureValueMutationStatus.InvalidMetadata, 0, name);
        if (await db.StoreCatalogFeatures.AnyAsync(x => x.IdParent == featureId && x.Name == name,
                cancellationToken))
            return new CatalogFeatureValueMutationResult(CatalogFeatureValueMutationStatus.Duplicate, 0, name);
        if (isColor && await db.StoreCatalogFeatures.AnyAsync(x => x.IdParent == featureId &&
                x.ValueKey == valueKey!.Trim(), cancellationToken))
            return new CatalogFeatureValueMutationResult(CatalogFeatureValueMutationStatus.Duplicate, 0, name);
        var value = new StoreCatalogFeature
        {
            IdParent = featureId,
            Kind = feature.Kind,
            Name = name,
            ValueKey = isColor ? valueKey!.Trim() : null,
            NameEn = isColor ? nameEn!.Trim() : null,
            HexCode = isColor ? hexCode!.Trim().ToUpperInvariant() : null
        };
        db.StoreCatalogFeatures.Add(value);
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException)
        {
            return new CatalogFeatureValueMutationResult(CatalogFeatureValueMutationStatus.Duplicate, 0, name);
        }
        value.TreePathIds = $"{feature.TreePathIds}/{value.Id}".TrimStart('/');
        value.TreePathNames = $"{feature.TreePathNames}/{value.Name}".TrimStart('/');
        await db.SaveChangesAsync(cancellationToken);
        return new CatalogFeatureValueMutationResult(CatalogFeatureValueMutationStatus.Created, value.Id, value.Name);
    }

    public async Task<IReadOnlyList<CatalogFeatureData>> ReadFeaturesAsync(CancellationToken cancellationToken)
    {
        var features = await db.StoreCatalogFeatures.AsNoTracking().Where(x => x.IdParent == null).OrderBy(x => x.Kind).ThenBy(x => x.Name)
            .Select(x => new { x.Id, x.Name, x.Kind, x.IsActive, x.TreePathIds, x.TreePathNames }).ToListAsync(cancellationToken);
        var values = await db.StoreCatalogFeatures.AsNoTracking().Where(x => x.IdParent != null).OrderBy(x => x.Name)
            .Select(x => new { x.Id, IdFeature = x.IdParent!.Value, x.Name, x.IsActive, x.TreePathIds, x.TreePathNames, x.ValueKey, x.NameEn, x.HexCode }).ToListAsync(cancellationToken);
        return features.Select(feature => new CatalogFeatureData(feature.Id, feature.Name, feature.Kind,
            feature.IsActive, feature.TreePathIds, feature.TreePathNames, values.Where(value => value.IdFeature == feature.Id)
                .Select(value => new CatalogFeatureValueData(value.Id, value.Name, value.IsActive,
                    value.TreePathIds, value.TreePathNames, value.ValueKey, value.NameEn, value.HexCode)).ToArray()))
            .ToArray();
    }

    public async Task<CategoryFeatureData?> ReadCategoryFeaturesAsync(string key,
        CancellationToken cancellationToken)
    {
        long categoryId = await db.StoreCategories.Where(x => x.CategoryKey == key).Select(x => x.Id)
            .SingleOrDefaultAsync(cancellationToken);
        if (categoryId == 0) return null;
        var chain = await ReadCategoryChainAsync(categoryId, cancellationToken);
        if (chain is null) return new CategoryFeatureData(false, [], []);
        var links = await db.StoreCategoryCatalogFeatures.AsNoTracking().Where(x => chain.Contains(x.IdCategory))
            .Select(x => new { x.IdCategory, x.IdFeature }).ToListAsync(cancellationToken);
        return new CategoryFeatureData(true,
            links.Where(x => x.IdCategory == categoryId).Select(x => x.IdFeature).Distinct().ToArray(),
            links.Where(x => x.IdCategory != categoryId).Select(x => x.IdFeature).Distinct().ToArray());
    }

    public async Task<ProductFeatureData> ReadProductFeaturesAsync(long productId,
        CancellationToken cancellationToken)
    {
        long? categoryId = await db.Products.AsNoTracking().Where(x => x.Id == productId)
            .Select(x => x.IdCategory).SingleOrDefaultAsync(cancellationToken);
        if (categoryId is null) return new ProductFeatureData(false, true, [], []);
        var chain = await ReadCategoryChainAsync(categoryId.Value, cancellationToken);
        if (chain is null) return new ProductFeatureData(true, false, [], []);
        long[] featureIds = await db.StoreCategoryCatalogFeatures.AsNoTracking()
            .Where(x => chain.Contains(x.IdCategory)).Select(x => x.IdFeature).Distinct().ToArrayAsync(cancellationToken);
        var selections = await db.StoreProductCatalogFeatureValues.AsNoTracking().Where(x => x.IdProduct == productId)
            .Select(x => new ProductFeatureSelectionData(x.IdFeature, x.IdFeatureValue)).ToArrayAsync(cancellationToken);
        return new ProductFeatureData(true, true, featureIds, selections);
    }

    private async Task<List<long>?> ReadCategoryChainAsync(long id, CancellationToken cancellationToken)
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

    private static bool IsColorFeature(StoreCatalogFeature feature) =>
        feature.Kind == StoreCatalogFeatureKinds.Display && feature.Name == "رنگ";

    private static bool IsValidColorMetadata(string? valueKey, string? nameEn, string? hexCode) =>
        valueKey is { Length: > 0 and <= 80 } &&
        Regex.IsMatch(valueKey.Trim(), "^[a-z0-9]+(?:-[a-z0-9]+)*$") &&
        nameEn is { Length: > 0 and <= 300 } && !string.IsNullOrWhiteSpace(nameEn) &&
        hexCode is { Length: 7 } && Regex.IsMatch(hexCode.Trim(), "^#[0-9a-fA-F]{6}$");
}
